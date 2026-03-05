using System.Collections.Concurrent;

namespace Girvs.TypeFinder;

public class AppDomainTypeFinder(IGirvsFileProvider fileProvider = null) : ITypeFinder
{
    private readonly bool _ignoreReflectionErrors = true;
    protected readonly IGirvsFileProvider FileProvider =
        fileProvider ?? CommonHelper.DefaultFileProvider;

    // 程序集列表缓存
    private IList<Assembly>? _assembliesCache;
    private readonly object _assembliesLock = new();

    // 预编译正则，只编译一次
    private Regex? _skipPattern;
    private Regex? _restrictPattern;

    // 类型查找结果缓存
    private readonly ConcurrentDictionary<(Type, FindType), IEnumerable<Type>> _typeCache = new();

    // 按需编译正则，复用实例
    protected Regex SkipPattern =>
        _skipPattern ??= new Regex(
            AssemblySkipLoadingPattern,
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant
        );

    protected Regex RestrictPattern =>
        _restrictPattern ??= new Regex(
            AssemblyRestrictToLoadingPattern,
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant
        );

    protected virtual bool Matches(string assemblyFullName) =>
        !SkipPattern.IsMatch(assemblyFullName) && RestrictPattern.IsMatch(assemblyFullName);

    // GetAssemblies 加双重检查锁缓存，只扫描一次
    public virtual IList<Assembly> GetAssemblies()
    {
        if (_assembliesCache != null)
            return _assembliesCache;

        lock (_assembliesLock)
        {
            if (_assembliesCache != null)
                return _assembliesCache;

            var addedAssemblyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var assemblies = new List<Assembly>();

            if (LoadAppDomainAssemblies)
                AddAssembliesInAppDomain(addedAssemblyNames, assemblies);

            AddConfiguredAssemblies(addedAssemblyNames, assemblies);

            _assembliesCache = assemblies;
            return _assembliesCache;
        }
    }

    // 参数改为 HashSet<string>，Contains 从 O(n) 降到 O(1)
    private void AddAssembliesInAppDomain(
        HashSet<string> addedAssemblyNames,
        ICollection<Assembly> assemblies
    )
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            var fullName = assembly.FullName;
            if (fullName == null)
                continue;
            if (!Matches(fullName))
                continue;
            if (addedAssemblyNames.Contains(fullName))
                continue;

            assemblies.Add(assembly);
            addedAssemblyNames.Add(fullName);
        }
    }

    protected virtual void AddConfiguredAssemblies(
        HashSet<string> addedAssemblyNames,
        List<Assembly> assemblies
    )
    {
        foreach (var assemblyName in AssemblyNames)
        {
            var assembly = Assembly.Load(assemblyName);
            var fullName = assembly.FullName;
            if (fullName == null)
                continue;
            if (addedAssemblyNames.Contains(fullName))
                continue;

            assemblies.Add(assembly);
            addedAssemblyNames.Add(fullName);
        }
    }

    protected virtual void LoadMatchingAssemblies(string directoryPath)
    {
        if (!FileProvider.DirectoryExists(directoryPath))
            return;

        // 用 HashSet 加速已加载程序集的查重
        var loadedNames = GetAssemblies()
            .Select(a => a.FullName)
            .Where(n => n != null)
            .ToHashSet(StringComparer.OrdinalIgnoreCase)!;

        foreach (var dllPath in FileProvider.GetFiles(directoryPath, "*.dll"))
        {
            try
            {
                var an = AssemblyName.GetAssemblyName(dllPath);
                if (
                    an.FullName != null
                    && Matches(an.FullName)
                    && !loadedNames.Contains(an.FullName)
                )
                {
                    App.Load(an);
                }
            }
            catch (BadImageFormatException ex)
            {
                Trace.TraceError(ex.ToString());
            }
        }

        // 加载了新程序集后使缓存失效，下次 GetAssemblies 重新扫描
        InvalidateCache();
    }

    // 直接用 GetInterfaces()，去掉 FindInterfaces 的无意义委托分配
    protected virtual bool DoesTypeImplementOpenGeneric(Type type, Type openGeneric)
    {
        try
        {
            var genericTypeDefinition = openGeneric.GetGenericTypeDefinition();
            foreach (var iface in type.GetInterfaces())
            {
                if (!iface.IsGenericType)
                    continue;
                if (genericTypeDefinition.IsAssignableFrom(iface.GetGenericTypeDefinition()))
                    return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    // FindOfType 加缓存，相同查询只执行一次
    public IEnumerable<Type> FindOfType<T>(FindType findType = FindType.ConcreteClasses) =>
        FindOfType(typeof(T), findType);

    public IEnumerable<Type> FindOfType(
        Type assignTypeFrom,
        FindType findType = FindType.ConcreteClasses
    )
    {
        return _typeCache.GetOrAdd(
            (assignTypeFrom, findType),
            key => FindOfTypeInternal(key.Item1, GetAssemblies(), key.Item2).ToList()
        );
    }

    private IEnumerable<Type> FindOfTypeInternal(
        Type assignTypeFrom,
        IEnumerable<Assembly> assemblies,
        FindType findType
    )
    {
        var result = new List<Type>();
        var isOpenGeneric = assignTypeFrom.IsGenericTypeDefinition;

        try
        {
            foreach (var assembly in assemblies)
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    // 部分加载失败时取能加载的类型，不整个跳过程序集
                    if (!_ignoreReflectionErrors)
                        throw;
                    types = ex.Types.Where(t => t != null).ToArray()!;
                }
                catch
                {
                    if (!_ignoreReflectionErrors)
                        throw;
                    continue;
                }

                foreach (var type in types)
                {
                    if (!IsTypeMatch(type, assignTypeFrom, isOpenGeneric))
                        continue;

                    var matched = findType switch
                    {
                        FindType.ConcreteClasses => type.IsClass && !type.IsAbstract,
                        FindType.Interface => type.IsInterface,
                        FindType.AbstractClasses => type.IsClass && type.IsAbstract,
                        _ => false,
                    };

                    if (matched)
                        result.Add(type);
                }
            }
        }
        catch (ReflectionTypeLoadException ex)
        {
            var msg = string.Join(Environment.NewLine, ex.LoaderExceptions.Select(e => e?.Message));

            var fail = new Exception(msg, ex);
            Debug.WriteLine(fail.Message, fail);
            throw fail;
        }

        return result;
    }

    private bool IsTypeMatch(Type type, Type assignTypeFrom, bool isOpenGeneric)
    {
        if (assignTypeFrom.IsAssignableFrom(type))
            return true;

        if (!isOpenGeneric)
            return false;

        return DoesTypeImplementOpenGeneric(type, assignTypeFrom);
    }

    // 缓存失效入口，供子类或外部在动态加载程序集后调用
    protected void InvalidateCache()
    {
        lock (_assembliesLock)
        {
            _assembliesCache = null;
        }
        _typeCache.Clear();
    }

    public virtual AppDomain App => AppDomain.CurrentDomain;

    public bool LoadAppDomainAssemblies { get; set; } = true;

    public IList<string> AssemblyNames { get; set; } = new List<string>();

    // 更新跳过列表：移除已废弃的旧库，补充现代 .NET 生态常用库
    public string AssemblySkipLoadingPattern { get; set; } =
        @"^System\.|^mscorlib|^Microsoft\.|^netstandard"
        + @"|^Serilog|^AutoMapper|^MediatR|^Newtonsoft"
        + @"|^StackExchange|^Pomelo|^Oracle|^Npgsql"
        + @"|^Grpc\.|^Google\.|^OpenTelemetry"
        + @"|^Quartz|^DotNetCore\.CAP"
        + @"|^Consul|^Refit|^Swashbuckle";

    public string AssemblyRestrictToLoadingPattern { get; set; } = ".*";
}

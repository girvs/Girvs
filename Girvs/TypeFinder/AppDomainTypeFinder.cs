namespace Girvs.TypeFinder;

public class AppDomainTypeFinder : ITypeFinder
{
    private readonly bool _ignoreReflectionErrors = true;
    protected readonly IGirvsFileProvider FileProvider;

    public AppDomainTypeFinder(IGirvsFileProvider fileProvider = null)
    {
        FileProvider = fileProvider ?? CommonHelper.DefaultFileProvider;
    }

    private void AddAssembliesInAppDomain(ICollection<string> addedAssemblyNames, ICollection<Assembly> assemblies)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!Matches(assembly.FullName))
                continue;

            if (addedAssemblyNames.Contains(assembly.FullName))
                continue;

            assemblies.Add(assembly);
            addedAssemblyNames.Add(assembly.FullName);
        }
    }

    protected virtual void AddConfiguredAssemblies(List<string> addedAssemblyNames, List<Assembly> assemblies)
    {
        foreach (var assemblyName in AssemblyNames)
        {
            var assembly = Assembly.Load(assemblyName);
            if (addedAssemblyNames.Contains(assembly.FullName))
                continue;

            assemblies.Add(assembly);
            addedAssemblyNames.Add(assembly.FullName);
        }
    }

    protected virtual bool Matches(string assemblyFullName)
    {
        return !Matches(assemblyFullName, AssemblySkipLoadingPattern)
               && Matches(assemblyFullName, AssemblyRestrictToLoadingPattern);
    }

    protected virtual bool Matches(string assemblyFullName, string pattern)
    {
        return Regex.IsMatch(assemblyFullName, pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    protected virtual void LoadMatchingAssemblies(string directoryPath)
    {
        var loadedAssemblyNames = GetAssemblies().Select(a => a.FullName).ToList();

        if (!FileProvider.DirectoryExists(directoryPath))
        {
            return;
        }

        foreach (var dllPath in FileProvider.GetFiles(directoryPath, "*.dll"))
        {
            try
            {
                var an = AssemblyName.GetAssemblyName(dllPath);
                if (Matches(an.FullName) && !loadedAssemblyNames.Contains(an.FullName))
                {
                    App.Load(an);
                }

                //old loading stuff
                //Assembly a = Assembly.ReflectionOnlyLoadFrom(dllPath);
                //if (Matches(a.FullName) && !loadedAssemblyNames.Contains(a.FullName))
                //{
                //    App.Load(a.FullName);
                //}
            }
            catch (BadImageFormatException ex)
            {
                Trace.TraceError(ex.ToString());
            }
        }
    }

    protected virtual bool DoesTypeImplementOpenGeneric(Type type, Type openGeneric)
    {
        try
        {
            var genericTypeDefinition = openGeneric.GetGenericTypeDefinition();
            foreach (var implementedInterface in type.FindInterfaces((objType, objCriteria) => true, null))
            {
                if (!implementedInterface.IsGenericType)
                    continue;

                if (genericTypeDefinition.IsAssignableFrom(implementedInterface.GetGenericTypeDefinition()))
                    return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    protected virtual IEnumerable<Type> FindOfType(Type assignTypeFrom, IEnumerable<Assembly> assemblies,
        FindType findType)
    {
        var result = new List<Type>();
        try
        {
            foreach (var a in assemblies)
            {
                Type[] types = null;
                try
                {
                    types = a.GetTypes();
                }
                catch
                {
                    //Entity Framework 6 doesn't allow getting types (throws an exception)
                    if (!_ignoreReflectionErrors)
                    {
                        throw;
                    }
                }

                if (types == null)
                    continue;

                foreach (var t in types)
                {
                    if (!assignTypeFrom.IsAssignableFrom(t) && (!assignTypeFrom.IsGenericTypeDefinition ||
                                                                !DoesTypeImplementOpenGeneric(t, assignTypeFrom)))
                        continue;

                    switch (findType)
                    {
                        case FindType.ConcreteClasses:
                            if (t.IsClass && !t.IsAbstract)
                            {
                                result.Add(t);
                            }
                            break;
                        case FindType.Interface:
                            if (t.IsInterface)
                            {
                                result.Add(t);
                            }
                            break;
                        case FindType.AbstractClasses:
                            if (t.IsClass && !t.IsAbstract)
                            {
                                result.Add(t);
                            }
                            break;
                    }
                    // if (t.IsInterface)
                    //     continue;
                    //
                    // if (onlyConcreteClasses)
                    // {
                    //     if (t.IsClass && !t.IsAbstract)
                    //     {
                    //         result.Add(t);
                    //     }
                    // }
                    // else
                    // {
                    //     result.Add(t);
                    // }
                }
            }
        }
        catch (ReflectionTypeLoadException ex)
        {
            var msg = string.Empty;
            foreach (var e in ex.LoaderExceptions)
                msg += e.Message + Environment.NewLine;

            var fail = new Exception(msg, ex);
            Debug.WriteLine(fail.Message, fail);

            throw fail;
        }

        return result;
    }

    public IEnumerable<Type> FindOfType<T>(FindType findType = FindType.ConcreteClasses)
    {
        return FindOfType(typeof(T), findType);
    }

    public IEnumerable<Type> FindOfType(Type assignTypeFrom, FindType findType = FindType.ConcreteClasses)
    {
        return FindOfType(assignTypeFrom, GetAssemblies(), findType);
    }

    public virtual IList<Assembly> GetAssemblies()
    {
        var addedAssemblyNames = new List<string>();
        var assemblies = new List<Assembly>();

        if (LoadAppDomainAssemblies)
            AddAssembliesInAppDomain(addedAssemblyNames, assemblies);
        AddConfiguredAssemblies(addedAssemblyNames, assemblies);

        return assemblies;
    }

    public virtual AppDomain App => AppDomain.CurrentDomain;

    public bool LoadAppDomainAssemblies { get; set; } = true;

    public IList<string> AssemblyNames { get; set; } = new List<string>();

    public string AssemblySkipLoadingPattern { get; set; } =
        "^System|^mscorlib|^Microsoft|^AjaxControlToolkit|^Antlr3|^Autofac|^AutoMapper|^Castle|^ComponentArt|^CppCodeProvider|^DotNetOpenAuth|^EntityFramework|^EPPlus|^FluentValidation|^ImageResizer|^itextsharp|^log4net|^MaxMind|^MbUnit|^MiniProfiler|^Mono.Math|^MvcContrib|^Newtonsoft|^NHibernate|^nunit|^Org.Mentalis|^PerlRegex|^QuickGraph|^Recaptcha|^Remotion|^RestSharp|^Rhino|^Telerik|^Iesi|^TestDriven|^TestFu|^UserAgentStringLibrary|^VJSharpCodeProvider|^WebActivator|^WebDev|^WebGrease";

    public string AssemblyRestrictToLoadingPattern { get; set; } = ".*";
}
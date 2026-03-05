namespace Girvs.TypeFinder;

public class WebAppTypeFinder(IGirvsFileProvider fileProvider = null) : AppDomainTypeFinder(fileProvider)
{
    // ✅ 修复五：改用 int + Interlocked，线程安全无锁
    private int _binFolderAssembliesLoaded;

    public bool EnsureBinFolderAssembliesLoaded { get; set; } = true;

    public virtual string GetBinDirectory() => AppContext.BaseDirectory;

    public override IList<Assembly> GetAssemblies()
    {
        if (!EnsureBinFolderAssembliesLoaded)
            return base.GetAssemblies();

        // ✅ Interlocked.Exchange 原子操作，确保只加载一次
        if (Interlocked.Exchange(ref _binFolderAssembliesLoaded, 1) == 0)
        {
            LoadMatchingAssemblies(GetBinDirectory());
            // LoadMatchingAssemblies 内部已调用 InvalidateCache()
        }

        return base.GetAssemblies();
    }
}
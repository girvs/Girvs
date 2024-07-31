namespace Girvs.TypeFinder;

public class WebAppTypeFinder(IGirvsFileProvider fileProvider = null)
    : AppDomainTypeFinder(fileProvider)
{
    private bool _binFolderAssembliesLoaded;

    public bool EnsureBinFolderAssembliesLoaded { get; set; } = true;

    public virtual string GetBinDirectory() => AppContext.BaseDirectory;

    public override IList<Assembly> GetAssemblies()
    {
        if (!EnsureBinFolderAssembliesLoaded || _binFolderAssembliesLoaded)
            return base.GetAssemblies();

        _binFolderAssembliesLoaded = true;
        var binPath = GetBinDirectory();
        LoadMatchingAssemblies(binPath);

        return base.GetAssemblies();
    }
}

namespace Girvs.TypeFinder;

public class WebAppTypeFinder : AppDomainTypeFinder
{
    private bool _binFolderAssembliesLoaded;

    public WebAppTypeFinder(IGirvsFileProvider fileProvider = null) : base(fileProvider)
    {
    }

    public bool EnsureBinFolderAssembliesLoaded { get; set; } = true;

    public virtual string GetBinDirectory()
    {
        return AppContext.BaseDirectory;
    }

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
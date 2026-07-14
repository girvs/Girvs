namespace Girvs.Aspire.Hosting;

public static class GirvsResourceRegistry
{
    private static readonly Dictionary<string, IGirvsResourceContributor> Contributors = new();

    static GirvsResourceRegistry()
    {
        Register(new CacheResourceContributor());
        Register(new EventBusResourceContributor());
        Register(new DatabaseResourceContributor());
    }

    /// <summary>注册自定义模块资源贡献器（同名覆盖），供业务方扩展自己的模块。</summary>
    public static void Register(IGirvsResourceContributor contributor)
    {
        ArgumentNullException.ThrowIfNull(contributor);
        Contributors[contributor.ModuleTypeFullName] = contributor;
    }

    public static bool TryGet(Type moduleType, out IGirvsResourceContributor contributor) =>
        Contributors.TryGetValue(moduleType.FullName ?? string.Empty, out contributor);
}

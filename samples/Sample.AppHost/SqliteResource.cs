using Aspire.Hosting.ApplicationModel;
using Girvs.Aspire.Hosting;
using GirvsResource = Girvs.Configuration.Resources.Resource;

namespace Sample.AppHost;

/// <summary>
/// 自定义资源扩展示例:SQLite 是本地文件数据库,不需要容器。
/// 实现 IResourceWithoutLifetime,AddGirvsProject 不会对它 WaitFor。
/// </summary>
public sealed class SqliteResource(string name, string databasePath)
    : Resource(name), IResourceWithoutLifetime
{
    public string DatabasePath { get; } = databasePath;
}

public static class SqliteResourceExtensions
{
    /// <summary>编排一个本地 SQLite 库文件,配合 AsGirvsResource 写入共享配置。</summary>
    public static IResourceBuilder<SqliteResource> AddSqlite(
        this IDistributedApplicationBuilder builder,
        string name,
        string databasePath
    ) => builder.AddResource(new SqliteResource(name, databasePath));
}

/// <summary>
/// IGirvsResourceSettingsProvider 扩展示例:把 SqliteResource 提取为
/// Type=sqlite 的 Girvs Resource 条目,服务端消费模块按 DataSource 组装连接串。
/// 在 Program.cs 中通过 GirvsResourceSettingsProviders.Register 注册,
/// 自定义提供程序优先于框架内置预设(redis/mysql/sqlserver/rabbitmq/kafka)。
/// </summary>
public sealed class SqliteSettingsProvider : IGirvsResourceSettingsProvider
{
    public Task<GirvsResource> TryBuildAsync(IResource resource, CancellationToken ct)
    {
        if (resource is not SqliteResource sqlite)
            return Task.FromResult<GirvsResource>(null);

        return Task.FromResult(
            new GirvsResource
            {
                Type = "sqlite",
                Settings = new Dictionary<string, string> { ["DataSource"] = sqlite.DatabasePath },
            }
        );
    }
}

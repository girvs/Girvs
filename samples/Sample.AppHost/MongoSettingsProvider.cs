using Aspire.Hosting.ApplicationModel;
using Girvs.Aspire.Hosting;
using GirvsResource = Girvs.Configuration.Resources.Resource;

namespace Sample.AppHost;

/// <summary>
/// IGirvsResourceSettingsProvider 扩展示例(有容器的官方集成包场景):
/// 把 Aspire.Hosting.MongoDB 编排的资源提取为 Type=mongodb 的 Girvs Resource 条目。
/// 与 SqliteSettingsProvider(无容器的本地文件场景)对照,覆盖两类扩展方式。
/// MongoDBServerResource 是普通容器资源,AddGirvsProject 会正常对它 WaitFor。
/// </summary>
public sealed class MongoSettingsProvider : IGirvsResourceSettingsProvider
{
    public async Task<GirvsResource> TryBuildAsync(IResource resource, CancellationToken ct)
    {
        var (server, databaseName) = resource switch
        {
            MongoDBDatabaseResource database => (database.Parent, database.DatabaseName),
            MongoDBServerResource mongo => (mongo, null),
            _ => ((MongoDBServerResource)null, null),
        };
        if (server is null)
            return null;

        var endpoint = server.PrimaryEndpoint;
        var settings = new Dictionary<string, string>
        {
            ["Host"] = endpoint.Host,
            ["Port"] = endpoint.Port.ToString(),
        };
        // Aspire 的 AddMongoDB 未显式传 userName 时容器 root 用户固定为 admin
        settings["UserName"] = server.UserNameParameter is null
            ? "admin"
            : await server.UserNameParameter.GetValueAsync(ct);
        if (server.PasswordParameter is not null)
            settings["Password"] = await server.PasswordParameter.GetValueAsync(ct);
        if (databaseName is not null)
            settings["Database"] = databaseName;

        return new GirvsResource { Type = "mongodb", Settings = settings };
    }
}

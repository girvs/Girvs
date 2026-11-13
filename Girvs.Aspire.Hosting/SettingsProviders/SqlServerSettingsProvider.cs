

namespace Girvs.Aspire.Hosting.SettingsProviders;

/// <summary>内置预设:SQL Server 服务器/数据库资源 → Type=sqlserver,Settings.Host/Port/UserName/Password/Database。</summary>
public class SqlServerSettingsProvider : IGirvsResourceSettingsProvider
{
    public virtual async Task<GirvsInfrastructureResource> TryBuildAsync(IResource resource, CancellationToken ct)
    {
        var (server, databaseName) = resource switch
        {
            SqlServerDatabaseResource database => (database.Parent, database.DatabaseName),
            SqlServerServerResource sql => (sql, null),
            _ => (null, null),
        };
        if (server is null)
            return null;

        var (host, port) = server.PrimaryEndpoint();
        var settings = new Dictionary<string, string>
        {
            ["Host"] = host,
            ["Port"] = port.ToString(),
            ["UserName"] = "sa",
            ["Password"] = await server.PasswordParameter.GetValueAsync(ct),
        };
        if (databaseName is not null)
            settings["Database"] = databaseName;
        return new GirvsInfrastructureResource { Type = "sqlserver", Settings = settings };
    }
}

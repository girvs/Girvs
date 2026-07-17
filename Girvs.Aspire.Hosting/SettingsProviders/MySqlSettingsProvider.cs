using GirvsResource = Girvs.Configuration.Resources.Resource;

namespace Girvs.Aspire.Hosting.SettingsProviders;

/// <summary>内置预设:MySQL 服务器/数据库资源 → Type=mysql,Settings.Host/Port/UserName/Password/Database。</summary>
public class MySqlSettingsProvider : IGirvsResourceSettingsProvider
{
    public virtual async Task<GirvsResource> TryBuildAsync(IResource resource, CancellationToken ct)
    {
        var (server, databaseName) = resource switch
        {
            MySqlDatabaseResource database => (database.Parent, database.DatabaseName),
            MySqlServerResource mysql => (mysql, null),
            _ => (null, null),
        };
        if (server is null)
            return null;

        var (host, port) = server.PrimaryEndpoint();
        var settings = new Dictionary<string, string>
        {
            ["Host"] = host,
            ["Port"] = port.ToString(),
            ["UserName"] = "root",
            ["Password"] = await server.PasswordParameter.GetValueAsync(ct),
        };
        if (databaseName is not null)
            settings["Database"] = databaseName;
        return new GirvsResource { Type = "mysql", Settings = settings };
    }
}

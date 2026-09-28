using Girvs.Configuration.Resources;

namespace Girvs.EventBus.Configuration;

public class EventBusConfig : IAppModuleConfig
{
    private const string DefaultPersistenceDatabaseName = "Girvs_EventBus";

    public string PersistenceConnectionRef { get; set; }
    public string TransportConnectionRef { get; set; }
    public int ConsumerThreadCount { get; set; } = 1;
    public int ProducerThreadCount { get; set; } = 1;
    public int SucceedMessageExpiredAfter { get; set; } = 60;
    public int FailedMessageExpiredAfter { get; set; } = 15 * 24 * 3600;
    public void Init() { }

    public string BuildPersistenceConnectionString(GirvsInfrastructureResource resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        return resource.Type.ToLowerInvariant() switch
        {
            "sqlserver" => BuildSqlServerConnectionString(resource),
            "mysql" => BuildMySqlConnectionString(resource),
            "sqlite" => BuildSqliteConnectionString(resource),
            _ => throw new GirvsException($"Resources:{PersistenceConnectionRef}:Type 不支持事件总线持久化"),
        };
    }

    private string BuildMySqlConnectionString(GirvsInfrastructureResource resource)
    {
        var host = resource.Settings.GetValueOrDefault("Host") ?? throw new GirvsException($"Resources:{PersistenceConnectionRef}:Settings:Host 未配置");
        var builder = new System.Data.Common.DbConnectionStringBuilder { ["Server"] = host };
        builder["Database"] = GetDatabaseName(resource);
        if (int.TryParse(resource.Settings.GetValueOrDefault("Port"), out var port)) builder["Port"] = port;
        if (resource.Settings.TryGetValue("UserName", out var userName)) builder["User ID"] = userName;
        if (resource.Settings.TryGetValue("Password", out var password)) builder["Password"] = password;
        return builder.ConnectionString;
    }

    private string BuildSqlServerConnectionString(GirvsInfrastructureResource resource)
    {
        var host = resource.Settings.GetValueOrDefault("Host") ?? throw new GirvsException($"Resources:{PersistenceConnectionRef}:Settings:Host 未配置");
        var builder = new System.Data.Common.DbConnectionStringBuilder
        {
            ["Data Source"] = int.TryParse(resource.Settings.GetValueOrDefault("Port"), out var port) ? $"{host},{port}" : host,
        };
        builder["Initial Catalog"] = GetDatabaseName(resource);
        if (resource.Settings.TryGetValue("UserName", out var userName)) builder["User ID"] = userName;
        if (resource.Settings.TryGetValue("Password", out var password)) builder["Password"] = password;
        return builder.ConnectionString;
    }

    private string BuildSqliteConnectionString(GirvsInfrastructureResource resource)
    {
        var dataSource = resource.Settings.GetValueOrDefault("DataSource")
                         ?? resource.Settings.GetValueOrDefault("Data Source")
                         ?? throw new GirvsException($"Resources:{PersistenceConnectionRef}:Settings:DataSource 未配置");
        var builder = new System.Data.Common.DbConnectionStringBuilder { ["Data Source"] = dataSource };
        return builder.ConnectionString;
    }

    private static string GetDatabaseName(GirvsInfrastructureResource resource)
    {
        return resource.Settings.TryGetValue("Database", out var database) && !string.IsNullOrWhiteSpace(database)
            ? database
            : DefaultPersistenceDatabaseName;
    }
}

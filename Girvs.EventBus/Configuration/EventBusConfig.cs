using Girvs.Configuration.Resources;

namespace Girvs.EventBus.Configuration;

public class EventBusConfig : IAppModuleConfig
{
    public string PersistenceConnectionRef { get; set; }
    public string TransportConnectionRef { get; set; }
    public int ConsumerThreadCount { get; set; } = 1;
    public int ProducerThreadCount { get; set; } = 1;
    public int SucceedMessageExpiredAfter { get; set; } = 60;
    public int FailedMessageExpiredAfter { get; set; } = 15 * 24 * 3600;
    public void Init() { }

    public string BuildPersistenceConnectionString(Resource resource)
    {
        if (!string.Equals(resource.Type, "mysql", StringComparison.OrdinalIgnoreCase)) throw new GirvsException($"Resources:{PersistenceConnectionRef}:Type 必须为 mysql");
        var host = resource.Settings.GetValueOrDefault("Host") ?? throw new GirvsException($"Resources:{PersistenceConnectionRef}:Settings:Host 未配置");
        var builder = new System.Data.Common.DbConnectionStringBuilder { ["Server"] = host };
        if (resource.Settings.TryGetValue("Database", out var database)) builder["Database"] = database;
        if (int.TryParse(resource.Settings.GetValueOrDefault("Port"), out var port)) builder["Port"] = port;
        if (resource.Settings.TryGetValue("UserName", out var userName)) builder["User ID"] = userName;
        if (resource.Settings.TryGetValue("Password", out var password)) builder["Password"] = password;
        return builder.ConnectionString;
    }
}

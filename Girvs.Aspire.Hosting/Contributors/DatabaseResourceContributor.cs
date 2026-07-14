namespace Girvs.Aspire.Hosting.Contributors;

public class DatabaseResourceContributor : IGirvsResourceContributor
{
    public string ModuleTypeFullName =>
        "Girvs.EntityFrameworkCore.GirvsEntityFrameworkCoreModule";

    public void Contribute(GirvsOrchestrationContext context)
    {
        var connectionConfigs = context
            .ServiceSettings.GetSection("ModuleConfigurations:DbConfig:DataConnectionConfigs")
            .GetChildren()
            .ToList();

        if (connectionConfigs.Count == 0)
        {
            Console.Error.WriteLine(
                $"[Girvs.Aspire.Hosting] 服务 {context.Project.Resource.Name} 未配置 DataConnectionConfigs，未自动创建数据库资源"
            );
            return;
        }

        foreach (var connectionConfig in connectionConfigs)
        {
            var name = connectionConfig["Name"] ?? "default";
            var useDataType = connectionConfig["UseDataType"]?.ToLowerInvariant();
            var serviceName = context.Project.Resource.Name;

            // 每服务独立数据库（架构图 orderdb/userdb 模式）：资源名全局唯一，
            // 注入名固定为 girvs-db-<name> 与服务端映射约定对齐
            var resourceName = $"girvs-db-{serviceName}-{name}";
            var databaseName = $"{serviceName}_{name}".Replace('-', '_');
            var connectionName = $"girvs-db-{name}";

            switch (useDataType)
            {
                case "mysql":
                {
                    var server = context.GetOrAddResource(
                        "girvs-mysql",
                        () => context.Builder.AddMySql("girvs-mysql")
                    );
                    var database = context.GetOrAddResource(
                        resourceName,
                        () => server.AddDatabase(resourceName, databaseName)
                    );
                    context.Project.WithReference(database, connectionName).WaitFor(database);
                    break;
                }
                case "mssql":
                {
                    var server = context.GetOrAddResource(
                        "girvs-sqlserver",
                        () => context.Builder.AddSqlServer("girvs-sqlserver")
                    );
                    var database = context.GetOrAddResource(
                        resourceName,
                        () => server.AddDatabase(resourceName, databaseName)
                    );
                    context.Project.WithReference(database, connectionName).WaitFor(database);
                    break;
                }
                default:
                    Console.Error.WriteLine(
                        $"[Girvs.Aspire.Hosting] 数据连接 '{name}' 的 UseDataType 为 '{useDataType ?? "未配置"}'，未自动创建数据库资源"
                    );
                    break;
            }
        }
    }
}

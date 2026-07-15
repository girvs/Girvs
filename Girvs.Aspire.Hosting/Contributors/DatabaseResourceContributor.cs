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
            var useDataType = NormalizeUseDataType(connectionConfig["UseDataType"]);
            var serviceName = context.Project.Resource.Name;

            // 每服务独立数据库（架构图 orderdb/userdb 模式）：资源名全局唯一，
            // 注入名固定为 girvs-db-<name> 与服务端映射约定对齐
            var resourceName = $"girvs-db-{serviceName}-{name}";
            var databaseName = $"{serviceName}_{name}".Replace('-', '_');
            var connectionName = $"girvs-db-{name}";

            if (context.Builder.ExecutionContext.IsPublishMode)
            {
                // 生产：阿里云 RDS 上已建好的独立库，连接串由部署参数/配置提供；不区分 MySql/MsSql
                // （都是外部连接串），不在集群内新建数据库服务器容器。
                var external = context.GetOrAddResource(
                    resourceName,
                    () => context.Builder.AddConnectionString(resourceName)
                );
                context.Project.WithReference(external, connectionName);
                continue;
            }

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

    // 服务端 appsettings 中枚举可能被持久化为名称（"MySql"）或数值（"1"）。
    // 归一到小写名称，兼容两种形式（UseDataType: MsSql=0, MySql=1）。
    private static string NormalizeUseDataType(string raw) =>
        raw?.Trim().ToLowerInvariant() switch
        {
            "0" or "mssql" => "mssql",
            "1" or "mysql" => "mysql",
            var other => other
        };
}

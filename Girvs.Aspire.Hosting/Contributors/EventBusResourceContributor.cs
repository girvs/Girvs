namespace Girvs.Aspire.Hosting.Contributors;

public class EventBusResourceContributor : IGirvsResourceContributor
{
    public string ModuleTypeFullName => "Girvs.EventBus.EventBusModule";

    public void Contribute(GirvsOrchestrationContext context)
    {
        var eventBusType = NormalizeEventBusType(
            context.ServiceSettings["ModuleConfigurations:EventBusConfig:EventBusType"]
        );

        var isPublish = context.Builder.ExecutionContext.IsPublishMode;

        switch (eventBusType)
        {
            case "rabbitmq":
                if (isPublish)
                {
                    // 生产：引用外部消息队列连接串，不在集群内新建容器
                    var ext = context.GetOrAddResource(
                        "girvs-eventbus-rabbitmq",
                        () => context.Builder.AddConnectionString("girvs-eventbus-rabbitmq")
                    );
                    context.Project.WithReference(ext);
                }
                else
                {
                    var rabbit = context.GetOrAddResource(
                        "girvs-eventbus-rabbitmq",
                        () => context.Builder.AddRabbitMQ("girvs-eventbus-rabbitmq")
                    );
                    context.Project.WithReference(rabbit).WaitFor(rabbit);
                }
                break;
            case "redis":
                if (isPublish)
                {
                    var ext = context.GetOrAddResource(
                        "girvs-eventbus-redis",
                        () => context.Builder.AddConnectionString("girvs-eventbus-redis")
                    );
                    context.Project.WithReference(ext);
                }
                else
                {
                    var redis = context.GetOrAddResource(
                        "girvs-eventbus-redis",
                        () => context.Builder.AddRedis("girvs-eventbus-redis")
                    );
                    context.Project.WithReference(redis).WaitFor(redis);
                }
                break;
            default:
                // Kafka（云集群直连）或配置缺失：不自动创建，可按 girvs-* 命名约定手动补资源
                Console.Error.WriteLine(
                    $"[Girvs.Aspire.Hosting] 服务 {context.Project.Resource.Name} 的 EventBusType 为 '{eventBusType ?? "未配置"}'，未自动创建事件总线资源"
                );
                break;
        }
    }

    // 服务端 appsettings 中枚举可能被持久化为名称（"RabbitMQ"）或数值（"0"）。
    // 归一到小写名称，兼容两种形式（EventBusType: RabbitMQ=0, Kafka=1, Redis=2）。
    private static string NormalizeEventBusType(string raw) =>
        raw?.Trim().ToLowerInvariant() switch
        {
            "0" or "rabbitmq" => "rabbitmq",
            "1" or "kafka" => "kafka",
            "2" or "redis" => "redis",
            var other => other
        };
}

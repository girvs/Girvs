namespace Girvs.Aspire.Hosting.Contributors;

public class EventBusResourceContributor : IGirvsResourceContributor
{
    public string ModuleTypeFullName => "Girvs.EventBus.EventBusModule";

    public void Contribute(GirvsOrchestrationContext context)
    {
        var eventBusType = context.ServiceSettings[
            "ModuleConfigurations:EventBusConfig:EventBusType"
        ];

        switch (eventBusType?.ToLowerInvariant())
        {
            case "rabbitmq":
                var rabbit = context.GetOrAddResource(
                    "girvs-eventbus-rabbitmq",
                    () => context.Builder.AddRabbitMQ("girvs-eventbus-rabbitmq")
                );
                context.Project.WithReference(rabbit).WaitFor(rabbit);
                break;
            case "redis":
                var redis = context.GetOrAddResource(
                    "girvs-eventbus-redis",
                    () => context.Builder.AddRedis("girvs-eventbus-redis")
                );
                context.Project.WithReference(redis).WaitFor(redis);
                break;
            default:
                // Kafka（云集群直连）或配置缺失：不自动创建，可按 girvs-* 命名约定手动补资源
                Console.Error.WriteLine(
                    $"[Girvs.Aspire.Hosting] 服务 {context.Project.Resource.Name} 的 EventBusType 为 '{eventBusType ?? "未配置"}'，未自动创建事件总线资源"
                );
                break;
        }
    }
}

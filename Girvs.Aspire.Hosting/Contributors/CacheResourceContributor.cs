namespace Girvs.Aspire.Hosting.Contributors;

public class CacheResourceContributor : IGirvsResourceContributor
{
    public string ModuleTypeFullName => "Girvs.Cache.GirvsCacheModule";

    public void Contribute(GirvsOrchestrationContext context)
    {
        // 默认共享实例；UseIsolatedCache 时为服务创建独立实例。
        // 注入名固定为 girvs-cache，与服务端 AspireConnectionStringMapper 约定对齐。
        var resourceName = context.Options.UseIsolatedCache
            ? $"girvs-cache-{context.Project.Resource.Name}"
            : "girvs-cache";

        if (context.Builder.ExecutionContext.IsPublishMode)
        {
            // 生产：引用阿里云托管 Redis（连接串由部署参数/配置提供），不在集群内新建容器
            var external = context.GetOrAddResource(
                resourceName,
                () => context.Builder.AddConnectionString(resourceName)
            );
            context.Project.WithReference(external, connectionName: "girvs-cache");
            return;
        }

        var redis = context.GetOrAddResource(
            resourceName,
            () => context.Builder.AddRedis(resourceName)
        );
        context.Project.WithReference(redis, connectionName: "girvs-cache").WaitFor(redis);
    }
}

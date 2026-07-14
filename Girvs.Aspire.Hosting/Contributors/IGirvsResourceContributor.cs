namespace Girvs.Aspire.Hosting.Contributors;

public interface IGirvsResourceContributor
{
    /// <summary>匹配的 Girvs 模块类型全名，如 "Girvs.Cache.GirvsCacheModule"</summary>
    string ModuleTypeFullName { get; }

    void Contribute(GirvsOrchestrationContext context);
}

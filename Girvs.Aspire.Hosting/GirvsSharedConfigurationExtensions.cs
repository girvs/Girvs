using System.Runtime.CompilerServices;

namespace Girvs.Aspire.Hosting;

public static class GirvsSharedConfigurationExtensions
{
    // 按 builder 实例暂存共享配置（AppHost 生命周期内一份），AddGirvsProject 再取回接线到各服务。
    // 用 ConditionalWeakTable 避免给 IDistributedApplicationBuilder 加自定义状态，也避免静态字段跨多个 builder 串味。
    private static readonly ConditionalWeakTable<
        IDistributedApplicationBuilder,
        GirvsSharedConfiguration
    > Store = new();

    /// <summary>声明跨服务共享配置；所有随后 AddGirvsProject 的服务自动接收。</summary>
    public static IDistributedApplicationBuilder AddGirvsSharedConfiguration(
        this IDistributedApplicationBuilder builder,
        Action<GirvsSharedConfiguration> configure
    )
    {
        ArgumentNullException.ThrowIfNull(configure);
        var shared = new GirvsSharedConfiguration();
        configure(shared);
        Store.AddOrUpdate(builder, shared);
        return builder;
    }

    /// <summary>取回已声明的共享配置；未声明时返回空实例（各服务不注入任何共享项）。</summary>
    public static GirvsSharedConfiguration GetShared(IDistributedApplicationBuilder builder) =>
        Store.TryGetValue(builder, out var shared) ? shared : new GirvsSharedConfiguration();
}

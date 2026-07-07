using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Serialization;

namespace Girvs.EventBus.CapEventBus;

/// <summary>
/// 复用 CAP 为每条消息创建的作用域，解决后台消费时 DbContext 被并发共享的问题。
/// <para>
/// CAP 的 <see cref="SubscribeInvoker.InvokeAsync"/> 本身会为每条消息 CreateAsyncScope 并从该作用域解析 handler；
/// 此处仅在官方扩展点 <see cref="GetInstance"/> 中，把该作用域的 ServiceProvider 桥接到 Girvs 的环境服务定位器
/// （<see cref="EngineContext"/>），使 handler 内经 EngineContext.Resolve 取得的 Scoped 服务（如 DbContext）
/// 落在本次消费的独立作用域内。不新建作用域，也不新增过滤器/中间件。
/// </para>
/// </summary>
public class GirvsSubscribeInvoker(IServiceProvider serviceProvider, ISerializer serializer)
    : SubscribeInvoker(serviceProvider, serializer)
{
    protected override object GetInstance(IServiceProvider provider, ConsumerContext context)
    {
        // provider 即 CAP 在 InvokeAsync 中通过 CreateAsyncScope 建立的、本条消息专属的作用域。
        EngineContext.Current.SetCurrentThreadServiceProvider(provider);
        return base.GetInstance(provider, context);
    }
}

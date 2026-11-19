using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Serialization;

namespace Girvs.EventBus.CapEventBus;

/// <summary>
/// 在 handler 构造期桥接 CAP 每消息作用域的订阅调用器。
/// <para>
/// CAP 的 <see cref="SubscribeInvoker.InvokeAsync"/> 为每条消息 CreateAsyncScope，
/// 并经由 <see cref="GetInstance"/>（参数即该消息专属作用域的 ServiceProvider）创建 handler。
/// 本类仅在这段同步代码内把该作用域桥接到 <see cref="EngineContext"/>，
/// 使 handler 构造函数中经 EngineContext.Resolve 取得的服务落在本消息作用域；
/// 桥接随 using 在 GetInstance 返回后即还原，不覆盖 handler 的 Handle 执行全程。
/// </para>
/// <para>
/// 贯穿 Handle 全程的 EngineContext 桥接与身份恢复由 GirvsCapFilter 完成
/// （其构造注入的 IServiceProvider 即 CAP 每消息作用域的 provider）。
/// 本类须在 AddCap 之前注册（CAP 内部 TryAddSingleton，先到先得）。
/// </para>
/// </summary>
public class GirvsSubscribeInvoker(IServiceProvider serviceProvider, ISerializer serializer)
    : SubscribeInvoker(serviceProvider, serializer)
{
    protected override object GetInstance(IServiceProvider provider, ConsumerContext context)
    {
        // provider 即 CAP 在 InvokeAsync 中通过 CreateAsyncScope 建立的、本条消息专属的作用域。
        using var _ = EngineContext.Current.ChangeCurrentThreadServiceProvider(provider);
        return base.GetInstance(provider, context);
    }
}

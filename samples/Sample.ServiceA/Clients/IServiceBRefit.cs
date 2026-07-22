using Girvs.Refit;
using Refit;

namespace Sample.ServiceA.Clients;

// 服务名与 AppHost 中声明的资源名一致；具体发现提供者由 RefitConfig 决定。
[RefitService("service-b", RefitServiceAddressType.ServiceDiscovery)]
public interface IServiceBRefit : IGirvsRefit
{
    [Get("/ping")]
    Task<string> PingAsync();
}

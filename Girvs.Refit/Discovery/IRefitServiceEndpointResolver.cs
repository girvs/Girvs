namespace Girvs.Refit.Discovery;

public interface IRefitServiceEndpointResolver
{
    // 解析器只处理自己负责的地址来源，处理器据此从 DI 集合中选择实现。
    bool CanResolve(RefitServiceAddressType addressType);

    Task<Uri> ResolveAsync(string serviceName, string? endpointName, CancellationToken cancellationToken);
}

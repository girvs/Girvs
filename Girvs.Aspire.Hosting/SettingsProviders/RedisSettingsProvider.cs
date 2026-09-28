

namespace Girvs.Aspire.Hosting.SettingsProviders;

/// <summary>内置预设:Redis 容器资源 → Type=redis,Settings.Endpoints。</summary>
public class RedisSettingsProvider : IGirvsResourceSettingsProvider
{
    public virtual async Task<GirvsInfrastructureResource> TryBuildAsync(IResource resource, CancellationToken ct)
    {
        if (resource is not RedisResource redis)
            return null;

        var (host, port) = redis.PrimaryEndpoint();
        var endpoints = $"{host}:{port}";
        // DistributedCacheConfig.BuildRedisConnectionString 会把 Endpoints 按逗号拆分后原样拼回,
        // 所以密码以 StackExchange.Redis 选项形式附在 Endpoints 内即可透传,模块无需感知。
        if (redis.PasswordParameter is not null)
            endpoints += $",password={await redis.PasswordParameter.GetValueAsync(ct)}";
        return new GirvsInfrastructureResource
        {
            Type = "redis",
            Settings = new Dictionary<string, string> { ["Endpoints"] = endpoints },
        };
    }
}

namespace Girvs.Aspire.Hosting;

/// <summary>AddGirvsProject 的可选配置。</summary>
public class GirvsProjectOptions
{
    /// <summary>
    /// 为该服务创建独立 Redis 缓存实例（girvs-cache-&lt;service&gt;）；
    /// 默认 false，多服务共享 girvs-cache 实例。
    /// </summary>
    public bool UseIsolatedCache { get; set; }
}

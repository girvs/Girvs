using Girvs.Configuration;

namespace Girvs.SignalR.Configuration;

public class SignalRConfig : IAppModuleConfig
{
    public void Init()
    {
    }

    /// <summary>
    /// Redis 资源引用：索引 0 为 Resources 资源键格式串，索引 1 起为 string.Format 参数；
    /// 同一组参数既用于组合资源键，也用于格式化资源的 Settings:Endpoints。
    /// 例如 ["girvs-redis", "1"] 会引用资源 girvs-redis，并把 Endpoints 中的 {0} 替换为 1。
    /// 为空、未配置或首项为空白时不启用 Redis Backplane。
    /// </summary>
    public string[] RedisConnectionRef { get; set; }
}

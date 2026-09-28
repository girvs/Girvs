using Girvs.Configuration;

namespace Girvs.SignalR.Configuration;

public class SignalRConfig :  IAppModuleConfig
{
    public void Init()
    {
    }

    /// <summary>引用 Resources 中的 Redis 资源键；为空时不启用 Redis Backplane。</summary>
    public string RedisConnectionRef { get; set; }
}

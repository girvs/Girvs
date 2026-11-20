using Girvs.Configuration;

namespace Girvs.Gateway;

/// <summary>网关模块配置。</summary>
public class GirvsGatewayConfig : IAppModuleConfig
{
    /// <summary>
    /// YARP 转发空闲超时，即两次转发 I/O 操作之间允许的最大间隔。
    /// 默认 5 分钟，大文件上传或下载场景可按需调大。
    /// </summary>
    public TimeSpan ActivityTimeout { get; set; } = TimeSpan.FromMinutes(5);

    public void Init() { }
}

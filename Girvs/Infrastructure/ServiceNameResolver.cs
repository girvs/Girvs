namespace Girvs;

/// <summary>统一生成服务发现与网关使用的服务名。</summary>
public static class ServiceNameResolver
{
    /// <summary>根据当前应用程序域的程序集名生成服务名。</summary>
    public static string FromAssemblyName() =>
        FromAssemblyName(AppDomain.CurrentDomain.FriendlyName);

    /// <summary>根据服务程序集名生成服务名。</summary>
    public static string FromAssemblyName(string assemblyName) =>
        assemblyName.Replace(".", "-").ToLowerInvariant();

    /// <summary>根据 Aspire 项目元数据类型名生成服务名。</summary>
    public static string FromProjectMetadataName(string projectMetadataName) =>
        projectMetadataName.Replace("_", "-").ToLowerInvariant();

    /// <summary>根据配置的 ServerName 生成规范化服务名：. 与 _ 转 -、全部小写；对规范配置幂等。</summary>
    public static string FromServerName(string serverName) =>
        serverName.Replace(".", "-").Replace("_", "-").ToLowerInvariant();
}

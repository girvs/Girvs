namespace Girvs;

/// <summary>统一生成服务发现与网关使用的服务名。</summary>
public static class ServiceNameResolver
{
    /// <summary>根据服务程序集名生成服务名。</summary>
    public static string FromAssemblyName(string assemblyName) =>
        assemblyName.Replace(".", "-").ToLowerInvariant();

    /// <summary>根据 Aspire 项目元数据类型名生成服务名。</summary>
    public static string FromProjectMetadataName(string projectMetadataName) =>
        projectMetadataName.Replace("_", "-").ToLowerInvariant();
}

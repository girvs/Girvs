namespace Girvs.Aspire.Hosting.Tests;

public class ServiceNameResolverTests
{
    [Fact]
    public void FromAssemblyName_未指定程序集名_使用当前应用程序域名称()
    {
        var expected = ServiceNameResolver.FromAssemblyName(AppDomain.CurrentDomain.FriendlyName);

        Assert.Equal(expected, ServiceNameResolver.FromAssemblyName());
    }

    [Theory]
    [InlineData("Sample.ServiceA", "sample-servicea")]
    [InlineData("Sample.ServiceB", "sample-serviceb")]
    public void FromAssemblyName_程序集名_生成统一服务名(string assemblyName, string expected)
    {
        Assert.Equal(expected, ServiceNameResolver.FromAssemblyName(assemblyName));
    }

    [Theory]
    [InlineData("Sample_ServiceA", "sample-servicea")]
    [InlineData("Sample_ServiceB", "sample-serviceb")]
    public void FromProjectMetadataName_项目元数据类型名_生成统一服务名(
        string projectMetadataName,
        string expected)
    {
        Assert.Equal(expected, ServiceNameResolver.FromProjectMetadataName(projectMetadataName));
    }

    [Theory]
    [InlineData("Sample.ServiceA", "sample-servicea")]
    [InlineData("My_Service_01", "my-service-01")]
    [InlineData("sample-servicea", "sample-servicea")]
    public void FromServerName_服务名配置_生成统一服务名(string serverName, string expected) =>
        Assert.Equal(expected, ServiceNameResolver.FromServerName(serverName));
}

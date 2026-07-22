namespace Girvs.Aspire.Hosting.Tests;

public class ServiceNameResolverTests
{
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
}

using System.Reflection;
using Girvs;
using Xunit;

namespace Girvs.Core.Tests;

public class GirvsSerilogSinkRegistryTests
{
    [Fact]
    public void 注册表提供SnapshotAndClear方法()
    {
        var registryType = typeof(GirvsHostBuilderManager).Assembly.GetType(
            "Girvs.GirvsSerilogSinkRegistry"
        );

        Assert.NotNull(registryType);
        Assert.NotNull(
            registryType.GetMethod(
                "SnapshotAndClear",
                BindingFlags.Static | BindingFlags.NonPublic
            )
        );
    }

    [Fact]
    public void 提供AddSerilogSink扩展方法()
    {
        var extensionType = typeof(GirvsHostBuilderManager).Assembly.GetType(
            "Girvs.Infrastructure.Extensions.SerilogSinkServiceCollectionExtensions"
        );

        Assert.NotNull(extensionType);
        Assert.Contains(extensionType.GetMethods(), method => method.Name == "AddSerilogSink");
    }
}

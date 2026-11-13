using Girvs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Girvs.Core.Tests;

public class HostConfigTests
{
    [Fact]
    public void HostUseGirvsConfig_环境变量源_加入配置()
    {
        const string variableName = "GIRVS_HOST_CONFIG_TEST";
        Environment.SetEnvironmentVariable(variableName, "host-value");

        try
        {
            var builder = new ConfigurationBuilder();

            builder.HostUseGirvsConfig(new FakeWebHostEnvironment());

            var configuration = builder.Build();
            Assert.Equal("host-value", configuration[variableName]);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, null);
        }
    }

    [Fact]
    public void HostUseGirvsConfig_命令行参数_优先于环境变量()
    {
        const string variableName = "GIRVS_HOST_CONFIG_TEST";
        Environment.SetEnvironmentVariable(variableName, "from-env");

        try
        {
            var builder = new ConfigurationBuilder();

            builder.HostUseGirvsConfig(
                new FakeWebHostEnvironment(),
                args: new[] { $"--{variableName}=from-args" }
            );

            var configuration = builder.Build();
            Assert.Equal("from-args", configuration[variableName]);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, null);
        }
    }

    private sealed class FakeWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Girvs.Core.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; }
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

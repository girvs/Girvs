using Girvs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
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

    [Fact]
    public void HostUseGirvsConfig_Json占位符_解析环境变量且保留可重载配置源()
    {
        const string variableName = "GIRVS_HOST_PLACEHOLDER_TEST";
        Environment.SetEnvironmentVariable(variableName, "resolved");

        var tempDirectory = Path.Combine(Path.GetTempPath(), $"girvs-host-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectory);
        File.WriteAllText(
            Path.Combine(tempDirectory, "appsettings.json"),
            "{\"Section\":{\"Key\":\"prefix-${" + variableName + "}-suffix\"}}"
        );

        try
        {
            var builder = new ConfigurationBuilder();

            // AddJsonFile 的相对路径由 builder 的 FileProvider 解析，避免依赖进程当前目录。
            builder.Properties["FileProvider"] = new PhysicalFileProvider(tempDirectory);

            builder.HostUseGirvsConfig(new FakeWebHostEnvironment());

            // 修复前 Sources 会被替换为单个 ChainedConfigurationProvider，reloadOnChange 随之失效。
            Assert.Contains(builder.Sources, source => source is JsonConfigurationSource);

            var configuration = builder.Build();
            Assert.Equal("prefix-resolved-suffix", configuration["Section:Key"]);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, null);
            Directory.Delete(tempDirectory, true);
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

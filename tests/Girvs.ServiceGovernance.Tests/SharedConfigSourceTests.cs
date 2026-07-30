using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace Girvs.ServiceGovernance.Tests;

/// <summary>
/// 验证 GIRVS_SHARED_CONFIG 共享配置源:作为最低优先级加载,服务本地配置覆盖共享值。
/// 测试通过进程环境变量与 AppContext.BaseDirectory 下的 appsettings.json 驱动,
/// 全部用例放在同一个 class 内以保证串行执行,避免进程级状态互相污染。
/// </summary>
public class SharedConfigSourceTests : IDisposable
{
    private const string EnvName = "GIRVS_SHARED_CONFIG";
    private readonly string _sharedFile = Path.Combine(
        Path.GetTempPath(), $"girvs-shared-{Guid.NewGuid():N}.json");
    private readonly string _localAppSettings = Path.Combine(
        AppContext.BaseDirectory, "appsettings.json");

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(EnvName, null);
        if (File.Exists(_sharedFile)) File.Delete(_sharedFile);
        if (File.Exists(_localAppSettings)) File.Delete(_localAppSettings);
    }

    private static IConfiguration BuildConfig()
    {
        var builder = new ConfigurationBuilder();
        builder.HostUseGirvsConfig(new TestWebHostEnvironment());
        return builder.Build();
    }

    [Fact]
    public void 共享文件被加载()
    {
        File.WriteAllText(_sharedFile,
            """{"Resources":{"platform-redis":{"Type":"redis","Settings":{"Endpoints":"shared:6379"}}}}""");
        Environment.SetEnvironmentVariable(EnvName, _sharedFile);

        var config = BuildConfig();

        Assert.Equal("shared:6379", config["Resources:platform-redis:Settings:Endpoints"]);
    }

    [Fact]
    public void 服务本地appsettings覆盖共享值()
    {
        File.WriteAllText(_sharedFile,
            """{"Resources":{"platform-redis":{"Type":"redis","Settings":{"Endpoints":"shared:6379"}}}}""");
        File.WriteAllText(_localAppSettings,
            """{"Resources":{"platform-redis":{"Type":"redis","Settings":{"Endpoints":"local:6379"}}}}""");
        Environment.SetEnvironmentVariable(EnvName, _sharedFile);

        var config = BuildConfig();

        Assert.Equal("local:6379", config["Resources:platform-redis:Settings:Endpoints"]);
    }

    [Fact]
    public void 未设置环境变量时不加载共享文件()
    {
        Environment.SetEnvironmentVariable(EnvName, null);

        var config = BuildConfig();

        Assert.Null(config["Resources:platform-redis:Settings:Endpoints"]);
    }

    [Fact]
    public void 共享文件不存在时静默降级()
    {
        Environment.SetEnvironmentVariable(EnvName,
            Path.Combine(Path.GetTempPath(), "girvs-not-exists.json"));

        var config = BuildConfig(); // 不抛异常即通过

        Assert.Null(config["Resources:platform-redis:Settings:Endpoints"]);
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Testing";
        public string ApplicationName { get; set; } = "";
        public string WebRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

using Girvs;
using Girvs.Configuration;
using Xunit;

namespace Girvs.Core.Tests;

public class AppSettingsTests
{
    [Fact]
    public void Get_配置系统未初始化_抛出可识别的异常()
    {
        var appSettings = new AppSettings();

        var exception = Assert.Throws<GirvsException>(() => appSettings.Get<CommonConfig>());

        Assert.Contains("尚未初始化", exception.Message);
    }

    [Fact]
    public void Get_模块配置不存在_异常包含配置类型名()
    {
        var appSettings = new AppSettings();
        appSettings.PreLoadModelConfig();

        var exception = Assert.Throws<GirvsException>(() => appSettings.Get<CommonConfig>());

        Assert.Contains(nameof(CommonConfig), exception.Message);
    }

    [Fact]
    public void TryGet_已注册的模块配置_返回实例()
    {
        var appSettings = new AppSettings();
        appSettings.PreLoadModelConfig();
        var commonConfig = new CommonConfig {DisplayFullErrorStack = true};
        appSettings.ModuleConfigurations[nameof(CommonConfig)] = commonConfig;

        Assert.True(appSettings.TryGet<CommonConfig>(out var config));
        Assert.Same(commonConfig, config);
        Assert.Same(commonConfig, appSettings.Get<CommonConfig>());
    }

    [Fact]
    public void TryGet_未初始化或类型不匹配_返回false且不抛异常()
    {
        var uninitialized = new AppSettings();
        Assert.False(uninitialized.TryGet<CommonConfig>(out var missing));
        Assert.Null(missing);

        var initialized = new AppSettings();
        initialized.PreLoadModelConfig();
        // 同名键但类型不同，视为取不到，避免 InvalidCastException。
        initialized.ModuleConfigurations[nameof(CommonConfig)] = new HostingConfig();

        Assert.False(initialized.TryGet<CommonConfig>(out var mismatched));
        Assert.Null(mismatched);
    }

    [Fact]
    public void Get_按名称_未初始化或键不存在_返回null()
    {
        var appSettings = new AppSettings();
        object notInitialized = appSettings.Get("NotExists");
        Assert.Null(notInitialized);

        appSettings.PreLoadModelConfig();
        object missingKey = appSettings.Get("NotExists");
        Assert.Null(missingKey);
    }
}

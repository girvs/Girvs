using System.Collections.Generic;
using Girvs;
using Girvs.Configuration;
using Girvs.Configuration.Resources;
using Girvs.Infrastructure;
using Xunit;

namespace Girvs.Core.Tests;

[Collection(EngineContextCollection.Name)]
public class InfrastructureResourceReferenceResolverTests
{
    private static Dictionary<string, GirvsInfrastructureResource> CreateResources(
        string key,
        string type = "redis",
        string endpoints = "host:6379,ssl=False,defaultdatabase={0}"
    ) =>
        new()
        {
            [key] = new()
            {
                Type = type,
                Settings = new Dictionary<string, string> {["Endpoints"] = endpoints},
            },
        };

    [Fact]
    public void 资源引用_未配置或首项为空白_返回false()
    {
        Assert.False(InfrastructureResourceReferenceResolver.IsConfigured(null));
        Assert.False(InfrastructureResourceReferenceResolver.IsConfigured([]));
        Assert.False(InfrastructureResourceReferenceResolver.IsConfigured([""]));
        Assert.False(InfrastructureResourceReferenceResolver.IsConfigured([" "]));
        Assert.True(InfrastructureResourceReferenceResolver.IsConfigured(["girvs-redis"]));
    }

    [Fact]
    public void 格式化资源键_按数组参数组合()
    {
        var resourceKey = InfrastructureResourceReferenceResolver.FormatResourceKey([
            "girvs-redis-{0}",
            "1",
        ]);

        Assert.Equal("girvs-redis-1", resourceKey);
    }

    [Fact]
    public void 格式化资源键_引用未配置_抛出可识别异常()
    {
        var exception = Assert.Throws<GirvsException>(() =>
            InfrastructureResourceReferenceResolver.FormatResourceKey(null)
        );

        Assert.Contains("资源引用未配置", exception.Message);
    }

    [Fact]
    public void 获取必需设置_资源键与设置值共用数组参数()
    {
        var reference = new[] {"girvs-redis-{0}", "1"};
        var resources = CreateResources("girvs-redis-1");

        var connectionString = InfrastructureResourceReferenceResolver.GetRequiredSetting(
            resources,
            reference,
            "Endpoints"
        );

        Assert.Equal("host:6379,ssl=False,defaultdatabase=1", connectionString);
    }

    [Fact]
    public void 获取必需设置_资源键无占位符时参数仅用于设置值()
    {
        var reference = new[] {"girvs-redis", "3"};
        var resources = CreateResources("girvs-redis");

        var connectionString = InfrastructureResourceReferenceResolver.GetRequiredSetting(
            resources,
            reference,
            "Endpoints"
        );

        Assert.Equal("host:6379,ssl=False,defaultdatabase=3", connectionString);
    }

    [Fact]
    public void 获取必需设置_设置值无占位符时保留原值()
    {
        var reference = new[] {"girvs-redis"};
        var resources = CreateResources("girvs-redis", endpoints: "host:6379");

        var connectionString = InfrastructureResourceReferenceResolver.GetRequiredSetting(
            resources,
            reference,
            "Endpoints"
        );

        Assert.Equal("host:6379", connectionString);
    }

    [Fact]
    public void 获取必需资源_资源不存在_抛出可识别异常()
    {
        var exception = Assert.Throws<GirvsException>(() =>
            InfrastructureResourceReferenceResolver.GetRequiredResource(
                new Dictionary<string, GirvsInfrastructureResource>(),
                ["missing"]
            )
        );

        Assert.Contains("Resources:missing 未配置", exception.Message);
    }

    [Fact]
    public void 获取必需资源_类型不匹配_抛出可识别异常()
    {
        var resources = CreateResources("mysql", type: "mysql", endpoints: "Server=mysql");

        var exception = Assert.Throws<GirvsException>(() =>
            InfrastructureResourceReferenceResolver.GetRequiredResource(
                resources,
                ["mysql"],
                "redis"
            )
        );

        Assert.Contains("Resources:mysql:Type 必须为 redis", exception.Message);
    }

    [Fact]
    public void 获取必需设置_设置未配置_抛出可识别异常()
    {
        var resources = new Dictionary<string, GirvsInfrastructureResource>
        {
            ["girvs-redis"] = new()
            {
                Type = "redis",
                Settings = new Dictionary<string, string>(),
            },
        };

        var exception = Assert.Throws<GirvsException>(() =>
            InfrastructureResourceReferenceResolver.GetRequiredSetting(
                resources,
                ["girvs-redis"],
                "Endpoints"
            )
        );

        Assert.Contains("Resources:girvs-redis:Settings:Endpoints 未配置", exception.Message);
    }

    [Fact]
    public void 获取必需设置_参数不足_抛出可识别异常()
    {
        var resources = CreateResources("girvs-redis");

        var exception = Assert.Throws<GirvsException>(() =>
            InfrastructureResourceReferenceResolver.GetRequiredSetting(
                resources,
                ["girvs-redis"],
                "Endpoints"
            )
        );

        Assert.Contains("格式串与参数不匹配", exception.Message);
    }

    [Fact]
    public void 获取必需设置_未初始化AppSettings_抛出可识别异常()
    {
        var previous = Singleton<AppSettings>.Instance;
        try
        {
            Singleton<AppSettings>.Instance = null;

            var exception = Assert.Throws<GirvsException>(() =>
                InfrastructureResourceReferenceResolver.GetRequiredSetting(
                    ["girvs-redis"],
                    "Endpoints"
                )
            );

            Assert.Contains("AppSettings 尚未初始化", exception.Message);
        }
        finally
        {
            Singleton<AppSettings>.Instance = previous;
        }
    }

    [Fact]
    public void 获取必需设置_从AppSettings单例解析资源()
    {
        var previous = Singleton<AppSettings>.Instance;
        try
        {
            Singleton<AppSettings>.Instance = new AppSettings
            {
                Resources = CreateResources("girvs-redis"),
            };

            var connectionString = InfrastructureResourceReferenceResolver.GetRequiredSetting(
                ["girvs-redis", "2"],
                "Endpoints"
            );

            Assert.Equal("host:6379,ssl=False,defaultdatabase=2", connectionString);
        }
        finally
        {
            Singleton<AppSettings>.Instance = previous;
        }
    }
}

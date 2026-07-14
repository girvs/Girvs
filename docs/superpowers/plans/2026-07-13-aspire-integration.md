# Girvs 引入 .NET Aspire 实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 新增 `Girvs.Aspire` 模块（仅 net10.0），提供 ServiceDefaults（OpenTelemetry、健康检查、Aspire 服务发现、HttpClient 弹性）、Serilog→OTLP 日志桥接与 Aspire 连接串到 Girvs 配置体系的自动映射。

**Architecture:** `Girvs.Aspire` 是标准 Girvs 模块（`AspireModule : IAppModuleStartup`，`Order = -10000`），引用即生效；连接串映射通过 `AppSettings.ModuleConfigurations` 字典 + 反射写入，避免对 EFCore/Cache/EventBus 的硬引用；唯一的核心改动是 `GirvsHostBuilderManager.HostUseSerilog` 内反射探测 `GirvsAspireSerilogHook` 追加 OTLP sink。

**Tech Stack:** .NET 10、OpenTelemetry 1.13+、Microsoft.Extensions.ServiceDiscovery 13.x、Serilog.Sinks.OpenTelemetry、DotNetCore.Cap.OpenTelemetry 10.0.1、xunit。

**设计文档:** `docs/superpowers/specs/2026-07-13-aspire-integration-design.md`（实施前先通读）

## Global Constraints

- `Girvs.Aspire` 仅目标 `net10.0`（csproj 中 `<TargetFrameworks>net10.0</TargetFrameworks>` 覆盖 `Directory.Build.props` 的三 TFM 设置）。
- 不改动 `Girvs.Consul`、`Girvs.Cache`、`Girvs.EventBus`、`Girvs.EntityFrameworkCore` 的任何代码。
- `Girvs` 核心模块只允许改 `GirvsHostBuilderManager.cs`，且不新增任何包依赖。
- `DotNetCore.Cap.OpenTelemetry` 版本必须与 `Girvs.EventBus` net10 分支的 CAP 版本一致（当前 `10.0.1`）。
- 计划中给出的其他 NuGet 包版本号为参考下限，执行时用 `dotnet add package <id>`（不带版本号）拉取最新稳定版；若网络不可用则按计划中的版本号写死。
- 版本号沿用 `Directory.Build.props` 的统一值（当前 `10.0.0-rc.1`），本计划不改版本号。
- 提交时用 `git add <具体路径>`，禁止 `git add -A`（仓库中存在无关的 `.omo/` 目录）。
- 每次提交信息用中文，遵循仓库现有风格。
- 全程验证命令：`dotnet build Girvs.slnx`（三 TFM 全量编译必须通过）与 `dotnet test tests/Girvs.Aspire.Tests/Girvs.Aspire.Tests.csproj`。

---

### Task 1: Girvs.Aspire 项目骨架与测试工程

**Files:**
- Create: `Girvs.Aspire/Girvs.Aspire.csproj`
- Create: `Girvs.Aspire/GlobalUsings.cs`
- Create: `tests/Girvs.Aspire.Tests/Girvs.Aspire.Tests.csproj`
- Create: `tests/Girvs.Aspire.Tests/GlobalUsings.cs`
- Modify: `Girvs.slnx`

**Interfaces:**
- Consumes: 现有 `Girvs.csproj`（核心模块）、`Directory.Build.props`（统一版本与 TFM）。
- Produces: 可编译的空模块工程与测试工程，后续 Task 2-4 的代码全部放入这两个工程。

- [ ] **Step 1: 创建 Girvs.Aspire 工程文件**

`Girvs.Aspire/Girvs.Aspire.csproj`：

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFrameworks>net10.0</TargetFrameworks>
        <Description>Girvs 框架的 .NET Aspire 集成模块：OpenTelemetry 可观测性、健康检查、Aspire 服务发现、HttpClient 弹性与 Aspire 连接串配置适配。</Description>
    </PropertyGroup>

    <ItemGroup>
        <FrameworkReference Include="Microsoft.AspNetCore.App"/>
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\Girvs\Girvs.csproj"/>
    </ItemGroup>

    <ItemGroup>
        <PackageReference Include="Microsoft.Extensions.ServiceDiscovery" Version="13.0.0"/>
        <PackageReference Include="Microsoft.Extensions.Http.Resilience" Version="10.0.0"/>
        <PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.13.1"/>
        <PackageReference Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.13.0"/>
        <PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.13.0"/>
        <PackageReference Include="OpenTelemetry.Instrumentation.Runtime" Version="1.13.0"/>
        <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.13.1"/>
        <PackageReference Include="Serilog.Sinks.OpenTelemetry" Version="4.2.0"/>
        <PackageReference Include="DotNetCore.Cap.OpenTelemetry" Version="10.0.1"/>
    </ItemGroup>
</Project>
```

注意：包版本按 Global Constraints 的规则处理（优先 `dotnet add package` 最新稳定版；`DotNetCore.Cap.OpenTelemetry` 固定 `10.0.1` 与 EventBus 对齐）。

`Girvs.Aspire/GlobalUsings.cs`（参照 `Girvs.Consul/GlobalUsings.cs` 的风格，按字母排序）：

```csharp
// Global using directives

global using System;
global using System.Collections;
global using System.Collections.Generic;
global using System.Linq;
global using System.Reflection;
global using Girvs.Aspire.Configuration;
global using Girvs.Configuration;
global using Girvs.Infrastructure;
global using Microsoft.AspNetCore.Builder;
global using Microsoft.AspNetCore.Diagnostics.HealthChecks;
global using Microsoft.AspNetCore.Hosting;
global using Microsoft.AspNetCore.Routing;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Diagnostics.HealthChecks;
global using OpenTelemetry;
global using OpenTelemetry.Metrics;
global using OpenTelemetry.Trace;
global using Serilog;
```

- [ ] **Step 2: 创建测试工程**

`tests/Girvs.Aspire.Tests/Girvs.Aspire.Tests.csproj`：

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFrameworks>net10.0</TargetFrameworks>
        <IsPackable>false</IsPackable>
        <GeneratePackageOnBuild>false</GeneratePackageOnBuild>
    </PropertyGroup>

    <ItemGroup>
        <FrameworkReference Include="Microsoft.AspNetCore.App"/>
    </ItemGroup>

    <ItemGroup>
        <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1"/>
        <PackageReference Include="xunit" Version="2.9.3"/>
        <PackageReference Include="xunit.runner.visualstudio" Version="3.1.0"/>
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\..\Girvs.Aspire\Girvs.Aspire.csproj"/>
        <ProjectReference Include="..\..\Girvs.Cache\Girvs.Cache.csproj"/>
        <ProjectReference Include="..\..\Girvs.EntityFrameworkCore\Girvs.EntityFrameworkCore.csproj"/>
        <ProjectReference Include="..\..\Girvs.EventBus\Girvs.EventBus.csproj"/>
    </ItemGroup>
</Project>
```

说明：测试工程引用真实模块工程以使用真实配置类型（`DbConfig`/`CacheConfig`/`EventBusConfig`）做映射断言；`Girvs.Aspire` 本身不引用这些模块（这是设计约束，禁止在 Girvs.Aspire.csproj 中加这三个 ProjectReference）。

`tests/Girvs.Aspire.Tests/GlobalUsings.cs`：

```csharp
// Global using directives

global using System;
global using System.Collections.Generic;
global using System.Linq;
global using Girvs.Aspire;
global using Girvs.Aspire.Configuration;
global using Girvs.Configuration;
global using Girvs.Infrastructure;
global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.DependencyInjection;
global using Xunit;
```

- [ ] **Step 3: 将两个工程加入解决方案**

修改 `Girvs.slnx`，在 `<Project Path="Girvs.AuthorizePermission/..." />` 之后按字母序插入两行：

```xml
  <Project Path="Girvs.Aspire/Girvs.Aspire.csproj" />
```

并在末尾 `<Project Path="Girvs/Girvs.csproj" />` 之后加：

```xml
  <Project Path="tests/Girvs.Aspire.Tests/Girvs.Aspire.Tests.csproj" />
```

- [ ] **Step 4: 编译验证**

Run: `dotnet build Girvs.slnx`
Expected: Build succeeded，无错误。若 `TargetFrameworks` 覆盖不生效导致 net8/net9 编译 Girvs.Aspire 失败，检查 csproj 中是 `<TargetFrameworks>net10.0</TargetFrameworks>`（复数属性单值覆盖），不要用单数 `TargetFramework`。

- [ ] **Step 5: 提交**

```bash
git add Girvs.Aspire tests/Girvs.Aspire.Tests Girvs.slnx
git commit -m "新增 Girvs.Aspire 模块骨架与首个测试工程"
```

---

### Task 2: 连接串配置适配层 AspireConnectionStringMapper

**Files:**
- Create: `Girvs.Aspire/Configuration/AspireConnectionStringMapper.cs`
- Test: `tests/Girvs.Aspire.Tests/AspireConnectionStringMapperTests.cs`

**Interfaces:**
- Consumes: `Girvs.Configuration.AppSettings`（`ModuleConfigurations` 字典、`PreLoadModelConfig()`）；`IConfiguration.GetConnectionString(name)`。
- Produces: `public static void AspireConnectionStringMapper.Apply(IConfiguration configuration, AppSettings appSettings)` — Task 4 的 `AspireModule.ConfigureServices` 调用它。

**映射约定（与设计文档 3.1 一致）：**

| 连接串名 | 目标 |
|---|---|
| `girvs-db-<name>` | `DbConfig.DataConnectionConfigs` 中 `Name == <name>` 项的 `MasterDataConnectionString` |
| `girvs-db-<name>-read-<N>`（N 从 0 连续） | 同名项的 `ReadDataConnectionString` 列表 |
| `girvs-cache` | `CacheConfig.DistributedCacheConfig.ConnectionString` |
| `girvs-eventbus-db` | `EventBusConfig.DbConnectionString` |
| `girvs-eventbus-redis` | `EventBusConfig.RedisConfig.RedisConnectionString` |
| `girvs-eventbus-rabbitmq` | AMQP URI 解析到 `EventBusConfig.RabbitMqConfig` 的 HostName/Port/UserName/Password/VirtualHost |

- [ ] **Step 1: 编写失败的测试**

`tests/Girvs.Aspire.Tests/AspireConnectionStringMapperTests.cs`：

```csharp
using Girvs.Cache.Configuration;
using Girvs.EntityFrameworkCore.Configuration;
using Girvs.EventBus.Configuration;

namespace Girvs.Aspire.Tests;

public class AspireConnectionStringMapperTests
{
    private static AppSettings CreateAppSettings(params IConfig[] configs)
    {
        var appSettings = new AppSettings();
        appSettings.PreLoadModelConfig();
        foreach (var config in configs)
        {
            appSettings.ModuleConfigurations.Add(config.GetType().Name, config);
        }

        return appSettings;
    }

    private static IConfiguration BuildConfiguration(
        params (string Name, string Value)[] connectionStrings
    )
    {
        var data = connectionStrings.ToDictionary(
            x => $"ConnectionStrings:{x.Name}",
            x => x.Value
        );
        return new ConfigurationBuilder().AddInMemoryCollection(data).Build();
    }

    [Fact]
    public void 主库连接串按名称映射到对应的数据连接配置()
    {
        var dbConfig = new DbConfig();
        dbConfig.DataConnectionConfigs.Add(new DataConnectionConfig { Name = "default" });
        var appSettings = CreateAppSettings(dbConfig);
        var configuration = BuildConfiguration(
            ("girvs-db-default", "Server=aspire-host;database=demo;")
        );

        AspireConnectionStringMapper.Apply(configuration, appSettings);

        Assert.Equal(
            "Server=aspire-host;database=demo;",
            dbConfig.DataConnectionConfigs.First().MasterDataConnectionString
        );
    }

    [Fact]
    public void 读库连接串按连续编号全部映射()
    {
        var dbConfig = new DbConfig();
        dbConfig.DataConnectionConfigs.Add(new DataConnectionConfig { Name = "default" });
        var appSettings = CreateAppSettings(dbConfig);
        var configuration = BuildConfiguration(
            ("girvs-db-default-read-0", "Server=read0;"),
            ("girvs-db-default-read-1", "Server=read1;")
        );

        AspireConnectionStringMapper.Apply(configuration, appSettings);

        var readList = dbConfig.DataConnectionConfigs.First().ReadDataConnectionString;
        Assert.Equal(2, readList.Count);
        Assert.Equal("Server=read0;", readList[0]);
        Assert.Equal("Server=read1;", readList[1]);
    }

    [Fact]
    public void 无对应连接串时保持配置原值()
    {
        var dbConfig = new DbConfig();
        dbConfig.DataConnectionConfigs.Add(
            new DataConnectionConfig { Name = "default", MasterDataConnectionString = "原值" }
        );
        var appSettings = CreateAppSettings(dbConfig);
        var configuration = BuildConfiguration();

        AspireConnectionStringMapper.Apply(configuration, appSettings);

        Assert.Equal("原值", dbConfig.DataConnectionConfigs.First().MasterDataConnectionString);
    }

    [Fact]
    public void 缓存连接串映射到分布式缓存配置()
    {
        var cacheConfig = new CacheConfig();
        var appSettings = CreateAppSettings(cacheConfig);
        var configuration = BuildConfiguration(("girvs-cache", "aspire-redis:6379"));

        AspireConnectionStringMapper.Apply(configuration, appSettings);

        Assert.Equal("aspire-redis:6379", cacheConfig.DistributedCacheConfig.ConnectionString);
    }

    [Fact]
    public void 事件总线数据库与Redis连接串分别映射()
    {
        var eventBusConfig = new EventBusConfig();
        var appSettings = CreateAppSettings(eventBusConfig);
        var configuration = BuildConfiguration(
            ("girvs-eventbus-db", "Server=cap-db;"),
            ("girvs-eventbus-redis", "cap-redis:6379")
        );

        AspireConnectionStringMapper.Apply(configuration, appSettings);

        Assert.Equal("Server=cap-db;", eventBusConfig.DbConnectionString);
        Assert.Equal("cap-redis:6379", eventBusConfig.RedisConfig.RedisConnectionString);
    }

    [Fact]
    public void RabbitMq的AMQP连接串解析到各字段()
    {
        var eventBusConfig = new EventBusConfig();
        var appSettings = CreateAppSettings(eventBusConfig);
        var configuration = BuildConfiguration(
            ("girvs-eventbus-rabbitmq", "amqp://guest:secret@rabbit-host:5673/myvhost")
        );

        AspireConnectionStringMapper.Apply(configuration, appSettings);

        Assert.Equal("rabbit-host", eventBusConfig.RabbitMqConfig.HostName);
        Assert.Equal(5673, eventBusConfig.RabbitMqConfig.Port);
        Assert.Equal("guest", eventBusConfig.RabbitMqConfig.UserName);
        Assert.Equal("secret", eventBusConfig.RabbitMqConfig.Password);
        Assert.Equal("myvhost", eventBusConfig.RabbitMqConfig.VirtualHost);
    }

    [Fact]
    public void RabbitMq连接串非URI格式时按纯主机名处理()
    {
        var eventBusConfig = new EventBusConfig();
        var originalPort = eventBusConfig.RabbitMqConfig.Port;
        var appSettings = CreateAppSettings(eventBusConfig);
        var configuration = BuildConfiguration(("girvs-eventbus-rabbitmq", "rabbit-host"));

        AspireConnectionStringMapper.Apply(configuration, appSettings);

        Assert.Equal("rabbit-host", eventBusConfig.RabbitMqConfig.HostName);
        Assert.Equal(originalPort, eventBusConfig.RabbitMqConfig.Port);
    }

    [Fact]
    public void 模块配置不存在时不抛异常()
    {
        var appSettings = CreateAppSettings();
        var configuration = BuildConfiguration(
            ("girvs-db-default", "Server=x;"),
            ("girvs-cache", "y:6379")
        );

        var exception = Record.Exception(
            () => AspireConnectionStringMapper.Apply(configuration, appSettings)
        );

        Assert.Null(exception);
    }
}
```

注意：`IConfig` 位于 `Girvs.Configuration` 命名空间（已在 GlobalUsings 中）。若 `Record.Exception` 不可用，改用 try/catch + `Assert.True(true)` 结构。

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test tests/Girvs.Aspire.Tests/Girvs.Aspire.Tests.csproj`
Expected: 编译失败，报 `AspireConnectionStringMapper` 不存在（CS0103/CS0246）。

- [ ] **Step 3: 编写实现**

`Girvs.Aspire/Configuration/AspireConnectionStringMapper.cs`：

```csharp
namespace Girvs.Aspire.Configuration;

/// <summary>
/// 将 Aspire AppHost 注入的 ConnectionStrings__* 环境变量映射到 Girvs 各模块配置节。
/// 通过 AppSettings.ModuleConfigurations 字典 + 反射按属性名写入，
/// 避免对 EntityFrameworkCore/Cache/EventBus 模块产生硬引用。
/// </summary>
public static class AspireConnectionStringMapper
{
    public static void Apply(IConfiguration configuration, AppSettings appSettings)
    {
        if (configuration == null || appSettings?.ModuleConfigurations == null)
            return;

        MapDbConfig(configuration, appSettings);
        MapCacheConfig(configuration, appSettings);
        MapEventBusConfig(configuration, appSettings);
    }

    private static void MapDbConfig(IConfiguration configuration, AppSettings appSettings)
    {
        if (!appSettings.ModuleConfigurations.TryGetValue("DbConfig", out var dbConfig))
            return;

        if (GetPropertyValue(dbConfig, "DataConnectionConfigs") is not IEnumerable connectionConfigs)
            return;

        foreach (var connectionConfig in connectionConfigs)
        {
            if (GetPropertyValue(connectionConfig, "Name") is not string name || name.Length == 0)
                continue;

            var master = configuration.GetConnectionString($"girvs-db-{name}");
            if (!string.IsNullOrEmpty(master))
                SetPropertyValue(connectionConfig, "MasterDataConnectionString", master);

            var readConnections = new List<string>();
            for (var i = 0; ; i++)
            {
                var read = configuration.GetConnectionString($"girvs-db-{name}-read-{i}");
                if (string.IsNullOrEmpty(read))
                    break;
                readConnections.Add(read);
            }

            if (readConnections.Count > 0)
                SetPropertyValue(connectionConfig, "ReadDataConnectionString", readConnections);
        }
    }

    private static void MapCacheConfig(IConfiguration configuration, AppSettings appSettings)
    {
        var redisConnection = configuration.GetConnectionString("girvs-cache");
        if (string.IsNullOrEmpty(redisConnection))
            return;

        if (!appSettings.ModuleConfigurations.TryGetValue("CacheConfig", out var cacheConfig))
            return;

        var distributedCacheConfig = GetPropertyValue(cacheConfig, "DistributedCacheConfig");
        if (distributedCacheConfig != null)
            SetPropertyValue(distributedCacheConfig, "ConnectionString", redisConnection);
    }

    private static void MapEventBusConfig(IConfiguration configuration, AppSettings appSettings)
    {
        if (!appSettings.ModuleConfigurations.TryGetValue("EventBusConfig", out var eventBusConfig))
            return;

        var dbConnection = configuration.GetConnectionString("girvs-eventbus-db");
        if (!string.IsNullOrEmpty(dbConnection))
            SetPropertyValue(eventBusConfig, "DbConnectionString", dbConnection);

        var redisConnection = configuration.GetConnectionString("girvs-eventbus-redis");
        if (!string.IsNullOrEmpty(redisConnection))
        {
            var redisConfig = GetPropertyValue(eventBusConfig, "RedisConfig");
            if (redisConfig != null)
                SetPropertyValue(redisConfig, "RedisConnectionString", redisConnection);
        }

        var amqpUri = configuration.GetConnectionString("girvs-eventbus-rabbitmq");
        if (!string.IsNullOrEmpty(amqpUri))
        {
            var rabbitMqConfig = GetPropertyValue(eventBusConfig, "RabbitMqConfig");
            if (rabbitMqConfig != null)
                MapRabbitMq(rabbitMqConfig, amqpUri);
        }
    }

    private static void MapRabbitMq(object rabbitMqConfig, string amqpUri)
    {
        if (
            !Uri.TryCreate(amqpUri, UriKind.Absolute, out var uri)
            || !uri.Scheme.StartsWith("amqp", StringComparison.OrdinalIgnoreCase)
        )
        {
            // 非 AMQP URI 格式时视为纯主机名，其余字段保持 appsettings 原值
            SetPropertyValue(rabbitMqConfig, "HostName", amqpUri);
            return;
        }

        SetPropertyValue(rabbitMqConfig, "HostName", uri.Host);
        if (uri.Port > 0)
            SetPropertyValue(rabbitMqConfig, "Port", uri.Port);

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            var userInfo = uri.UserInfo.Split(':', 2);
            if (userInfo[0].Length > 0)
                SetPropertyValue(rabbitMqConfig, "UserName", Uri.UnescapeDataString(userInfo[0]));
            if (userInfo.Length == 2)
                SetPropertyValue(rabbitMqConfig, "Password", Uri.UnescapeDataString(userInfo[1]));
        }

        var virtualHost = uri.AbsolutePath.TrimStart('/');
        if (virtualHost.Length > 0)
            SetPropertyValue(rabbitMqConfig, "VirtualHost", Uri.UnescapeDataString(virtualHost));
    }

    private static object GetPropertyValue(object target, string propertyName) =>
        target?.GetType().GetProperty(propertyName)?.GetValue(target);

    private static void SetPropertyValue(object target, string propertyName, object value)
    {
        var property = target?.GetType().GetProperty(propertyName);
        if (property is { CanWrite: true })
            property.SetValue(target, value);
    }
}
```

- [ ] **Step 4: 运行测试确认通过**

Run: `dotnet test tests/Girvs.Aspire.Tests/Girvs.Aspire.Tests.csproj`
Expected: 全部测试 PASS。

- [ ] **Step 5: 提交**

```bash
git add Girvs.Aspire/Configuration/AspireConnectionStringMapper.cs tests/Girvs.Aspire.Tests/AspireConnectionStringMapperTests.cs
git commit -m "新增 Aspire 连接串到 Girvs 模块配置的自动映射"
```

---

### Task 3: Serilog OTLP 桥接与核心挂接

**Files:**
- Create: `Girvs.Aspire/Logging/GirvsAspireSerilogHook.cs`
- Modify: `Girvs/GirvsHostBuilderManager.cs`（`HostUseSerilog` 方法，当前约 34-42 行）
- Test: `tests/Girvs.Aspire.Tests/GirvsAspireSerilogHookTests.cs`

**Interfaces:**
- Consumes: Serilog 的 `LoggerConfiguration`（核心 Girvs 已引用 Serilog）。
- Produces: 反射契约 `Girvs.Aspire.GirvsAspireSerilogHook.AddOtlpSink(LoggerConfiguration)`（public static，类型全名与方法名是跨程序集契约，禁止改名）。核心 `HostUseSerilog` 在检测到 `OTEL_EXPORTER_OTLP_ENDPOINT` 环境变量且 Girvs.Aspire 程序集存在时调用它。

- [ ] **Step 1: 编写失败的测试**

`tests/Girvs.Aspire.Tests/GirvsAspireSerilogHookTests.cs`：

```csharp
using Serilog;

namespace Girvs.Aspire.Tests;

public class GirvsAspireSerilogHookTests
{
    [Fact]
    public void 反射契约_核心能通过类型全名找到AddOtlpSink方法()
    {
        var hookType = Type.GetType("Girvs.Aspire.GirvsAspireSerilogHook, Girvs.Aspire");

        Assert.NotNull(hookType);
        var method = hookType.GetMethod("AddOtlpSink", new[] { typeof(LoggerConfiguration) });
        Assert.NotNull(method);
        Assert.True(method.IsStatic);
    }

    [Fact]
    public void 未设置OTLP端点环境变量时不追加Sink也不抛异常()
    {
        var original = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
        try
        {
            Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", null);
            var loggerConfiguration = new LoggerConfiguration();

            var exception = Record.Exception(
                () => GirvsAspireSerilogHook.AddOtlpSink(loggerConfiguration)
            );

            Assert.Null(exception);
        }
        finally
        {
            Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", original);
        }
    }

    [Fact]
    public void 设置OTLP端点环境变量时追加Sink不抛异常()
    {
        var original = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
        try
        {
            Environment.SetEnvironmentVariable(
                "OTEL_EXPORTER_OTLP_ENDPOINT",
                "http://localhost:4317"
            );
            var loggerConfiguration = new LoggerConfiguration();

            var exception = Record.Exception(
                () => GirvsAspireSerilogHook.AddOtlpSink(loggerConfiguration)
            );

            Assert.Null(exception);
        }
        finally
        {
            Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", original);
        }
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test tests/Girvs.Aspire.Tests/Girvs.Aspire.Tests.csproj`
Expected: 编译失败，报 `GirvsAspireSerilogHook` 不存在。

- [ ] **Step 3: 编写实现**

`Girvs.Aspire/Logging/GirvsAspireSerilogHook.cs`：

```csharp
using Serilog.Sinks.OpenTelemetry;

namespace Girvs.Aspire;

/// <summary>
/// 由 Girvs 核心的 HostUseSerilog 通过反射调用。
/// 契约：类型全名 Girvs.Aspire.GirvsAspireSerilogHook 与方法签名 AddOtlpSink(LoggerConfiguration) 不可改动，
/// 改动时必须同步修改 GirvsHostBuilderManager.TryAddGirvsAspireOtlpSink。
/// </summary>
public static class GirvsAspireSerilogHook
{
    public const string OtlpEndpointVariable = "OTEL_EXPORTER_OTLP_ENDPOINT";

    public static void AddOtlpSink(LoggerConfiguration loggerConfiguration)
    {
        var endpoint = Environment.GetEnvironmentVariable(OtlpEndpointVariable);
        if (string.IsNullOrEmpty(endpoint))
            return;

        loggerConfiguration.WriteTo.OpenTelemetry(options =>
        {
            options.Endpoint = endpoint;
            options.Protocol = GetProtocol();

            var serviceName = Environment.GetEnvironmentVariable("OTEL_SERVICE_NAME");
            if (!string.IsNullOrEmpty(serviceName))
            {
                options.ResourceAttributes = new Dictionary<string, object>
                {
                    ["service.name"] = serviceName
                };
            }
        });
    }

    private static OtlpProtocol GetProtocol()
    {
        var protocol = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL");
        return protocol?.ToLowerInvariant() switch
        {
            "http/protobuf" or "http" => OtlpProtocol.HttpProtobuf,
            _ => OtlpProtocol.Grpc
        };
    }
}
```

修改 `Girvs/GirvsHostBuilderManager.cs` 的 `HostUseSerilog`，并新增私有方法：

```csharp
    public static void HostUseSerilog(this IHostBuilder hostBuilder)
    {
        hostBuilder.UseSerilog(
            (context, configuration) =>
            {
                configuration.ReadFrom.Configuration(context.Configuration);
                TryAddGirvsAspireOtlpSink(configuration);
            }
        );
    }

    /// <summary>
    /// 反射探测 Girvs.Aspire（仅 net10 包），存在且处于 Aspire 环境时追加 OTLP sink，否则静默跳过。
    /// 契约：Girvs.Aspire.GirvsAspireSerilogHook.AddOtlpSink(LoggerConfiguration)
    /// </summary>
    private static void TryAddGirvsAspireOtlpSink(LoggerConfiguration loggerConfiguration)
    {
        if (
            string.IsNullOrEmpty(
                Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")
            )
        )
            return;

        var hookType = Type.GetType("Girvs.Aspire.GirvsAspireSerilogHook, Girvs.Aspire");
        var method = hookType?.GetMethod("AddOtlpSink", new[] { typeof(LoggerConfiguration) });
        method?.Invoke(null, new object[] { loggerConfiguration });
    }
```

注意：`GirvsHostBuilderManager.cs` 顶部已有 `using Serilog;`，`LoggerConfiguration` 无需额外 using。此改动对 net8/net9 同样编译（`Type.GetType` 对缺失程序集返回 null），不需要条件编译。

- [ ] **Step 4: 运行测试与全量编译**

Run: `dotnet test tests/Girvs.Aspire.Tests/Girvs.Aspire.Tests.csproj && dotnet build Girvs.slnx`
Expected: 测试全部 PASS；三 TFM 全量编译成功。

- [ ] **Step 5: 提交**

```bash
git add Girvs.Aspire/Logging/GirvsAspireSerilogHook.cs Girvs/GirvsHostBuilderManager.cs tests/Girvs.Aspire.Tests/GirvsAspireSerilogHookTests.cs
git commit -m "Serilog 通过 OTLP 桥接到 Aspire Dashboard"
```

---

### Task 4: AspireModule（ServiceDefaults 模块入口）

**Files:**
- Create: `Girvs.Aspire/AspireModule.cs`
- Test: `tests/Girvs.Aspire.Tests/AspireModuleTests.cs`

**Interfaces:**
- Consumes: `IAppModuleStartup`（Girvs 核心接口：`ConfigureServices(IServiceCollection, IConfiguration)`、`Configure(IApplicationBuilder, IWebHostEnvironment)`、`ConfigureMapEndpointRoute(IEndpointRouteBuilder)`、`int Order`）；`Singleton<AppSettings>.Instance`（模块执行时已由 `AddBindAppModelConfiguation` 完成绑定）；Task 2 的 `AspireConnectionStringMapper.Apply`。
- Produces: `AspireModule`，被 Girvs 引擎自动发现并执行；`/health` 与 `/alive` 端点。

- [ ] **Step 1: 编写失败的测试**

`tests/Girvs.Aspire.Tests/AspireModuleTests.cs`：

```csharp
using Girvs.EntityFrameworkCore.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Girvs.Aspire.Tests;

public class AspireModuleTests
{
    private static IConfiguration BuildConfiguration(
        params (string Name, string Value)[] connectionStrings
    )
    {
        var data = connectionStrings.ToDictionary(
            x => $"ConnectionStrings:{x.Name}",
            x => x.Value
        );
        return new ConfigurationBuilder().AddInMemoryCollection(data).Build();
    }

    private static AppSettings SetupSingletonAppSettings(params IConfig[] configs)
    {
        var appSettings = new AppSettings();
        appSettings.PreLoadModelConfig();
        foreach (var config in configs)
        {
            appSettings.ModuleConfigurations.Add(config.GetType().Name, config);
        }

        Singleton<AppSettings>.Instance = appSettings;
        return appSettings;
    }

    [Fact]
    public void 模块Order为极小值确保先于其他模块执行()
    {
        Assert.True(new AspireModule().Order < 0);
    }

    [Fact]
    public void ConfigureServices注册健康检查服务()
    {
        SetupSingletonAppSettings();
        var services = new ServiceCollection();
        var configuration = BuildConfiguration();

        new AspireModule().ConfigureServices(services, configuration);

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(HealthCheckService)
        );
    }

    [Fact]
    public void ConfigureServices执行连接串映射()
    {
        var dbConfig = new DbConfig();
        dbConfig.DataConnectionConfigs.Add(new DataConnectionConfig { Name = "default" });
        SetupSingletonAppSettings(dbConfig);
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(("girvs-db-default", "Server=from-aspire;"));

        new AspireModule().ConfigureServices(services, configuration);

        Assert.Equal(
            "Server=from-aspire;",
            dbConfig.DataConnectionConfigs.First().MasterDataConnectionString
        );
    }

    [Fact]
    public void 未设置OTLP端点时不注册OpenTelemetry()
    {
        var original = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
        try
        {
            Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", null);
            SetupSingletonAppSettings();
            var services = new ServiceCollection();

            new AspireModule().ConfigureServices(services, BuildConfiguration());

            Assert.DoesNotContain(
                services,
                descriptor =>
                    descriptor.ImplementationType?.FullName?.Contains(
                        "OpenTelemetry",
                        StringComparison.Ordinal
                    ) == true
            );
        }
        finally
        {
            Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", original);
        }
    }
}
```

注意：`Singleton<T>` 位于 `Girvs.Infrastructure`（若测试编译报找不到，向测试 GlobalUsings 追加 `global using Girvs.Infrastructure;`）。

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test tests/Girvs.Aspire.Tests/Girvs.Aspire.Tests.csproj`
Expected: 编译失败，报 `AspireModule` 不存在。

- [ ] **Step 3: 编写实现**

`Girvs.Aspire/AspireModule.cs`：

```csharp
namespace Girvs.Aspire;

/// <summary>
/// Aspire 集成模块：连接串映射、OpenTelemetry、Aspire 服务发现、HttpClient 弹性与健康检查。
/// Order 取极小值，确保连接串映射先于其他模块消费配置。
/// </summary>
public class AspireModule : IAppModuleStartup
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        AspireConnectionStringMapper.Apply(configuration, Singleton<AppSettings>.Instance);

        services.AddServiceDiscovery();
        services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
        });

        services
            .AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" });

        if (
            !string.IsNullOrEmpty(
                Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")
            )
        )
        {
            services
                .AddOpenTelemetry()
                .WithMetrics(metrics =>
                    metrics
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddRuntimeInstrumentation()
                )
                .WithTracing(tracing =>
                {
                    tracing.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation();
                    if (IsEventBusModuleLoaded())
                        tracing.AddCapInstrumentation();
                })
                .UseOtlpExporter();
        }

        WarnWhenConsulCoexists();
    }

    public void Configure(IApplicationBuilder application, IWebHostEnvironment env) { }

    public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder)
    {
        builder.MapHealthChecks("/health");
        builder.MapHealthChecks(
            "/alive",
            new HealthCheckOptions
            {
                Predicate = registration => registration.Tags.Contains("live")
            }
        );
    }

    public int Order => -10000;

    private static bool IsEventBusModuleLoaded() =>
        Type.GetType("Girvs.EventBus.EventBusModule, Girvs.EventBus") != null;

    private static void WarnWhenConsulCoexists()
    {
        if (Type.GetType("Girvs.Consul.ConsulModule, Girvs.Consul") != null)
        {
            Log.Warning(
                "检测到 Girvs.Consul 与 Girvs.Aspire 同时启用：两者的服务发现机制互斥，请仅保留其一（迁移期间可忽略此警告）"
            );
        }
    }
}
```

可能的编译问题排查：
- `AddCapInstrumentation` 来自 `DotNetCore.Cap.OpenTelemetry` 包，扩展方法在 `OpenTelemetry.Trace` 命名空间（GlobalUsings 已含）；若方法名不匹配，用 `dotnet nuget` 拉包后查看该包的公开 API 并改用实际方法名。
- `UseOtlpExporter()` 来自 `OpenTelemetry.Exporter.OpenTelemetryProtocol` ≥1.9 的跨信号 Exporter API，命名空间 `OpenTelemetry`（GlobalUsings 已含）。
- `HealthCheckOptions` 在 `Microsoft.AspNetCore.Diagnostics.HealthChecks`（GlobalUsings 已含）。
- `IAppModuleStartup` 若接口成员与上述签名不符（以 `Girvs/Infrastructure/IAppModuleStartup.cs` 为准），按实际接口调整。

- [ ] **Step 4: 运行测试与全量编译**

Run: `dotnet test tests/Girvs.Aspire.Tests/Girvs.Aspire.Tests.csproj && dotnet build Girvs.slnx`
Expected: 测试全部 PASS；全量编译成功。

- [ ] **Step 5: 提交**

```bash
git add Girvs.Aspire/AspireModule.cs tests/Girvs.Aspire.Tests/AspireModuleTests.cs
git commit -m "新增 AspireModule 提供 ServiceDefaults 能力"
```

---

### Task 5: 发布脚本与接入文档

**Files:**
- Modify: `nugetpublish.ps1`（`$modules` 数组，约 14-28 行）
- Create: `docs/aspire/apphost-guide.md`
- Modify: `README.md`（模块列表处追加一行；找到现有模块表格追加）
- Modify: `CLAUDE.md`（模块架构表追加 `Girvs.Aspire` 一行）

**Interfaces:**
- Consumes: Task 1-4 产出的模块与约定（资源命名 `girvs-db-<name>`、`girvs-cache` 等）。
- Produces: 发布链路与业务方接入文档。

- [ ] **Step 1: 修改发布脚本**

`nugetpublish.ps1` 的 `$modules` 数组中，在 `'Girvs',` 之后按字母序插入：

```powershell
    'Girvs.Aspire',
```

- [ ] **Step 2: 编写 AppHost 接入文档**

创建 `docs/aspire/apphost-guide.md`，内容如下（完整写入）：

````markdown
# Girvs 服务接入 .NET Aspire 指南

适用范围：目标框架为 net10.0 的 Girvs 服务。net8/net9 服务请继续使用 Girvs.Consul。

## 1. 服务端接入

1. 服务项目追加包引用：

```xml
<PackageReference Include="Girvs.Aspire" Version="10.0.0-rc.1" />
```

2. 无需修改任何代码与配置文件。`AspireModule` 会随 Girvs 模块机制自动生效：
   - 在 Aspire 环境（存在 `OTEL_EXPORTER_OTLP_ENDPOINT` 环境变量）下自动上报日志/追踪/指标到 Dashboard；
   - 自动把 AppHost 注入的连接串映射到 Girvs 各模块配置（见下文命名约定）；
   - 暴露 `/health` 与 `/alive` 健康检查端点；
   - HttpClient 默认启用 Aspire 服务发现与标准弹性策略。
3. 与 Girvs.Consul 互斥：迁移到 Aspire 服务发现后，移除 Girvs.Consul 包引用与相关配置。

## 2. AppHost 项目（业务方自建）

新建一个 Aspire AppHost 项目（`dotnet new aspire-apphost`），示例 `Program.cs`：

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// 资源名必须遵循 Girvs 约定，才能被自动映射到模块配置
var mysql = builder.AddMySql("mysql");
var db = mysql.AddDatabase("girvs-db-default");

var redis = builder.AddRedis("girvs-cache");
var rabbit = builder.AddRabbitMQ("girvs-eventbus-rabbitmq");

builder
    .AddProject<Projects.My_Girvs_Service>("my-girvs-service")
    .WithReference(db)
    .WithReference(redis)
    .WithReference(rabbit);

builder.Build().Run();
```

## 3. 资源命名约定

| Aspire 资源名 | 映射到的 Girvs 配置 |
|---|---|
| `girvs-db-<name>` | `DbConfig.DataConnectionConfigs` 中 `Name == <name>` 项的主库连接串（默认名为 `default`） |
| `girvs-db-<name>-read-<N>`（N 从 0 起连续编号） | 同名项的读库连接串列表 |
| `girvs-cache` | `CacheConfig.DistributedCacheConfig.ConnectionString` |
| `girvs-eventbus-db` | `EventBusConfig.DbConnectionString` |
| `girvs-eventbus-redis` | `EventBusConfig.RedisConfig.RedisConnectionString` |
| `girvs-eventbus-rabbitmq` | AMQP URI，解析到 `EventBusConfig.RabbitMqConfig`（HostName/Port/UserName/Password/VirtualHost） |

没有按约定命名的资源不会被映射，对应配置保持 appsettings.json 原值。

## 4. 常见问题

- **Dashboard 里看不到日志？** 确认服务由 AppHost 启动（`OTEL_EXPORTER_OTLP_ENDPOINT` 由 Aspire 自动注入）；框架保留 Serilog，OTLP 是追加的 sink，本地文件/控制台日志不受影响。
- **与 Consul 共存告警？** 迁移期间可忽略；完成迁移后移除 Girvs.Consul。
- **数据库连接串格式？** Aspire 注入的是标准 ADO.NET 连接串，与 Girvs 现有格式一致，直接覆盖主库连接串。
````

- [ ] **Step 3: 更新 README 与 CLAUDE.md 模块列表**

`README.md`：找到模块介绍列表/表格（搜索 `Girvs.Consul`），紧邻处按现有格式追加一行：`Girvs.Aspire`——".NET Aspire 集成：OpenTelemetry 可观测性、健康检查、服务发现、连接串自动映射（仅 net10.0）"，并在合适位置链接 `docs/aspire/apphost-guide.md`。

`CLAUDE.md`：在"模块架构"表格 `Girvs.Consul` 行之前追加：

```markdown
| `Girvs.Aspire` | .NET Aspire 集成（仅 net10.0）：OpenTelemetry、健康检查、Aspire 服务发现、连接串映射 |
```

- [ ] **Step 4: 验证脚本语法**

Run: `pwsh -NoProfile -Command "& { $c = Get-Content ./nugetpublish.ps1 -Raw; [System.Management.Automation.PSParser]::Tokenize($c, [ref]$null) | Out-Null; 'OK' }"`（若本机无 pwsh 则跳过，人工复查数组语法逗号）
Expected: 输出 OK 或人工确认 `$modules` 数组语法正确。

- [ ] **Step 5: 提交**

```bash
git add nugetpublish.ps1 docs/aspire/apphost-guide.md README.md CLAUDE.md
git commit -m "新增 Aspire 接入文档并将 Girvs.Aspire 纳入发布脚本"
```

---

### Task 6: 收尾验证

**Files:**
- 无新增；只跑验证。

**Interfaces:**
- Consumes: Task 1-5 的全部产出。
- Produces: 可发布状态的分支。

- [ ] **Step 1: 全量清理重建**

Run: `dotnet clean Girvs.slnx && dotnet build Girvs.slnx --no-incremental`
Expected: Build succeeded；`Girvs.Aspire/bin/Debug/` 下仅生成 net10.0 产物与 nupkg；`Girvs/bin/Debug/` 下 net8.0/net9.0/net10.0 三套产物齐全。

- [ ] **Step 2: 全量测试**

Run: `dotnet test tests/Girvs.Aspire.Tests/Girvs.Aspire.Tests.csproj`
Expected: 全部 PASS，0 failed。

- [ ] **Step 3: Release 构建验证打包**

Run: `dotnet build Girvs.slnx -c Release`
Expected: Build succeeded；`Girvs.Aspire/bin/Release/Girvs.Aspire.10.0.0-rc.1.nupkg` 存在（版本号以 `Directory.Build.props` 当前值为准）。

- [ ] **Step 4: 检查未提交内容**

Run: `git status --short`
Expected: 除 `.omo/`（无关目录，不提交）外无未跟踪/未提交文件；若有遗漏文件按所属 Task 的提交粒度补提交。

**手工验证（需要用户环境，不阻塞本计划）：** 按 `docs/aspire/apphost-guide.md` 搭一个最小 AppHost + Girvs 示例服务，确认 Dashboard 中日志/追踪/指标可见、`/health` 返回 Healthy、连接串被正确覆盖。此步骤留给用户在带 Docker 的开发机上执行。

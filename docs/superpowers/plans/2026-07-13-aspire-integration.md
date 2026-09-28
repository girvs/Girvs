# Girvs 引入 .NET Aspire 实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

> **执行后修订（2026-07-14）：** 本计划已全部执行完成。Task 2/4 中"通过反射把连接串映射进 EFCore/Cache/EventBus 配置对象"的方案，在代码评审后改为**各组件模块自己实现并调用** `ApplyAspireConnectionString(s)` 方法（如 `CacheConfig.ApplyAspireConnectionString`），移除了 `Girvs.Aspire.Configuration.AspireConnectionStringMapper`。本文件保留作为原始执行记录，当前设计以 `docs/superpowers/specs/2026-07-13-aspire-integration-design.md` 为准。

**Goal:** 新增 `Girvs.Aspire`（服务端 ServiceDefaults + 连接串映射 + Serilog→OTLP 桥接）与 `Girvs.Aspire.Hosting`（AppHost 端：`AddGirvsProject` 依据 `[DependsOn]` 声明自动创建资源并接线）两个模块（均仅 net10.0）。

**Architecture:** `Girvs.Aspire` 是标准 Girvs 模块（`AspireModule : IAppModuleStartup`，`Order = -10000`），引用即生效；连接串映射通过 `AppSettings.ModuleConfigurations` 字典 + 反射写入，避免对 EFCore/Cache/EventBus 的硬引用；唯一的核心侵入改动是 `GirvsHostBuilderManager.HostUseSerilog` 内反射探测 `GirvsAspireSerilogHook` 追加 OTLP sink，以及核心新增 `DependsOnAttribute`。`Girvs.Aspire.Hosting` 在 AppHost 进程内递归遍历根模块的 `DependsOn` 图，按"模块类型全名 → 资源贡献器"注册表惰性创建共享资源（Redis/RabbitMQ/MySql/SqlServer），资源形态取自服务的 appsettings.json；资源命名遵循 `girvs-*` 约定，与服务端连接串映射自动对齐。

**Tech Stack:** .NET 10、OpenTelemetry 1.13+、Microsoft.Extensions.ServiceDiscovery 13.x、Aspire.Hosting 13.x（含 Redis/RabbitMQ/MySql/SqlServer 集成）、Serilog.Sinks.OpenTelemetry、DotNetCore.Cap.OpenTelemetry 10.0.1、xunit。

**设计文档:** `docs/superpowers/specs/2026-07-13-aspire-integration-design.md`（实施前先通读）

## Global Constraints

- `Girvs.Aspire` 与 `Girvs.Aspire.Hosting` 均仅目标 `net10.0`（csproj 中 `<TargetFrameworks>net10.0</TargetFrameworks>` 覆盖 `Directory.Build.props` 的三 TFM 设置）。
- 不改动 `Girvs.Consul`、`Girvs.Cache`、`Girvs.EventBus`、`Girvs.EntityFrameworkCore`、`Girvs.Quartz` 的任何代码。
- `Girvs` 核心模块只允许改 `GirvsHostBuilderManager.cs` 与新增 `DependsOnAttribute.cs`，且不新增任何包依赖。
- `Girvs.Aspire.Hosting` 只引用 `Girvs.csproj` 与 `Aspire.Hosting.*` 包，禁止引用任何 Girvs 组件工程（Cache/EventBus/EFCore 等）——模块按类型全名字符串匹配。
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

### Task 5: DependsOnAttribute（Girvs 核心）

**Files:**
- Create: `Girvs/DependsOnAttribute.cs`
- Modify: `tests/Girvs.Aspire.Tests/GlobalUsings.cs`（追加 `global using Girvs;`）
- Test: `tests/Girvs.Aspire.Tests/DependsOnAttributeTests.cs`

**Interfaces:**
- Consumes: 无（纯声明属性）。
- Produces: `Girvs.DependsOnAttribute`（`AttributeUsage(Class, AllowMultiple = true)`，构造参数 `params Type[]`，属性 `Type[] DependedModuleTypes`）— Task 7 的 `DependsOnGraph` 依赖它。

- [ ] **Step 1: 编写失败的测试**

在 `tests/Girvs.Aspire.Tests/GlobalUsings.cs` 追加一行 `global using Girvs;`（保持字母排序），然后创建 `tests/Girvs.Aspire.Tests/DependsOnAttributeTests.cs`：

```csharp
namespace Girvs.Aspire.Tests;

public class DependsOnAttributeTests
{
    [DependsOn(typeof(string), typeof(int))]
    private class FakeModule;

    private class NoDependsModule;

    [Fact]
    public void 属性保存声明的依赖模块类型()
    {
        var attribute = typeof(FakeModule)
            .GetCustomAttributes(typeof(DependsOnAttribute), false)
            .Cast<DependsOnAttribute>()
            .Single();

        Assert.Equal(new[] { typeof(string), typeof(int) }, attribute.DependedModuleTypes);
    }

    [Fact]
    public void 传入null时依赖列表为空数组()
    {
        var attribute = new DependsOnAttribute(null);

        Assert.NotNull(attribute.DependedModuleTypes);
        Assert.Empty(attribute.DependedModuleTypes);
    }

    [Fact]
    public void 未声明属性的类读取结果为空()
    {
        Assert.Empty(
            typeof(NoDependsModule).GetCustomAttributes(typeof(DependsOnAttribute), false)
        );
    }
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test tests/Girvs.Aspire.Tests/Girvs.Aspire.Tests.csproj`
Expected: 编译失败，报 `DependsOnAttribute` 不存在。

- [ ] **Step 3: 编写实现**

`Girvs/DependsOnAttribute.cs`：

```csharp
namespace Girvs;

/// <summary>
/// 声明模块间的依赖关系。业务服务定义根模块类并声明其使用的 Girvs 组件模块，
/// 由 Girvs.Aspire.Hosting 的 AddGirvsProject 读取以完成 AppHost 资源自动编排。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public class DependsOnAttribute : Attribute
{
    public Type[] DependedModuleTypes { get; }

    public DependsOnAttribute(params Type[] dependedModuleTypes)
    {
        DependedModuleTypes = dependedModuleTypes ?? Type.EmptyTypes;
    }
}
```

- [ ] **Step 4: 运行测试与全量编译**

Run: `dotnet test tests/Girvs.Aspire.Tests/Girvs.Aspire.Tests.csproj && dotnet build Girvs.slnx`
Expected: 测试全部 PASS；三 TFM 全量编译成功（属性代码无框架版本差异）。

- [ ] **Step 5: 提交**

```bash
git add Girvs/DependsOnAttribute.cs tests/Girvs.Aspire.Tests/DependsOnAttributeTests.cs tests/Girvs.Aspire.Tests/GlobalUsings.cs
git commit -m "核心新增 DependsOn 模块依赖声明属性"
```

---

### Task 6: Girvs.Aspire.Hosting 骨架与测试工程

**Files:**
- Create: `Girvs.Aspire.Hosting/Girvs.Aspire.Hosting.csproj`
- Create: `Girvs.Aspire.Hosting/GlobalUsings.cs`
- Create: `tests/Girvs.Aspire.Hosting.Tests/Girvs.Aspire.Hosting.Tests.csproj`
- Create: `tests/Girvs.Aspire.Hosting.Tests/GlobalUsings.cs`
- Modify: `Girvs.slnx`

**Interfaces:**
- Consumes: `Girvs.csproj`（取 `DependsOnAttribute`）、`Aspire.Hosting.*` 集成包。
- Produces: 可编译的 AppHost 端类库工程，Task 7 的编排代码放入其中。

- [ ] **Step 1: 创建 Hosting 工程文件**

`Girvs.Aspire.Hosting/Girvs.Aspire.Hosting.csproj`：

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFrameworks>net10.0</TargetFrameworks>
        <Description>Girvs 框架的 .NET Aspire AppHost 编排扩展：AddGirvsProject 依据 [DependsOn] 模块声明自动创建共享资源并接线。</Description>
    </PropertyGroup>

    <ItemGroup>
        <ProjectReference Include="..\Girvs\Girvs.csproj"/>
    </ItemGroup>

    <ItemGroup>
        <PackageReference Include="Aspire.Hosting" Version="13.0.0"/>
        <PackageReference Include="Aspire.Hosting.Redis" Version="13.0.0"/>
        <PackageReference Include="Aspire.Hosting.RabbitMQ" Version="13.0.0"/>
        <PackageReference Include="Aspire.Hosting.MySql" Version="13.0.0"/>
        <PackageReference Include="Aspire.Hosting.SqlServer" Version="13.0.0"/>
    </ItemGroup>

    <ItemGroup>
        <InternalsVisibleTo Include="Girvs.Aspire.Hosting.Tests"/>
    </ItemGroup>
</Project>
```

版本处理同 Global Constraints（`dotnet add package` 取最新稳定版，13.0.0 为参考下限）。注意：禁止引用 Girvs.Cache/EventBus/EntityFrameworkCore 等组件工程。

`Girvs.Aspire.Hosting/GlobalUsings.cs`：

```csharp
// Global using directives

global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Reflection;
global using Aspire.Hosting;
global using Aspire.Hosting.ApplicationModel;
global using Girvs.Aspire.Hosting.Contributors;
global using Microsoft.Extensions.Configuration;
```

- [ ] **Step 2: 创建测试工程**

`tests/Girvs.Aspire.Hosting.Tests/Girvs.Aspire.Hosting.Tests.csproj`：

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFrameworks>net10.0</TargetFrameworks>
        <IsPackable>false</IsPackable>
        <GeneratePackageOnBuild>false</GeneratePackageOnBuild>
    </PropertyGroup>

    <ItemGroup>
        <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.14.1"/>
        <PackageReference Include="xunit" Version="2.9.3"/>
        <PackageReference Include="xunit.runner.visualstudio" Version="3.1.0"/>
    </ItemGroup>

    <ItemGroup>
        <ProjectReference Include="..\..\Girvs.Aspire.Hosting\Girvs.Aspire.Hosting.csproj"/>
        <ProjectReference Include="..\..\Girvs.Cache\Girvs.Cache.csproj"/>
        <ProjectReference Include="..\..\Girvs.EntityFrameworkCore\Girvs.EntityFrameworkCore.csproj"/>
        <ProjectReference Include="..\..\Girvs.EventBus\Girvs.EventBus.csproj"/>
    </ItemGroup>
</Project>
```

说明：测试工程引用组件工程只为拿到真实模块类型（`GirvsCacheModule` 等）构造 `[DependsOn]` 声明，与 Hosting 包本身的"禁止引用"约束不冲突。

`tests/Girvs.Aspire.Hosting.Tests/GlobalUsings.cs`：

```csharp
// Global using directives

global using System;
global using System.IO;
global using System.Linq;
global using Aspire.Hosting;
global using Aspire.Hosting.ApplicationModel;
global using Girvs;
global using Girvs.Aspire.Hosting;
global using Xunit;
```

- [ ] **Step 3: 加入解决方案**

`Girvs.slnx` 中 `Girvs.Aspire` 行之后插入：

```xml
  <Project Path="Girvs.Aspire.Hosting/Girvs.Aspire.Hosting.csproj" />
```

`tests/Girvs.Aspire.Tests` 行之后插入：

```xml
  <Project Path="tests/Girvs.Aspire.Hosting.Tests/Girvs.Aspire.Hosting.Tests.csproj" />
```

- [ ] **Step 4: 编译验证**

Run: `dotnet build Girvs.slnx`
Expected: Build succeeded。

- [ ] **Step 5: 提交**

```bash
git add Girvs.Aspire.Hosting tests/Girvs.Aspire.Hosting.Tests Girvs.slnx
git commit -m "新增 Girvs.Aspire.Hosting 工程骨架与测试工程"
```

---

### Task 7: DependsOn 图遍历、资源注册表与 AddGirvsProject

**Files:**
- Create: `Girvs.Aspire.Hosting/DependsOnGraph.cs`
- Create: `Girvs.Aspire.Hosting/GirvsServiceAppSettings.cs`
- Create: `Girvs.Aspire.Hosting/GirvsOrchestrationContext.cs`
- Create: `Girvs.Aspire.Hosting/GirvsProjectOptions.cs`
- Create: `Girvs.Aspire.Hosting/Contributors/IGirvsResourceContributor.cs`
- Create: `Girvs.Aspire.Hosting/Contributors/CacheResourceContributor.cs`
- Create: `Girvs.Aspire.Hosting/Contributors/EventBusResourceContributor.cs`
- Create: `Girvs.Aspire.Hosting/Contributors/DatabaseResourceContributor.cs`
- Create: `Girvs.Aspire.Hosting/GirvsResourceRegistry.cs`
- Create: `Girvs.Aspire.Hosting/GirvsProjectExtensions.cs`
- Test: `tests/Girvs.Aspire.Hosting.Tests/DependsOnGraphTests.cs`
- Test: `tests/Girvs.Aspire.Hosting.Tests/GirvsProjectOrchestrationTests.cs`

**Interfaces:**
- Consumes: Task 5 的 `Girvs.DependsOnAttribute`；Aspire 的 `IDistributedApplicationBuilder`、`AddRedis/AddRabbitMQ/AddMySql/AddSqlServer`。
- Produces:
  - `public static IReadOnlyList<Type> DependsOnGraph.Collect(Type rootModuleType)`（不含根模块本身，去重防环）；
  - `public static IResourceBuilder<ProjectResource> AddGirvsProject<TProject>(this IDistributedApplicationBuilder builder, string name, Type rootModuleType, Action<GirvsProjectOptions> configure = null) where TProject : IProjectMetadata, new()`（业务方 AppHost 使用的唯一入口，Web API 与 Worker 项目均适用）；
  - `internal static IResourceBuilder<ProjectResource> WireGirvsResources(this IDistributedApplicationBuilder builder, IResourceBuilder<ProjectResource> project, string projectDirectory, Type rootModuleType, GirvsProjectOptions options = null)`（供测试直接调用）；
  - `public class GirvsProjectOptions { public bool UseIsolatedCache { get; set; } }`；
  - `public static void GirvsResourceRegistry.Register(IGirvsResourceContributor contributor)`（业务扩展点，同名覆盖）。

**资源命名与注入名分离（与架构图对齐的关键）**：Aspire 资源名须全局唯一（每服务独立库 → `girvs-db-<service>-<name>`；独立缓存 → `girvs-cache-<service>`），但注入到服务的连接串名通过 `WithReference(resource, connectionName)` 固定为服务端映射约定（`girvs-db-<name>`、`girvs-cache`），服务端 `AspireConnectionStringMapper` 无需任何改动。

- [ ] **Step 1: 编写失败的测试（图遍历）**

`tests/Girvs.Aspire.Hosting.Tests/DependsOnGraphTests.cs`：

```csharp
using Girvs.Cache;
using Girvs.EventBus;

namespace Girvs.Aspire.Hosting.Tests;

public class DependsOnGraphTests
{
    [DependsOn(typeof(GirvsCacheModule), typeof(EventBusModule))]
    private class ServiceModule;

    [DependsOn(typeof(ServiceModule), typeof(GirvsCacheModule))]
    private class NestedModule;

    [DependsOn(typeof(CycleB))]
    private class CycleA;

    [DependsOn(typeof(CycleA))]
    private class CycleB;

    private class EmptyModule;

    [Fact]
    public void 收集直接声明的依赖模块()
    {
        var modules = DependsOnGraph.Collect(typeof(ServiceModule));

        Assert.Equal(2, modules.Count);
        Assert.Contains(typeof(GirvsCacheModule), modules);
        Assert.Contains(typeof(EventBusModule), modules);
        Assert.DoesNotContain(typeof(ServiceModule), modules);
    }

    [Fact]
    public void 递归收集嵌套依赖且去重()
    {
        var modules = DependsOnGraph.Collect(typeof(NestedModule));

        Assert.Contains(typeof(GirvsCacheModule), modules);
        Assert.Contains(typeof(EventBusModule), modules);
        Assert.Single(modules, type => type == typeof(GirvsCacheModule));
    }

    [Fact]
    public void 循环依赖不会导致死循环()
    {
        var modules = DependsOnGraph.Collect(typeof(CycleA));

        Assert.Contains(typeof(CycleB), modules);
    }

    [Fact]
    public void 无声明的模块返回空列表()
    {
        Assert.Empty(DependsOnGraph.Collect(typeof(EmptyModule)));
    }
}
```

注意：`GirvsCacheModule` 命名空间为 `Girvs.Cache`，`EventBusModule` 为 `Girvs.EventBus`（以实际源码为准，编译报错时用 IDE 提示修正 using）。

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test tests/Girvs.Aspire.Hosting.Tests/Girvs.Aspire.Hosting.Tests.csproj`
Expected: 编译失败，报 `DependsOnGraph` 不存在。

- [ ] **Step 3: 实现图遍历**

`Girvs.Aspire.Hosting/DependsOnGraph.cs`：

```csharp
namespace Girvs.Aspire.Hosting;

public static class DependsOnGraph
{
    /// <summary>
    /// 深度优先收集根模块声明的全部依赖模块类型（含递归依赖），去重且防循环。
    /// 返回结果不含根模块本身。
    /// </summary>
    public static IReadOnlyList<Type> Collect(Type rootModuleType)
    {
        ArgumentNullException.ThrowIfNull(rootModuleType);

        var visited = new HashSet<Type>();
        var result = new List<Type>();
        Visit(rootModuleType, visited, result);
        result.Remove(rootModuleType);
        return result;
    }

    private static void Visit(Type moduleType, HashSet<Type> visited, List<Type> result)
    {
        if (!visited.Add(moduleType))
            return;

        result.Add(moduleType);

        var dependedTypes = moduleType
            .GetCustomAttributes<DependsOnAttribute>(false)
            .SelectMany(attribute => attribute.DependedModuleTypes);

        foreach (var dependedType in dependedTypes)
            Visit(dependedType, visited, result);
    }
}
```

- [ ] **Step 4: 运行图遍历测试确认通过**

Run: `dotnet test tests/Girvs.Aspire.Hosting.Tests/Girvs.Aspire.Hosting.Tests.csproj`
Expected: `DependsOnGraphTests` 全部 PASS。

- [ ] **Step 5: 编写失败的测试（编排）**

`tests/Girvs.Aspire.Hosting.Tests/GirvsProjectOrchestrationTests.cs`：

```csharp
using Girvs.Cache;
using Girvs.EntityFrameworkCore;
using Girvs.EventBus;

namespace Girvs.Aspire.Hosting.Tests;

public class GirvsProjectOrchestrationTests : IDisposable
{
    [DependsOn(typeof(GirvsCacheModule))]
    private class CacheOnlyModule;

    [DependsOn(typeof(EventBusModule))]
    private class EventBusOnlyModule;

    [DependsOn(typeof(GirvsEntityFrameworkCoreModule))]
    private class DbOnlyModule;

    private class NoResourceModule;

    private readonly string _tempRoot = Directory
        .CreateTempSubdirectory("girvs-aspire-hosting-tests")
        .FullName;

    public void Dispose() => Directory.Delete(_tempRoot, true);

    private string CreateServiceProjectDirectory(string name, string appSettingsJson = null)
    {
        var directory = Path.Combine(_tempRoot, name);
        Directory.CreateDirectory(directory);
        File.WriteAllText(
            Path.Combine(directory, $"{name}.csproj"),
            """<Project Sdk="Microsoft.NET.Sdk.Web"></Project>"""
        );
        if (appSettingsJson != null)
            File.WriteAllText(Path.Combine(directory, "appsettings.json"), appSettingsJson);
        return directory;
    }

    private static IDistributedApplicationBuilder CreateBuilder() =>
        DistributedApplication.CreateBuilder(
            new DistributedApplicationOptions { DisableDashboard = true }
        );

    private static IResourceBuilder<ProjectResource> AddServiceProject(
        IDistributedApplicationBuilder builder,
        string name,
        string projectDirectory
    ) => builder.AddProject(name, Path.Combine(projectDirectory, $"{name}.csproj"));

    [Fact]
    public void 声明Cache模块_自动创建Redis资源()
    {
        var builder = CreateBuilder();
        var directory = CreateServiceProjectDirectory("order-api");
        var project = AddServiceProject(builder, "order-api", directory);

        builder.WireGirvsResources(project, directory, typeof(CacheOnlyModule));

        Assert.Single(builder.Resources.OfType<RedisResource>(), r => r.Name == "girvs-cache");
    }

    [Fact]
    public void 两个服务声明Cache模块_共享同一个Redis资源()
    {
        var builder = CreateBuilder();
        var directoryA = CreateServiceProjectDirectory("order-api");
        var directoryB = CreateServiceProjectDirectory("user-api");
        var projectA = AddServiceProject(builder, "order-api", directoryA);
        var projectB = AddServiceProject(builder, "user-api", directoryB);

        builder.WireGirvsResources(projectA, directoryA, typeof(CacheOnlyModule));
        builder.WireGirvsResources(projectB, directoryB, typeof(CacheOnlyModule));

        Assert.Single(builder.Resources.OfType<RedisResource>());
    }

    [Fact]
    public void EventBusType为RabbitMQ_创建RabbitMQ资源()
    {
        var builder = CreateBuilder();
        var directory = CreateServiceProjectDirectory(
            "order-api",
            """
            {
              "ModuleConfigurations": {
                "EventBusConfig": { "EventBusType": "RabbitMQ" }
              }
            }
            """
        );
        var project = AddServiceProject(builder, "order-api", directory);

        builder.WireGirvsResources(project, directory, typeof(EventBusOnlyModule));

        Assert.Single(
            builder.Resources.OfType<RabbitMQServerResource>(),
            r => r.Name == "girvs-eventbus-rabbitmq"
        );
    }

    [Fact]
    public void EventBus配置缺失_不创建资源且不抛异常()
    {
        var builder = CreateBuilder();
        var directory = CreateServiceProjectDirectory("order-api");
        var project = AddServiceProject(builder, "order-api", directory);

        var exception = Record.Exception(
            () => builder.WireGirvsResources(project, directory, typeof(EventBusOnlyModule))
        );

        Assert.Null(exception);
        Assert.Empty(builder.Resources.OfType<RabbitMQServerResource>());
    }

    [Fact]
    public void 多命名库_分别创建MySql与SqlServer数据库资源()
    {
        var builder = CreateBuilder();
        var directory = CreateServiceProjectDirectory(
            "order-api",
            """
            {
              "ModuleConfigurations": {
                "DbConfig": {
                  "DataConnectionConfigs": [
                    { "Name": "default", "UseDataType": "MySql" },
                    { "Name": "log", "UseDataType": "MsSql" }
                  ]
                }
              }
            }
            """
        );
        var project = AddServiceProject(builder, "order-api", directory);

        builder.WireGirvsResources(project, directory, typeof(DbOnlyModule));

        Assert.Single(builder.Resources.OfType<MySqlServerResource>(), r => r.Name == "girvs-mysql");
        Assert.Single(
            builder.Resources.OfType<SqlServerServerResource>(),
            r => r.Name == "girvs-sqlserver"
        );
        Assert.Contains(builder.Resources, r => r.Name == "girvs-db-order-api-default");
        Assert.Contains(builder.Resources, r => r.Name == "girvs-db-order-api-log");
    }

    [Fact]
    public void 两个服务各自声明数据库_创建各自独立的数据库资源并共享服务器()
    {
        var builder = CreateBuilder();
        const string appSettings = """
            {
              "ModuleConfigurations": {
                "DbConfig": {
                  "DataConnectionConfigs": [{ "Name": "default", "UseDataType": "MySql" }]
                }
              }
            }
            """;
        var directoryA = CreateServiceProjectDirectory("order-api", appSettings);
        var directoryB = CreateServiceProjectDirectory("user-api", appSettings);
        var projectA = AddServiceProject(builder, "order-api", directoryA);
        var projectB = AddServiceProject(builder, "user-api", directoryB);

        builder.WireGirvsResources(projectA, directoryA, typeof(DbOnlyModule));
        builder.WireGirvsResources(projectB, directoryB, typeof(DbOnlyModule));

        Assert.Single(builder.Resources.OfType<MySqlServerResource>());
        Assert.Contains(builder.Resources, r => r.Name == "girvs-db-order-api-default");
        Assert.Contains(builder.Resources, r => r.Name == "girvs-db-user-api-default");
    }

    [Fact]
    public void 启用独立缓存_创建服务专属Redis资源()
    {
        var builder = CreateBuilder();
        var directory = CreateServiceProjectDirectory("order-api");
        var project = AddServiceProject(builder, "order-api", directory);

        builder.WireGirvsResources(
            project,
            directory,
            typeof(CacheOnlyModule),
            new GirvsProjectOptions { UseIsolatedCache = true }
        );

        Assert.Single(
            builder.Resources.OfType<RedisResource>(),
            r => r.Name == "girvs-cache-order-api"
        );
    }

    [Fact]
    public void 无资源映射的模块被忽略()
    {
        var builder = CreateBuilder();
        var directory = CreateServiceProjectDirectory("order-api");
        var project = AddServiceProject(builder, "order-api", directory);
        var resourceCountBefore = builder.Resources.Count;

        builder.WireGirvsResources(project, directory, typeof(NoResourceModule));

        Assert.Equal(resourceCountBefore, builder.Resources.Count);
    }
}
```

排错提示：若 `AddProject(name, path)` 因缺少 `launchSettings.json` 抛异常，在 `CreateServiceProjectDirectory` 中额外写入 `Properties/launchSettings.json` 最小内容 `{"profiles":{}}`，或改用带 `launchProfileName: null` 的重载。资源类型名（`RedisResource`/`RabbitMQServerResource`/`MySqlServerResource`/`SqlServerServerResource`）以所装 Aspire 包实际 API 为准。

- [ ] **Step 6: 运行测试确认失败**

Run: `dotnet test tests/Girvs.Aspire.Hosting.Tests/Girvs.Aspire.Hosting.Tests.csproj`
Expected: 编译失败，报 `WireGirvsResources` 不存在。

- [ ] **Step 7: 实现编排核心**

`Girvs.Aspire.Hosting/GirvsServiceAppSettings.cs`：

```csharp
namespace Girvs.Aspire.Hosting;

public static class GirvsServiceAppSettings
{
    /// <summary>
    /// 读取服务项目目录下的 appsettings.json（与服务端 ConfigurationDefaults.AppSettingsFilePath 一致）。
    /// 目录为空或文件不存在时返回空配置（各贡献器随之降级跳过）。
    /// </summary>
    public static IConfiguration Read(string projectDirectory)
    {
        var builder = new ConfigurationBuilder();
        if (!string.IsNullOrEmpty(projectDirectory))
        {
            builder.AddJsonFile(
                Path.Combine(projectDirectory, "appsettings.json"),
                optional: true,
                reloadOnChange: false
            );
        }

        return builder.Build();
    }
}
```

`Girvs.Aspire.Hosting/GirvsOrchestrationContext.cs`：

```csharp
namespace Girvs.Aspire.Hosting;

public class GirvsOrchestrationContext(
    IDistributedApplicationBuilder builder,
    IResourceBuilder<ProjectResource> project,
    IConfiguration serviceSettings,
    GirvsProjectOptions options
)
{
    public IDistributedApplicationBuilder Builder { get; } = builder;
    public IResourceBuilder<ProjectResource> Project { get; } = project;
    public IConfiguration ServiceSettings { get; } = serviceSettings;
    public GirvsProjectOptions Options { get; } = options ?? new GirvsProjectOptions();

    /// <summary>
    /// 按资源名惰性创建资源：已存在（其他服务先创建）则复用，实现多服务共享同一资源实例。
    /// </summary>
    public IResourceBuilder<T> GetOrAddResource<T>(string name, Func<IResourceBuilder<T>> factory)
        where T : class, IResource
    {
        var existing = Builder
            .Resources.OfType<T>()
            .FirstOrDefault(resource =>
                string.Equals(resource.Name, name, StringComparison.OrdinalIgnoreCase)
            );
        return existing is not null ? Builder.CreateResourceBuilder(existing) : factory();
    }
}
```

`Girvs.Aspire.Hosting/GirvsProjectOptions.cs`：

```csharp
namespace Girvs.Aspire.Hosting;

/// <summary>AddGirvsProject 的可选配置。</summary>
public class GirvsProjectOptions
{
    /// <summary>
    /// 为该服务创建独立 Redis 缓存实例（girvs-cache-&lt;service&gt;）；
    /// 默认 false，多服务共享 girvs-cache 实例。
    /// </summary>
    public bool UseIsolatedCache { get; set; }
}
```

`Girvs.Aspire.Hosting/Contributors/IGirvsResourceContributor.cs`：

```csharp
namespace Girvs.Aspire.Hosting.Contributors;

public interface IGirvsResourceContributor
{
    /// <summary>匹配的 Girvs 模块类型全名，如 "Girvs.Cache.GirvsCacheModule"</summary>
    string ModuleTypeFullName { get; }

    void Contribute(GirvsOrchestrationContext context);
}
```

`Girvs.Aspire.Hosting/Contributors/CacheResourceContributor.cs`：

```csharp
namespace Girvs.Aspire.Hosting.Contributors;

public class CacheResourceContributor : IGirvsResourceContributor
{
    public string ModuleTypeFullName => "Girvs.Cache.GirvsCacheModule";

    public void Contribute(GirvsOrchestrationContext context)
    {
        // 默认共享实例；UseIsolatedCache 时为服务创建独立实例。
        // 注入名固定为 girvs-cache，与服务端 AspireConnectionStringMapper 约定对齐。
        var resourceName = context.Options.UseIsolatedCache
            ? $"girvs-cache-{context.Project.Resource.Name}"
            : "girvs-cache";

        var redis = context.GetOrAddResource(
            resourceName,
            () => context.Builder.AddRedis(resourceName)
        );
        context.Project.WithReference(redis, connectionName: "girvs-cache").WaitFor(redis);
    }
}
```

`Girvs.Aspire.Hosting/Contributors/EventBusResourceContributor.cs`：

```csharp
namespace Girvs.Aspire.Hosting.Contributors;

public class EventBusResourceContributor : IGirvsResourceContributor
{
    public string ModuleTypeFullName => "Girvs.EventBus.EventBusModule";

    public void Contribute(GirvsOrchestrationContext context)
    {
        var eventBusType = context.ServiceSettings[
            "ModuleConfigurations:EventBusConfig:EventBusType"
        ];

        switch (eventBusType?.ToLowerInvariant())
        {
            case "rabbitmq":
                var rabbit = context.GetOrAddResource(
                    "girvs-eventbus-rabbitmq",
                    () => context.Builder.AddRabbitMQ("girvs-eventbus-rabbitmq")
                );
                context.Project.WithReference(rabbit).WaitFor(rabbit);
                break;
            case "redis":
                var redis = context.GetOrAddResource(
                    "girvs-eventbus-redis",
                    () => context.Builder.AddRedis("girvs-eventbus-redis")
                );
                context.Project.WithReference(redis).WaitFor(redis);
                break;
            default:
                // Kafka（云集群直连）或配置缺失：不自动创建，可按 girvs-* 命名约定手动补资源
                Console.Error.WriteLine(
                    $"[Girvs.Aspire.Hosting] 服务 {context.Project.Resource.Name} 的 EventBusType 为 '{eventBusType ?? "未配置"}'，未自动创建事件总线资源"
                );
                break;
        }
    }
}
```

`Girvs.Aspire.Hosting/Contributors/DatabaseResourceContributor.cs`：

```csharp
namespace Girvs.Aspire.Hosting.Contributors;

public class DatabaseResourceContributor : IGirvsResourceContributor
{
    public string ModuleTypeFullName =>
        "Girvs.EntityFrameworkCore.GirvsEntityFrameworkCoreModule";

    public void Contribute(GirvsOrchestrationContext context)
    {
        var connectionConfigs = context
            .ServiceSettings.GetSection("ModuleConfigurations:DbConfig:DataConnectionConfigs")
            .GetChildren()
            .ToList();

        if (connectionConfigs.Count == 0)
        {
            Console.Error.WriteLine(
                $"[Girvs.Aspire.Hosting] 服务 {context.Project.Resource.Name} 未配置 DataConnectionConfigs，未自动创建数据库资源"
            );
            return;
        }

        foreach (var connectionConfig in connectionConfigs)
        {
            var name = connectionConfig["Name"] ?? "default";
            var useDataType = connectionConfig["UseDataType"]?.ToLowerInvariant();
            var serviceName = context.Project.Resource.Name;

            // 每服务独立数据库（架构图 orderdb/userdb 模式）：资源名全局唯一，
            // 注入名固定为 girvs-db-<name> 与服务端映射约定对齐
            var resourceName = $"girvs-db-{serviceName}-{name}";
            var databaseName = $"{serviceName}_{name}".Replace('-', '_');
            var connectionName = $"girvs-db-{name}";

            switch (useDataType)
            {
                case "mysql":
                {
                    var server = context.GetOrAddResource(
                        "girvs-mysql",
                        () => context.Builder.AddMySql("girvs-mysql")
                    );
                    var database = context.GetOrAddResource(
                        resourceName,
                        () => server.AddDatabase(resourceName, databaseName)
                    );
                    context.Project.WithReference(database, connectionName).WaitFor(database);
                    break;
                }
                case "mssql":
                {
                    var server = context.GetOrAddResource(
                        "girvs-sqlserver",
                        () => context.Builder.AddSqlServer("girvs-sqlserver")
                    );
                    var database = context.GetOrAddResource(
                        resourceName,
                        () => server.AddDatabase(resourceName, databaseName)
                    );
                    context.Project.WithReference(database, connectionName).WaitFor(database);
                    break;
                }
                default:
                    Console.Error.WriteLine(
                        $"[Girvs.Aspire.Hosting] 数据连接 '{name}' 的 UseDataType 为 '{useDataType ?? "未配置"}'，未自动创建数据库资源"
                    );
                    break;
            }
        }
    }
}
```

`Girvs.Aspire.Hosting/GirvsResourceRegistry.cs`：

```csharp
namespace Girvs.Aspire.Hosting;

public static class GirvsResourceRegistry
{
    private static readonly Dictionary<string, IGirvsResourceContributor> Contributors = new();

    static GirvsResourceRegistry()
    {
        Register(new CacheResourceContributor());
        Register(new EventBusResourceContributor());
        Register(new DatabaseResourceContributor());
    }

    /// <summary>注册自定义模块资源贡献器（同名覆盖），供业务方扩展自己的模块。</summary>
    public static void Register(IGirvsResourceContributor contributor)
    {
        ArgumentNullException.ThrowIfNull(contributor);
        Contributors[contributor.ModuleTypeFullName] = contributor;
    }

    public static bool TryGet(Type moduleType, out IGirvsResourceContributor contributor) =>
        Contributors.TryGetValue(moduleType.FullName ?? string.Empty, out contributor);
}
```

`Girvs.Aspire.Hosting/GirvsProjectExtensions.cs`：

```csharp
namespace Girvs.Aspire.Hosting;

public static class GirvsProjectExtensions
{
    /// <summary>
    /// 添加 Girvs 服务项目：递归遍历根模块的 [DependsOn] 声明，
    /// 自动创建/复用对应的 Aspire 资源并 WithReference + WaitFor。
    /// </summary>
    public static IResourceBuilder<ProjectResource> AddGirvsProject<TProject>(
        this IDistributedApplicationBuilder builder,
        string name,
        Type rootModuleType,
        Action<GirvsProjectOptions> configure = null
    )
        where TProject : IProjectMetadata, new()
    {
        var options = new GirvsProjectOptions();
        configure?.Invoke(options);

        var project = builder.AddProject<TProject>(name);
        var projectDirectory = Path.GetDirectoryName(new TProject().ProjectPath);
        return builder.WireGirvsResources(project, projectDirectory, rootModuleType, options);
    }

    internal static IResourceBuilder<ProjectResource> WireGirvsResources(
        this IDistributedApplicationBuilder builder,
        IResourceBuilder<ProjectResource> project,
        string projectDirectory,
        Type rootModuleType,
        GirvsProjectOptions options = null
    )
    {
        var serviceSettings = GirvsServiceAppSettings.Read(projectDirectory);
        var context = new GirvsOrchestrationContext(builder, project, serviceSettings, options);

        foreach (var moduleType in DependsOnGraph.Collect(rootModuleType))
        {
            if (GirvsResourceRegistry.TryGet(moduleType, out var contributor))
                contributor.Contribute(context);
        }

        return project;
    }
}
```

- [ ] **Step 8: 运行测试确认通过**

Run: `dotnet test tests/Girvs.Aspire.Hosting.Tests/Girvs.Aspire.Hosting.Tests.csproj && dotnet build Girvs.slnx`
Expected: 全部测试 PASS；全量编译成功。

- [ ] **Step 9: 提交**

```bash
git add Girvs.Aspire.Hosting tests/Girvs.Aspire.Hosting.Tests
git commit -m "实现 AddGirvsProject 依据 DependsOn 声明自动编排资源"
```

---

### Task 8: 发布脚本与接入文档

**Files:**
- Modify: `nugetpublish.ps1`（`$modules` 数组，约 14-28 行）
- Create: `docs/aspire/apphost-guide.md`
- Modify: `README.md`（模块列表处追加一行；找到现有模块表格追加）
- Modify: `CLAUDE.md`（模块架构表追加 `Girvs.Aspire` 一行）

**Interfaces:**
- Consumes: Task 1-7 产出的模块与约定（`AddGirvsProject`、`[DependsOn]`、`girvs-*` 资源与注入名约定）。
- Produces: 发布链路与业务方接入文档。

- [ ] **Step 1: 修改发布脚本**

`nugetpublish.ps1` 的 `$modules` 数组中，在 `'Girvs',` 之后按字母序插入两行：

```powershell
    'Girvs.Aspire',
    'Girvs.Aspire.Hosting',
```

- [ ] **Step 2: 编写 AppHost 接入文档**

创建 `docs/aspire/apphost-guide.md`，内容如下（完整写入）：

````markdown
# Girvs 服务接入 .NET Aspire 指南

适用范围：目标框架为 net10.0 的 Girvs 服务。net8/net9 服务请继续使用 Girvs.Consul。
架构总览见仓库根目录 `Architect.png`。

## 1. 服务端接入（Girvs.Aspire）

1. 服务项目追加包引用：

```xml
<PackageReference Include="Girvs.Aspire" Version="10.0.0-rc.1" />
```

2. 无需修改任何代码与配置文件。`AspireModule` 会随 Girvs 模块机制自动生效：
   - 在 Aspire 环境（存在 `OTEL_EXPORTER_OTLP_ENDPOINT` 环境变量）下自动上报日志/追踪/指标到 Dashboard；
   - 自动把 AppHost 注入的连接串映射到 Girvs 各模块配置（见下文约定）；
   - 暴露 `/health` 与 `/alive` 健康检查端点；
   - HttpClient 默认启用 Aspire 服务发现与标准弹性策略。

3. 定义根模块类并用 `[DependsOn]` 声明该服务使用的 Girvs 组件（放在可被 AppHost 引用的程序集，推荐 Application 层）：

```csharp
[DependsOn(
    typeof(GirvsCacheModule),
    typeof(EventBusModule),
    typeof(GirvsEntityFrameworkCoreModule))]
public class OrderModule;
```

4. 与 Girvs.Consul 互斥：迁移到 Aspire 服务发现后，移除 Girvs.Consul 包引用与相关配置。

## 2. AppHost 项目（Girvs.Aspire.Hosting）

新建 Aspire AppHost 项目（`dotnet new aspire-apphost`），引用 `Girvs.Aspire.Hosting` 包与各服务根模块所在程序集，每个服务一行：

```csharp
var builder = DistributedApplication.CreateBuilder(args);

builder.AddGirvsProject<Projects.Order_Api>("order-api", typeof(OrderModule));
builder.AddGirvsProject<Projects.User_Api>("user-api", typeof(UserModule));

// 需要独立 Redis 实例的服务：
builder.AddGirvsProject<Projects.Inventory_Api>(
    "inventory-api", typeof(InventoryModule), o => o.UseIsolatedCache = true);

builder.Build().Run();
```

`AddGirvsProject` 依据 `[DependsOn]` 声明自动完成：

- 创建/复用共享资源（Redis、RabbitMQ、MySql/SqlServer 服务器），首个声明的服务触发创建，后续服务复用；
- 每服务创建独立数据库资源（`girvs-db-<service>-<name>`，实际库名 `<service>_<name>`）；
- 自动 `WithReference` + `WaitFor`，注入的连接串名与服务端映射约定自动对齐。

Worker / Background Service 项目同样用 `AddGirvsProject` 编排。

## 3. 资源与注入名约定

| 模块声明 | 创建的资源 | 注入到服务的连接串名 |
|---|---|---|
| `GirvsCacheModule` | 共享 `girvs-cache`（`UseIsolatedCache` 时为 `girvs-cache-<service>`） | `girvs-cache` |
| `EventBusModule`（appsettings 中 `EventBusType=RabbitMQ`） | 共享 `girvs-eventbus-rabbitmq` | `girvs-eventbus-rabbitmq` |
| `EventBusModule`（`EventBusType=Redis`） | 共享 `girvs-eventbus-redis` | `girvs-eventbus-redis` |
| `GirvsEntityFrameworkCoreModule` | 共享 `girvs-mysql`/`girvs-sqlserver` 服务器 + 每服务 `girvs-db-<service>-<name>` 数据库 | `girvs-db-<name>` |

服务端连接串映射（注入名 → Girvs 配置）：

| 注入的连接串名 | 映射到的 Girvs 配置 |
|---|---|
| `girvs-db-<name>` | `DbConfig.DataConnectionConfigs` 中 `Name == <name>` 项的主库连接串 |
| `girvs-db-<name>-read-<N>`（N 从 0 起连续编号） | 同名项的读库连接串列表 |
| `girvs-cache` | `CacheConfig.DistributedCacheConfig.ConnectionString` |
| `girvs-eventbus-db` | `EventBusConfig.DbConnectionString`（不自动创建，通常复用业务库） |
| `girvs-eventbus-redis` | `EventBusConfig.RedisConfig.RedisConnectionString` |
| `girvs-eventbus-rabbitmq` | AMQP URI，解析到 `EventBusConfig.RabbitMqConfig`（HostName/Port/UserName/Password/VirtualHost） |

资源形态取自服务的 appsettings.json（`EventBusType`、`DataConnectionConfigs`）；解析失败会输出警告并跳过，可用原生 Aspire API 按注入名约定手动补资源。自定义模块的资源编排：实现 `IGirvsResourceContributor` 后调用 `GirvsResourceRegistry.Register(...)`。

## 4. 根模块程序集引用模式

`typeof(OrderModule)` 要求 AppHost 能编译引用其所在程序集（Aspire 的 `IsAspireProjectResource="true"` 引用不暴露类型）：

- 推荐：根模块类放在服务的 Application 层或共享程序集，AppHost 对其加普通 `ProjectReference`；
- 备选：对服务项目额外加一个 `IsAspireProjectResource="false"` 的普通引用。

## 5. 常见问题

- **Dashboard 里看不到日志？** 确认服务由 AppHost 启动（`OTEL_EXPORTER_OTLP_ENDPOINT` 由 Aspire 自动注入）；框架保留 Serilog，OTLP 是追加的 sink，本地文件/控制台日志不受影响。
- **与 Consul 共存告警？** 迁移期间可忽略；完成迁移后移除 Girvs.Consul。
- **Kafka？** 云 Kafka 场景直连外部集群，不做本地容器编排；服务端按现有 `KafkaConfig` 配置直连。
- **数据库连接串格式？** Aspire 注入的是标准 ADO.NET 连接串，与 Girvs 现有格式一致，直接覆盖主库连接串。
````

- [ ] **Step 3: 更新 README 与 CLAUDE.md 模块列表**

`README.md`：找到模块介绍列表/表格（搜索 `Girvs.Consul`），紧邻处按现有格式追加两行：`Girvs.Aspire`——".NET Aspire 服务端集成：OpenTelemetry 可观测性、健康检查、服务发现、连接串自动映射（仅 net10.0）"；`Girvs.Aspire.Hosting`——"Aspire AppHost 编排扩展：AddGirvsProject 依据 [DependsOn] 声明自动创建资源并接线（仅 net10.0）"，并在合适位置链接 `docs/aspire/apphost-guide.md` 与架构图 `Architect.png`。

`CLAUDE.md`：在"模块架构"表格 `Girvs.Consul` 行之前追加：

```markdown
| `Girvs.Aspire` | .NET Aspire 服务端集成（仅 net10.0）：OpenTelemetry、健康检查、Aspire 服务发现、连接串映射 |
| `Girvs.Aspire.Hosting` | Aspire AppHost 编排扩展（仅 net10.0）：`AddGirvsProject` 依据 `[DependsOn]` 自动创建资源并 WithReference |
```

- [ ] **Step 4: 验证脚本语法**

Run: `pwsh -NoProfile -Command "& { $c = Get-Content ./nugetpublish.ps1 -Raw; [System.Management.Automation.PSParser]::Tokenize($c, [ref]$null) | Out-Null; 'OK' }"`（若本机无 pwsh 则跳过，人工复查数组语法逗号）
Expected: 输出 OK 或人工确认 `$modules` 数组语法正确。

- [ ] **Step 5: 提交**

```bash
git add nugetpublish.ps1 docs/aspire/apphost-guide.md README.md CLAUDE.md
git commit -m "新增 Aspire 接入文档并将 Girvs.Aspire 系列包纳入发布脚本"
```

---

### Task 9: 收尾验证

**Files:**
- 无新增；只跑验证。

**Interfaces:**
- Consumes: Task 1-8 的全部产出。
- Produces: 可发布状态的分支。

- [ ] **Step 1: 全量清理重建**

Run: `dotnet clean Girvs.slnx && dotnet build Girvs.slnx --no-incremental`
Expected: Build succeeded；`Girvs.Aspire/bin/Debug/` 与 `Girvs.Aspire.Hosting/bin/Debug/` 下仅生成 net10.0 产物与 nupkg；`Girvs/bin/Debug/` 下 net8.0/net9.0/net10.0 三套产物齐全。

- [ ] **Step 2: 全量测试**

Run: `dotnet test tests/Girvs.Aspire.Tests/Girvs.Aspire.Tests.csproj && dotnet test tests/Girvs.Aspire.Hosting.Tests/Girvs.Aspire.Hosting.Tests.csproj`
Expected: 两个测试工程全部 PASS，0 failed。

- [ ] **Step 3: Release 构建验证打包**

Run: `dotnet build Girvs.slnx -c Release`
Expected: Build succeeded；`Girvs.Aspire/bin/Release/Girvs.Aspire.10.0.0-rc.1.nupkg` 与 `Girvs.Aspire.Hosting/bin/Release/Girvs.Aspire.Hosting.10.0.0-rc.1.nupkg` 存在（版本号以 `Directory.Build.props` 当前值为准）。

- [ ] **Step 4: 检查未提交内容**

Run: `git status --short`
Expected: 除 `.omo/`（无关目录，不提交）外无未跟踪/未提交文件；若有遗漏文件按所属 Task 的提交粒度补提交。

**手工验证（需要用户环境，不阻塞本计划）：** 按 `docs/aspire/apphost-guide.md` 搭一个最小 AppHost + Girvs 示例服务，确认 Dashboard 中日志/追踪/指标可见、`/health` 返回 Healthy、连接串被正确覆盖。此步骤留给用户在带 Docker 的开发机上执行。

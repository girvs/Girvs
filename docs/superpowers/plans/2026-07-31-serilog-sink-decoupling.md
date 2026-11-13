# Girvs Serilog 日志出口解耦 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 让 Girvs 仅提供可读的 Console 日志兜底，所有生产日志 sink 均由业务应用自行选择、引用并配置。

**Architecture:** `HostUseSerilog` 先写入稳定的 Console 基础配置，再调用 `ReadFrom.Configuration` 读取业务 `appsettings.json` 中的 `Serilog` 节，使业务能覆盖级别并追加 sink。删除独立 `Serilog.json` 生成链路、自动 OTLP Hook 和所有框架层的具体生产 sink 依赖。

**Tech Stack:** .NET 8/9/10、Serilog.AspNetCore、Serilog.Settings.Configuration、Serilog.Sinks.Console、xUnit。

## Global Constraints

- Girvs 仅保留 `Serilog.AspNetCore`、`Serilog.Settings.Configuration` 与 `Serilog.Sinks.Console`；不得引用 Elasticsearch、File、OpenTelemetry 或数据库等具体生产 sink/formatter 包。
- `Girvs.ServiceGovernance` 保留 OpenTelemetry metrics/tracing 的 `UseOtlpExporter()`，但不得保留 Serilog OTLP sink 或反射 Hook。
- 默认级别必须为 `Information`，`Microsoft` 与 `System` 覆盖级别必须为 `Warning`。
- Console 模板必须为 `{Timestamp:yyyy-MM-dd HH:mm:ss} || [{Level:u3}] || {SourceContext:l} || {Message:lj} || {Exception}{NewLine}`，不得包含 `||end`。
- `ReadFrom.Configuration(context.Configuration)` 必须在 Console 默认配置之后调用，以便业务配置覆盖日志级别并追加 sink。
- 不改动业务应用的日志输出目标配置；业务自行在 `appsettings.json` 的 `Serilog` 节配置并引用所需 sink 包。
- 不提交 Git commit，除非用户另行明确要求。

---

### Task 1: 清理独立 Serilog 配置与生产 sink 依赖

**Files:**
- Delete: `Girvs/Configuration/SerilogInitConfig.cs`
- Modify: `Girvs/Girvs.csproj`
- Modify: `Girvs/Configuration/AppSettingsHelper.cs`
- Modify: `Girvs/Configuration/ConfigurationDefaults.cs`
- Modify: `Girvs/Infrastructure/Extensions/ServiceCollectionExtensions.cs`
- Modify: `Girvs/GirvsHostBuilderManager.cs`

**Interfaces:**
- Consumes: `ConfigurationDefaults.AppSettingsFilePath`、`HostUseGirvsConfig`。
- Produces: 配置加载不再依赖 `Serilog.json`，`ConfigureApplicationServices` 不再创建该文件。

- [ ] **Step 1: 为旧配置生成链路写失败断言**

在可运行的回归检查中断言以下标识符不再存在于 Girvs 生产代码：`SerilogInitConfig`、`SerilogSettingFilePath`、`CreateSerilogConfig`、`ExistSerilogConfigFile`、`AddBindSerilogConfiguation`。先执行检查，确认改造前会因这些标识符存在而失败。

- [ ] **Step 2: 删除 C# 生成的 Serilog.json 链路**

删除 `SerilogInitConfig.cs`；从 `AppSettingsHelper` 删除 `CreateSerilogConfig`、`ExistSerilogConfigFile`；从 `ConfigurationDefaults` 删除 `SerilogSettingFilePath`；从 `ConfigureApplicationServices` 删除 `AddBindSerilogConfiguation` 调用和方法；从 `HostUseGirvsConfig` 删除：

```csharp
config.AddJsonFile(ConfigurationDefaults.SerilogSettingFilePath, true, true);
```

- [ ] **Step 3: 删除核心项目的具体生产 sink 依赖**

在 `Girvs/Girvs.csproj` 的 `net8.0`、`net9.0`、`net10.0` 条件包组中分别删除：

```xml
<PackageReference Include="Serilog.Formatting.Elasticsearch" Version="10.0.0"/>
<PackageReference Include="Serilog.Sinks.Elasticsearch" Version="10.0.0"/>
<PackageReference Include="Serilog.Sinks.File" Version="..."/>
```

保留对应框架的 `Serilog.AspNetCore`、`Serilog.Settings.Configuration`、`Serilog.Sinks.Console`。

- [ ] **Step 4: 复跑回归检查与核心项目构建**

Run: `dotnet build Girvs/Girvs.csproj --nologo`

Expected: 三个目标框架通过；标识符检查不再匹配。

### Task 2: TDD 重建 Console 基础配置与业务配置扩展

**Files:**
- Modify: `Girvs/GirvsHostBuilderManager.cs`
- Create: `tests/Girvs.Core.Tests/Girvs.Core.Tests.csproj`
- Create: `tests/Girvs.Core.Tests/GirvsHostBuilderManagerTests.cs`
- Modify: `Girvs.slnx`

**Interfaces:**
- Consumes: `GirvsHostBuilderManager.HostUseSerilog(IHostBuilder)`、`Serilog.Settings.Configuration`。
- Produces: 默认 Console 管道；业务 `Serilog` 配置的级别覆盖与 sink 追加能力。

- [ ] **Step 1: 建立核心日志回归测试项目和失败测试**

创建 `tests/Girvs.Core.Tests/Girvs.Core.Tests.csproj`：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <GeneratePackageOnBuild>false</GeneratePackageOnBuild>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.7.0" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5" />
    <PackageReference Include="Serilog.Sinks.TestCorrelator" Version="4.0.0" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="../../Girvs/Girvs.csproj" />
  </ItemGroup>
</Project>
```

将项目加入 `Girvs.slnx`。创建 `GirvsHostBuilderManagerTests.cs`，使用 `Console.SetOut` 验证没有业务配置时的 Console 兜底，并使用 `TestCorrelator` 验证业务配置能把 `MinimumLevel` 覆盖为 `Debug` 且追加 sink：

```csharp
[Fact]
public void HostUseSerilog_未提供业务配置时_输出Information日志到Console()
{
    var originalOut = Console.Out;
    var output = new StringWriter();
    Console.SetOut(output);
    try
    {
        using var host = CreateHost([]);
        host.Services.GetRequiredService<ILogger<GirvsHostBuilderManagerTests>>()
            .LogInformation("默认 Console 日志");

        Assert.Contains("默认 Console 日志", output.ToString());
        Assert.Contains("INF", output.ToString());
    }
    finally
    {
        Console.SetOut(originalOut);
    }
}

[Fact]
public void HostUseSerilog_读取业务Serilog配置以覆盖默认级别并追加Sink()
{
    using var context = TestCorrelator.CreateContext();
    using var host = CreateHost(
    [
        new KeyValuePair<string, string?>("Serilog:MinimumLevel:Default", "Debug"),
        new KeyValuePair<string, string?>("Serilog:WriteTo:0:Name", "TestCorrelator")
    ]);

    host.Services.GetRequiredService<ILogger<GirvsHostBuilderManagerTests>>()
        .LogDebug("业务配置 Debug 日志");

    Assert.Contains(
        TestCorrelator.GetLogEventsFromCurrentContext(),
        logEvent => logEvent.RenderMessage().Contains("业务配置 Debug 日志")
    );
}
```

`CreateHost` 必须使用 `new HostBuilder().ConfigureAppConfiguration(...)` 注入 `ConfigurationBuilder().AddInMemoryCollection(settings).Build()`，再调用 `.HostUseSerilog().Build()`。测试类添加 `using Microsoft.Extensions.Configuration;`、`using Microsoft.Extensions.DependencyInjection;`、`using Microsoft.Extensions.Hosting;`、`using Microsoft.Extensions.Logging;`、`using Serilog.Sinks.TestCorrelator;`。

测试应使用实际 `IHostBuilder` 和内存配置构建 Host，验证无业务 `Serilog` 节时可构建并记录 Information 日志；提供 `Serilog:MinimumLevel:Default=Debug` 与测试 sink 配置时，验证 Debug 日志可被业务 sink 接收。测试所需的 test-only sink 包只放在测试项目，不得进入 Girvs 生产项目。

- [ ] **Step 2: 运行测试并确认 RED**

Run: `dotnet test tests/Girvs.Core.Tests/Girvs.Core.Tests.csproj --nologo`

Expected: FAIL，原因是旧 `HostUseSerilog` 仍调用自动 OTLP Hook，且未提供已定义的默认配置顺序/测试接口。

- [ ] **Step 3: 实现最小 Console 基础配置**

在 `GirvsHostBuilderManager.cs` 添加 `using Serilog.Events;`。将 `HostUseSerilog` 改为以下顺序，移除 `TryAddGirvsOtlpSink` 的调用和整个方法：

```csharp
hostBuilder.UseSerilog((context, configuration) =>
{
    configuration
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("System", LogEventLevel.Warning)
        .WriteTo.Console(
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} || [{Level:u3}] || {SourceContext:l} || {Message:lj} || {Exception}{NewLine}"
        )
        .ReadFrom.Configuration(context.Configuration);
});
```

添加仅供测试使用的最小可见性或测试辅助接口；不得暴露新的公共业务 API。

- [ ] **Step 4: 运行核心测试并确认 GREEN**

Run: `dotnet test tests/Girvs.Core.Tests/Girvs.Core.Tests.csproj --nologo`

Expected: PASS；默认 Console 行为与业务配置覆盖均通过。

### Task 3: 移除自动 OTLP 日志 Hook 及其测试

**Files:**
- Delete: `Girvs.ServiceGovernance/GirvsSerilogOtlpHook.cs`
- Delete: `Girvs.ServiceGovernance/Logging/GirvsSerilogOtlpHook.cs`
- Delete: `tests/Girvs.ServiceGovernance.Tests/GirvsSerilogOtlpHookTests.cs`
- Modify: `Girvs.ServiceGovernance/Girvs.ServiceGovernance.csproj`

**Interfaces:**
- Consumes: Task 2 已删除的 `TryAddGirvsOtlpSink` 反射调用。
- Produces: ServiceGovernance 不再对 Serilog 日志出口负责；metrics/tracing 保持现状。

- [ ] **Step 1: 写失败的仓库引用检查**

检查生产与测试代码中不再存在 `GirvsSerilogOtlpHook` 或 `Serilog.Sinks.OpenTelemetry`；先执行，确认改造前因 Hook 文件、项目引用与专用测试而失败。

- [ ] **Step 2: 删除 Hook 与包引用**

删除两个 Hook 文件和 `GirvsSerilogOtlpHookTests.cs`；从 `Girvs.ServiceGovernance.csproj` 删除：

```xml
<PackageReference Include="Serilog.Sinks.OpenTelemetry" Version="4.2.0"/>
```

不得修改 `ServiceGovernanceModule.cs` 中的 `.UseOtlpExporter()`，因为它仍负责 metrics/tracing。

- [ ] **Step 3: 运行检查与服务治理测试**

Run: `dotnet test tests/Girvs.ServiceGovernance.Tests/Girvs.ServiceGovernance.Tests.csproj --nologo`

Expected: PASS；引用检查不再匹配 Hook 或 Serilog OTLP sink。

### Task 4: 全量验证与文档核对

**Files:**
- Modify: `docs/superpowers/specs/2026-07-31-serilog-sink-decoupling-design.md`（仅在验证发现规格与实现不一致时修正）

**Interfaces:**
- Consumes: Task 1-3 的删改结果。
- Produces: 满足验收标准的多目标构建与回归测试证据。

- [ ] **Step 1: 执行全量构建**

Run: `dotnet build Girvs.slnx --nologo`

Expected: 所有项目构建通过。

- [ ] **Step 2: 执行全量测试**

Run: `dotnet test Girvs.slnx --nologo`

Expected: 所有测试通过；若存在环境相关基线失败，记录完整失败列表并确认没有日志改造导致的新失败。

- [ ] **Step 3: 执行最终删除边界检查**

Run: `rg "SerilogInitConfig|SerilogSettingFilePath|CreateSerilogConfig|ExistSerilogConfigFile|AddBindSerilogConfiguation|TryAddGirvsOtlpSink|GirvsSerilogOtlpHook|Serilog\.Sinks\.(Elasticsearch|File|OpenTelemetry)|Serilog\.Formatting\.Elasticsearch" Girvs Girvs.ServiceGovernance tests`

Expected: 无匹配。

- [ ] **Step 4: 核对 Spec 验收标准**

逐项核对 `docs/superpowers/specs/2026-07-31-serilog-sink-decoupling-design.md` 的五项验收标准，记录验证命令和结果。

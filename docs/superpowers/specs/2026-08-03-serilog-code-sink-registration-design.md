---
workflow_status: approved
approved_at: 2026-08-03
---

# Girvs Serilog 代码方式 Sink 注册设计

## 状态

- 日期：2026-08-03
- 状态：草稿
- 前置 Spec：[2026-07-31-serilog-sink-decoupling-design.md](./2026-07-31-serilog-sink-decoupling-design.md)

## 背景

`2026-07-31-serilog-sink-decoupling-design` 将 Girvs 核心与具体 Serilog Sink (File、Elasticsearch、OpenTelemetry) 解耦，业务通过 `appsettings.json` 的 `Serilog:WriteTo` 节自行配置输出目标。

实践发现，`appsettings.json` 配置方式在某些场景不够灵活——业务需要动态构造 SinkOptions（如运行时获取 Elasticsearch 地址、根据环境变量拼接索引名称）。业务期望在 `Startup.ConfigureServices` 中通过 C# 代码直接调用 `WriteTo.File(...)` 或 `WriteTo.Elasticsearch(...)` 等强类型 API。

本 Spec 在保留配置方式的基础上，新增代码方式注册 Sink 的能力，两种方式可并存。

## 目标

1. 提供 `services.AddSerilogSink(Action<LoggerConfiguration>, params string[])` 扩展方法，允许业务在 `ConfigureServices` 中以代码方式追加 Sink。
2. 代码注册的 Sink 与 `appsettings.json` 中 `Serilog:WriteTo` 配置的 Sink 合并到同一 Serilog 管道。
3. 代码注册声明覆盖的 Sink 类型名，配置文件中的同名 Sink 自动跳过，避免重复。
4. 同一覆盖声明集合的多次代码注册，后者整体替换前者。
5. 代码委托执行异常时兜底降级（SelfLog 记录，跳过该 Sink），App 正常启动。
6. Girvs 核心不引用任何具体 Sink 包，维持解耦设计。

## 非目标

- 不提供运行时动态增删 Sink 的能力（注册是静态的，仅发生在 `ConfigureServices` 阶段）。
- 不新增 `IGirvsSerilogConfigurator` 等接口，不修改 `IGirvsStartup`。
- 不改变业务代码中对 `ILogger<T>` 的调用方式。
- 不改变 OpenTelemetry metrics/tracing 的配置。

## 运行时架构

### 注册阶段（ConfigureServices）

业务在 `ConfigureServices` 中调用 `services.AddSerilogSink(...)`，委托被写入静态注册表 `GirvsSerilogSinkRegistry.Registrations`。

```csharp
// 业务侧示例 — File sink
services.AddSerilogSink(
    config => config.WriteTo.File("./logs/log-.txt", rollingInterval: RollingInterval.Day),
    "File"
);

// 业务侧示例 — Elasticsearch sink
services.AddSerilogSink(
    config => config.WriteTo.Elasticsearch(new ElasticsearchSinkOptions(nodeUri)
    {
        IndexFormat = "my-index-{0:yyyy.MM.dd}",
        AutoRegisterTemplate = true,
    }),
    "Elasticsearch"
);
```

### 消费阶段（HostUseSerilog 回调）

`HostUseSerilog` 在 `builder.Build()` 的 Serilog 回调中执行以下流程：

```
① SnapshotAndClear 注册表
② 执行所有代码委托（兜底降级）→ 追加到 LoggerConfiguration
③ 收集 OverriddenSinkTypeNames → 黑名单
④ 遍历 Serilog:WriteTo 配置数组
   - 黑名单命中 → 跳过
   - 未命中 → 保留原条目
⑤ 构造裁剪后的 IConfiguration（保留 MinimumLevel/Enrich/Filter/Using，过滤 WriteTo）
⑥ ReadFrom.Configuration(裁剪后配置)
⑦ Console 兜底 ← 框架始终保留
```

## 核心 API

### GirvsSerilogSinkRegistry（框架内部）

| 成员 | 说明 |
|------|------|
| `Registrations: List<SerilogSinkRegistration>` | 静态注册表 |
| `SnapshotAndClear()` | 提取快照并清空，返回 `List<SerilogSinkRegistration>` |

```csharp
internal sealed record SerilogSinkRegistration(
    Action<LoggerConfiguration> Configure,
    IReadOnlySet<string> OverriddenSinkTypeNames
);
```

### AddSerilogSink 扩展方法（公开 API）

```csharp
public static IServiceCollection AddSerilogSink(
    this IServiceCollection services,
    Action<LoggerConfiguration> configure,
    params string[] overriddenSinkTypeNames)
```

**语义：**

- `overriddenSinkTypeNames` 声明该委托覆盖的 Sink 类型名（与 `Serilog:WriteTo[n]:Name` 比较，忽略大小写）；
- 若已有注册的 `OverriddenSinkTypeNames` 集合**完全相等**（`SetEquals`），则后者替换前者（整个注册条目替换）；
- 不同覆盖集合视为独立注册，互不替换；
- 配置文件中 `Serilog:WriteTo[n]:Name` 命中任何 `overriddenSinkTypeNames` 中的值即被跳过。

### HostUseSerilog 改造

```csharp
public static void HostUseSerilog(this IHostBuilder hostBuilder)
{
    hostBuilder.UseSerilog((context, configuration) =>
    {
        configuration
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .WriteTo.Console(outputTemplate: "...");

        // ① 执行代码注册的 sink
        var registrations = GirvsSerilogSinkRegistry.SnapshotAndClear();
        var overrides = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var reg in registrations)
        {
            try { reg.Configure(configuration); }
            catch (Exception ex)
            {
                SelfLog.WriteLine("Girvs Serilog Sink 注册失败 [{0}]: {1}",
                    string.Join(",", reg.OverriddenSinkTypeNames), ex);
            }
            foreach (var name in reg.OverriddenSinkTypeNames)
                overrides.Add(name);
        }

        // ② 构造裁剪后的配置（过滤被覆盖的 WriteTo 条目）
        var serilogSection = context.Configuration.GetSection("Serilog");
        if (serilogSection.Exists())
        {
            var filteredConfig = BuildFilteredSerilogSection(serilogSection, overrides);
            configuration.ReadFrom.Configuration(filteredConfig);
        }
    });
}
```

### BuildFilteredSerilogSection

```csharp
private static IConfiguration BuildFilteredSerilogSection(
    IConfigurationSection serilogSection,
    HashSet<string> overrides)
```

**逻辑：**

- 遍历 `Serilog` 节的所有子节点；
- `WriteTo` 子节特殊处理：遍历其数组子节点，命名的 `Name` 属性在 `overrides` 集合中则跳过，其余保留；
- `MinimumLevel`、`Enrich`、`Filter`、`Using` 等其他子节原样保留；
- 通过 `MemoryConfigurationSource` 构造裁剪后的 `IConfiguration` 返回。

## 配置节过滤示例

假设业务代码注册了 `AddSerilogSink(..., "File")`，且 `appsettings.json` 中有：

```json
{
  "Serilog": {
    "MinimumLevel": { "Default": "Debug" },
    "WriteTo": [
      { "Name": "File", "Args": { "path": "./logs/..." } },
      { "Name": "Elasticsearch", "Args": { "nodeUris": "..." } }
    ]
  }
}
```

过滤结果：
- `"File"` 在 overrides → 跳过
- `"Elasticsearch"` 不在 overrides → 保留
- `MinimumLevel` 保留

## 异常处理策略

| 异常场景 | 处理方式 |
|---------|---------|
| 代码委托执行失败 | `SelfLog.WriteLine` 记录 → 跳过该委托 → App 继续启动，Console 始终有效 |
| `BuildFilteredSerilogSection` 解析失败 | `SelfLog.WriteLine` 记录 → 降级为空配置传给 `ReadFrom.Configuration` |
| 配置 `Serilog:WriteTo` 单条目解析失败 | 跳过该条目，其余保留 |
| 无配置、无代码注册 | Console 兜底，App 正常启动 |

## 删除边界

本 Spec 新增以下文件/类：

| 文件 | 说明 |
|------|------|
| `Girvs/Infrastructure/GirvsSerilogSinkRegistry.cs` | 静态注册表 + `SerilogSinkRegistration` record |
| `Girvs/Infrastructure/Extensions/SerilogSinkServiceCollectionExtensions.cs` | `AddSerilogSink` 扩展方法 |

改造以下文件：

| 文件 | 改造内容 |
|------|---------|
| `Girvs/GirvsHostBuilderManager.cs` | `HostUseSerilog` 中新增注册表消费 + `BuildFilteredSerilogSection` 私有方法 |
| `samples/Sample.Gateway/Startup.cs` | 移除反射 `AddElasticsearchLogging`，改用 `services.AddSerilogSink(..., "Elasticsearch")` |
| `tests/Sample.Gateway.Tests/StartupElasticsearchLoggingTests.cs` | 适配新的 API |

## 验收标准

1. `services.AddSerilogSink(cfg => cfg.WriteTo.File(...), "File")` 在 `ConfigureServices` 中注册后，运行时 File 日志生效。
2. 代码声明覆盖 `"File"` 时，`appsettings.json` 中 `Serilog:WriteTo[n]:Name=File` 的条目被跳过，不会出现两个 File sink。
3. 同一覆盖集合的多次注册，后者替换前者，仅最后一个生效。
4. 代码委托抛出异常时（如文件路径无权限），App 不崩溃，SelfLog 记录异常，Console 输出正常。
5. 无代码注册、无配置文件时，框架 Console 兜底正常工作。
6. `dotnet build Girvs.slnx --nologo` 与 `dotnet test Girvs.slnx --nologo` 通过。
7. Girvs 核心不新增对 `Serilog.Sinks.File`、`Serilog.Sinks.Elasticsearch`、`Serilog.Sinks.OpenTelemetry` 等具体 Sink 包的引用。

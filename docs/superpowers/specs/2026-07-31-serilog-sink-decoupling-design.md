# Girvs Serilog 日志出口解耦设计

## 状态

- 日期：2026-07-31
- 状态：已冻结

## 背景

当前 Girvs 核心通过 `SerilogInitConfig` 用 C# 匿名对象生成 `Serilog.json`，并默认配置 Console 与多组本地 File sink。核心项目还直接引用 Elasticsearch 格式化器、Elasticsearch sink 和 File sink；`Girvs.ServiceGovernance` 则通过反射 Hook 自动追加 OTLP sink。

这让框架替业务系统决定了日志落点，导致不需要 ES、File 或 OTLP 的应用也携带对应依赖；同时默认 File 分流会把高等级日志重复写入多个文件。

## 目标

1. Girvs 核心只负责配置 Serilog 基础管道与 Console 输出兜底。
2. Girvs 不再引用 Elasticsearch、File 或 OpenTelemetry 的 Serilog sink/formatter 包。
3. 业务应用自行引用所需 sink 包，并通过 `appsettings.json` 的 `Serilog` 节配置 File、Elasticsearch、数据库、OTLP 或其他输出目标。
4. 默认日志级别为 `Information`，`Microsoft` 与 `System` 命名空间默认覆盖为 `Warning`。
5. Console 采用供人阅读的纯文本模板，时间格式为 `yyyy-MM-dd HH:mm:ss`；OTLP 等业务 sink 自行提供结构化输出。

## 非目标

- 不为 Girvs 提供 Elasticsearch、File、数据库或 OTLP sink 的封装 API。
- 不改变现有业务代码中的 `ILogger<T>` 日志调用与结构化消息模板。
- 不修改 `ServiceGovernanceModule` 的 OpenTelemetry metrics/tracing 配置及其 `UseOtlpExporter()`。
- 不新增或修改业务应用的日志落点配置。

## 运行时架构

### Girvs 核心默认配置

`GirvsHostBuilderManager.HostUseSerilog` 在配置 Serilog 时，先设置以下基础配置：

```csharp
configuration
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .WriteTo.Console(
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} || [{Level:u3}] || {SourceContext:l} || {Message:lj} || {Exception}{NewLine}"
    );
```

随后调用 `configuration.ReadFrom.Configuration(context.Configuration)`。因此：

- 未提供 `Serilog` 配置节的业务应用仍能在 Console 看到日志；
- 业务应用的 `Serilog:MinimumLevel` 可以覆盖框架默认级别；
- 业务应用的 `Serilog:WriteTo` 可以额外追加任意已引用的 sink；
- 框架的 Console sink 始终存在，不由业务 `WriteTo` 覆盖或移除。

### 业务应用扩展

业务应用负责引用所需的 Serilog sink 包。例如业务自行引用 `Serilog.Sinks.Elasticsearch` 后，在其 `appsettings.json` 中配置 `Serilog:WriteTo`。Girvs 核心保留 `Serilog.Settings.Configuration`，由该包在运行时发现业务应用已引用的 sink。

业务应用可同样选择 File、OTLP、数据库或其他 Serilog sink；Girvs 不感知、不过滤也不封装这些输出目标。

## 删除边界

删除以下旧机制及其依赖：

- `Girvs/Configuration/SerilogInitConfig.cs`；
- `ConfigurationDefaults.SerilogSettingFilePath`；
- `AppSettingsHelper.CreateSerilogConfig` 与 `ExistSerilogConfigFile`；
- `ServiceCollectionExtensions.AddBindSerilogConfiguation` 及其调用；
- `HostUseGirvsConfig` 对独立 `Serilog.json` 的加载；
- `TryAddGirvsOtlpSink` 与 `GirvsSerilogOtlpHook` 的反射契约；
- `Girvs.ServiceGovernance` 的 `Serilog.Sinks.OpenTelemetry` 包、两个 Hook 文件及其专用测试；
- `Girvs` 三个目标框架上的 `Serilog.Formatting.Elasticsearch`、`Serilog.Sinks.Elasticsearch`、`Serilog.Sinks.File` 包。

保留 `Serilog.AspNetCore`、`Serilog.Settings.Configuration` 与 `Serilog.Sinks.Console`。

## 验收标准

1. Girvs 与 Girvs.ServiceGovernance 的项目文件不再直接引用 Elasticsearch、File 或 OpenTelemetry 的 Serilog sink/formatter 包。
2. 仓库不再包含 `SerilogInitConfig`、`SerilogSettingFilePath`、`AddBindSerilogConfiguation`、`TryAddGirvsOtlpSink` 或 `GirvsSerilogOtlpHook` 的生产代码引用。
3. `HostUseSerilog` 在没有业务 `Serilog` 配置时，仍配置 Information/Warning 级别的 Console 输出；Console 模板使用 `HH:mm:ss`，且不包含 `||end`。
4. `HostUseSerilog` 仍调用 `ReadFrom.Configuration`，业务 `appsettings.json` 可追加自选 sink 并覆盖日志级别。
5. `dotnet build Girvs.slnx --nologo` 与 `dotnet test Girvs.slnx --nologo` 完成；若存在基线失败，新增失败不得归因于本次日志改造。

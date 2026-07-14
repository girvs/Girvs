# Girvs 框架引入 .NET Aspire 升级方案（设计文档）

- 日期：2026-07-13
- 状态：已确认
- 相关模块：新增 `Girvs.Aspire`；涉及 `Girvs`（宿主入口）、`Girvs.Consul`（共存）、`Girvs.Cache`、`Girvs.EventBus`、`Girvs.EntityFrameworkCore`（仅配置适配，不改代码）

## 1. 背景与目标

Girvs 是模块化 NuGet 框架（多目标 `net8.0;net9.0;net10.0`），当前：

- 服务注册与发现依赖 `Girvs.Consul`；
- 日志强制 Serilog（`GirvsHostBuilderManager.HostUseSerilog`）；
- 各模块（Cache/EventBus/EFCore）从各自配置节读取连接串；
- 无 OpenTelemetry 可观测性，无法接入 Aspire Dashboard。

目标：**全面拥抱 .NET Aspire**，让基于 Girvs 的服务能够以最小改动接入 Aspire 编排与可观测性体系，同时不破坏现有 net8/net9 下游服务。

## 2. 已确认的关键决策

| 决策点 | 结论 |
|--------|------|
| 升级定位 | 全面拥抱 Aspire：新增 `Girvs.Aspire` 模块 + AppHost 编排支持 |
| Consul 去留 | 共存过渡：`Girvs.Consul` 保留、继续发包，与 Aspire 服务发现互斥使用，下游按自己节奏迁移 |
| 目标框架 | `Girvs.Aspire` 仅 `net10.0`；net8/net9 下游不受影响、对新包不可见 |
| 日志方案 | 保留 Serilog 门面，通过 `Serilog.Sinks.OpenTelemetry` 桥接 OTLP，现有配置零改动 |
| AppHost 支持 | 只发 ServiceDefaults + 配置适配层；AppHost 项目由业务方自建，框架提供模板与文档，不发 `Girvs.Aspire.Hosting` 包 |

## 3. 架构设计

### 3.1 新模块 `Girvs.Aspire`（仅 net10.0）

承担三块职责：

**（1）ServiceDefaults**

- OpenTelemetry Traces/Metrics：`AspNetCore`、`HttpClient`、`Runtime` instrumentation，检测到 `OTEL_EXPORTER_OTLP_ENDPOINT` 环境变量（Aspire AppHost 自动注入）时启用 OTLP Exporter；
- Serilog → OTLP 日志桥接：追加 `Serilog.Sinks.OpenTelemetry`，同样以 `OTEL_EXPORTER_OTLP_ENDPOINT` 存在与否作为开关；无 Aspire 环境时日志行为与现在完全一致；
- 健康检查端点：按 Aspire 约定暴露 `/health`（全部检查）与 `/alive`（liveness），仅在开发环境或显式配置时对外开放；
- Aspire 服务发现：注册 `Microsoft.Extensions.ServiceDiscovery`，为 HttpClient 启用 `AddServiceDiscovery`；
- HttpClient 弹性：`AddStandardResilienceHandler()` 作为默认配置。

**（2）配置适配层**

把 AppHost 注入的 `ConnectionStrings__<name>` 环境变量，在配置构建阶段映射到 Girvs 各模块配置节：

| Aspire 资源名（约定） | 映射目标 |
|----------------------|----------|
| `girvs-db-master` | `DbConfig.DataConnectionConfig.MasterDataConnectionString` |
| `girvs-db-read-N`（N 从 0 起） | `DbConfig.DataConnectionConfig.ReadDataConnectionString[N]` |
| `girvs-cache` | `CacheConfig.DistributedCacheConfig.ConnectionString`（及 Redis 相关配置） |
| `girvs-eventbus-rabbitmq` | `EventBusConfig` RabbitMQ 连接信息 |
| `girvs-eventbus-redis` | `EventBusConfig.RedisConnectionString` |
| `girvs-eventbus-db` | `EventBusConfig.DbConnectionString` |

实现方式：注册一个 `IConfigurationSource`/后处理步骤，在 `HostUseGirvsConfig` 之后、容器构建之前执行；只在对应环境变量存在时覆盖，不存在时保持 appsettings.json 原值。业务代码与现有配置文件零改动。

**（3）模块入口**

`AspireModule : IAppModuleStartup`，沿用 Girvs 模块自发现机制，引用包即生效。

### 3.2 宿主入口挂接（`Girvs` 核心模块）

`GirvsHostBuilderManager.CreateGirvsWebApplicationBuilder` 中，通过 `#if NET10_0_OR_GREATER` + 反射探测 `Girvs.Aspire` 程序集：

- 存在：在 `builder.Build()` 前调用其 ServiceDefaults 挂接点与配置映射；
- 不存在：静默跳过，行为与当前版本完全一致。

核心模块不新增对 OpenTelemetry 等包的直接依赖。

### 3.3 与现有能力的关系

- **Girvs.Consul**：代码零改动，继续发包。启动时若检测到 `Girvs.Aspire` 与 `Girvs.Consul` 同时启用，记录一条警告日志（不阻断启动）。
- **Girvs.Cache / Girvs.EventBus / Girvs.EntityFrameworkCore**：模块代码不改，只接受配置适配层喂入的连接串。
- **CAP 分布式追踪**：`DotNetCore.Cap.OpenTelemetry` 作为 `Girvs.Aspire` 的可选注册项（检测到 EventBus 模块启用时自动加入 tracing source）。

### 3.4 AppHost 模板与约定文档

- 在 `docs/aspire/` 提供 AppHost 项目模板（`AddRedis("girvs-cache")`、`AddSqlServer(...).AddDatabase("girvs-db-master")`、`AddRabbitMQ("girvs-eventbus-rabbitmq")` 等）与资源命名约定说明；
- README 增补 Aspire 接入章节。

## 4. 工程约束

- 版本：纳入 `Directory.Build.props` 统一版本体系（当前 `10.0.0-rc.1`）；`Girvs.Aspire.csproj` 单独覆写 `<TargetFramework>net10.0</TargetFramework>`（注意用单数 `TargetFramework` 覆盖 props 中的复数 `TargetFrameworks`）。
- 依赖（均为 net10 兼容的最新稳定版）：
  - `OpenTelemetry.Extensions.Hosting`、`OpenTelemetry.Instrumentation.AspNetCore`、`OpenTelemetry.Instrumentation.Http`、`OpenTelemetry.Instrumentation.Runtime`、`OpenTelemetry.Exporter.OpenTelemetryProtocol`
  - `Microsoft.Extensions.ServiceDiscovery`（13.x）
  - `Microsoft.Extensions.Http.Resilience`
  - `Serilog.Sinks.OpenTelemetry`
  - `DotNetCore.Cap.OpenTelemetry`（可选特性）
- 不引入任何 `Aspire.Hosting.*` 包（那些属于业务方 AppHost 项目）。
- `nugetpublish.ps1` 增加 `Girvs.Aspire` 推送行。

## 5. 业务方升级路径

1. 升级 Girvs 系列包到新版本；
2. net10 服务追加 `Girvs.Aspire` 包引用；
3. 自建 AppHost 项目（按模板），资源命名遵循约定；
4. （可选）从 `Girvs.Consul` 切到 Aspire 服务发现时，移除 Consul 包引用与配置。

net8/net9 服务：无任何动作，无任何影响。

## 6. 测试策略

- `tests/` 下新增 `Girvs.Aspire.Tests`（net10.0）：
  - 配置适配层：各资源名 → 配置节映射、无环境变量时不覆盖原值、读库列表多条映射；
  - OTLP 开关：有/无 `OTEL_EXPORTER_OTLP_ENDPOINT` 时的注册行为；
  - Consul 共存警告逻辑。
- 手工验证：最小示例 AppHost + 一个 Girvs 示例服务，确认 Dashboard 中日志/追踪/指标可见、健康检查端点可用。

## 7. 明确不做的事（YAGNI）

- 不改 `Girvs.Consul` 任何代码，不打 `[Obsolete]`；
- 不为 net8/net9 提供 Aspire 能力；
- 不发 `Girvs.Aspire.Hosting` AppHost 侧扩展包；
- 不替换 Serilog、不改动 `serilogsetting.json` 配置体系；
- 不改 Cache/EventBus/EFCore 模块内部代码。

## 8. 风险

| 风险 | 缓解 |
|------|------|
| `TargetFramework` 单数覆写与 props 复数属性的交互问题 | csproj 中显式清空 `TargetFrameworks` 再设 `TargetFramework`，构建验证三 TFM 解决方案整体可编译 |
| 反射探测挂接点脆弱 | 挂接契约集中在一个静态类型名 + 方法名常量，加测试覆盖 |
| Serilog OTLP sink 与 `ReadFrom.Configuration` 的叠加顺序 | 桥接以代码方式追加 sink，不依赖 json 配置，验证两者共存 |
| Aspire 注入的连接串格式与 Girvs 期望格式不一致（如 Redis 带密码格式） | 适配层做格式归一化，测试覆盖常见格式 |

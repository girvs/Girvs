# Girvs 框架引入 .NET Aspire 升级方案（设计文档）

- 日期：2026-07-13（2026-07-14 修订：新增 AppHost 自动编排）
- 状态：已确认
- 架构图：仓库根目录 `Architect.png`（Girvs + .NET Aspire 模块化云原生架构图，六层：访问入口 / API Gateway / Aspire 编排与运行时 / 业务服务 / Girvs 基础组件能力 / 基础设施资源）
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
| AppHost 支持 | **（2026-07-14 修订）**发 `Girvs.Aspire.Hosting` 包：`AddGirvsProject<TProject>(name, typeof(根模块))` 依据 `[DependsOn]` 声明自动创建/复用资源并自动 `WithReference`；ServiceDefaults + 配置适配层不变 |
| 组件编排声明 | **（2026-07-14 新增）**服务定义根模块类并用 `[DependsOn(typeof(GirvsCacheModule), typeof(EventBusModule), ...)]` 声明使用的 Girvs 组件；覆盖全部自带含外部资源的组件（Cache、EventBus、EFCore；Quartz 当前为内存调度、无外部资源）。资源形态（broker 类型、库名/库类型）以服务 appsettings.json 为唯一真源 |

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
| `girvs-db-<name>` | `DbConfig.DataConnectionConfigs` 中 `Name == <name>` 项的 `MasterDataConnectionString`（`DbConfig` 是命名集合，`<name>` 默认为 `default`） |
| `girvs-db-<name>-read-<N>`（N 从 0 起连续） | 同名项的 `ReadDataConnectionString[N]` |
| `girvs-cache` | `CacheConfig.DistributedCacheConfig.ConnectionString` |
| `girvs-eventbus-rabbitmq` | AMQP URI（`amqp://user:pass@host:port/vhost`），解析后填入 `EventBusConfig.RabbitMqConfig` 的 HostName/Port/UserName/Password/VirtualHost |
| `girvs-eventbus-redis` | `EventBusConfig.RedisConfig.RedisConnectionString` |
| `girvs-eventbus-db` | `EventBusConfig.DbConnectionString` |

实现方式：`AspireModule.ConfigureServices` 执行时（此时 `Singleton<AppSettings>.Instance` 已完成绑定），通过 `AppSettings.ModuleConfigurations` 字典 + 反射按属性名写入，避免 `Girvs.Aspire` 对 EFCore/Cache/EventBus 模块产生硬引用（硬引用会把未使用的模块拉进业务服务并触发其模块启动逻辑）。`AspireModule.Order` 取极小值（-10000），确保映射先于其他模块消费配置。只在对应连接串存在时覆盖，不存在时保持 appsettings.json 原值。业务代码与现有配置文件零改动。

**（3）模块入口**

`AspireModule : IAppModuleStartup`，沿用 Girvs 模块自发现机制，引用包即生效。

### 3.2 宿主入口挂接（`Girvs` 核心模块）

除 Serilog 桥接外，`Girvs.Aspire` 的全部能力（OTel、服务发现、健康检查、配置映射）都通过标准 `IAppModuleStartup` 模块机制生效，核心模块无需改动。

唯一需要挂接核心的是 Serilog OTLP sink（Serilog 在模块机制运行之前、由 `HostUseSerilog` 配置）：在 `GirvsHostBuilderManager.HostUseSerilog` 的配置回调中，用 `Type.GetType("Girvs.Aspire.GirvsAspireSerilogHook, Girvs.Aspire")` 反射探测；程序集存在且检测到 `OTEL_EXPORTER_OTLP_ENDPOINT` 时调用其 `AddOtlpSink` 追加 sink，否则静默跳过（`Type.GetType` 对缺失程序集返回 null，无需条件编译）。核心模块不新增对 OpenTelemetry 等包的直接依赖。

### 3.3 与现有能力的关系

- **Girvs.Consul**：代码零改动，继续发包。启动时若检测到 `Girvs.Aspire` 与 `Girvs.Consul` 同时启用，记录一条警告日志（不阻断启动）。
- **Girvs.Cache / Girvs.EventBus / Girvs.EntityFrameworkCore**：模块代码不改，只接受配置适配层喂入的连接串。
- **CAP 分布式追踪**：`DotNetCore.Cap.OpenTelemetry` 作为 `Girvs.Aspire` 的可选注册项（检测到 EventBus 模块启用时自动加入 tracing source）。

### 3.4 AppHost 自动编排（`Girvs.Aspire.Hosting`，2026-07-14 修订）

**背景约束**：`IAppModuleStartup` 运行在服务进程内，`DistributedApplicationBuilder` 只存在于 AppHost 进程，二者无法直接互通。因此"组件自动编排"由 AppHost 侧的 `Girvs.Aspire.Hosting` 包实现，声明源是服务侧的 `[DependsOn]`。

**（1）`DependsOnAttribute`（放在 Girvs 核心包）**

```csharp
[DependsOn(
    typeof(GirvsCacheModule),
    typeof(EventBusModule),
    typeof(GirvsEntityFrameworkCoreModule))]
public class OrderModule;
```

服务定义根模块类（纯声明类，无需实现接口）标注其使用的 Girvs 组件模块类型；属性支持递归（模块可再依赖模块）。本期只作为 Aspire 编排的声明源，未来可扩展用于服务端模块加载排序/校验。

**（2）`AddGirvsProject`（AppHost 侧入口）**

```csharp
var builder = DistributedApplication.CreateBuilder(args);
builder.AddGirvsProject<Projects.Order_Api>("order-api", typeof(OrderModule));
builder.AddGirvsProject<Projects.User_Api>("user-api", typeof(UserModule));
builder.Build().Run();
```

内部流程：递归遍历 `DependsOn` 图（去重、防环）→ 按"模块 → 资源"注册表创建资源（**按资源名惰性创建、多服务共享同一实例**）→ 自动 `WithReference` + `WaitFor`。

可选配置通过 `GirvsProjectOptions` 回调传入，如 `builder.AddGirvsProject<Projects.Order_Api>("order-api", typeof(OrderModule), o => o.UseIsolatedCache = true)` 为该服务创建独立 Redis 实例。

**（3）模块 → 资源注册表（按模块类型全名匹配，Hosting 不引用各组件工程）**

| 模块类型 | 资源 |
|---|---|
| `Girvs.Cache.GirvsCacheModule` | 默认共享实例 `AddRedis("girvs-cache")`；通过 `GirvsProjectOptions.UseIsolatedCache` 可为服务创建独立实例 `girvs-cache-<service>`。无论共享还是独立，注入名固定为 `girvs-cache`（`WithReference(redis, connectionName: "girvs-cache")`），服务端映射不变（对应架构图"同一 Cache 组件可映射共享 Redis 也可映射独立实例"；按 DB Index 隔离留待后续版本） |
| `Girvs.EventBus.EventBusModule` | 读服务 appsettings 的 `EventBusType`：RabbitMQ → 共享 `AddRabbitMQ("girvs-eventbus-rabbitmq")`；Redis → 共享 `AddRedis("girvs-eventbus-redis")`；Kafka → 警告并跳过（云 Kafka 通常直连外部集群，且服务端 `KafkaConfig` 含 SASL 等云端专属配置，不适合本地容器化） |
| `Girvs.EntityFrameworkCore.GirvsEntityFrameworkCoreModule` | **每服务独立数据库**（对应架构图 orderdb/userdb/inventorydb）：共享数据库服务器（MySql → `AddMySql("girvs-mysql")`、MsSql → `AddSqlServer("girvs-sqlserver")`）+ 每服务独立 database 资源 `girvs-db-<service>-<Name>`（实际库名 `<service>_<Name>`）；用 `WithReference(db, connectionName: "girvs-db-<Name>")` 把注入名固定为服务端映射约定，服务端零感知 |
| 其余模块（Quartz、AutoMapper、Driven、DynamicWebApi 等） | 无外部资源，参与依赖图但不产生资源 |

`AddGirvsProject` 对 Web API 与 Worker/Background Service 项目同样适用（都是 Aspire `ProjectResource`），架构图第 4 层的四类服务用同一入口编排。

资源形态以服务的 `appsettings.json`（`ModuleConfigurations` 节，AppHost 通过 `IProjectMetadata.ProjectPath` 定位服务目录）为唯一真源；文件或字段缺失时对该资源警告并跳过（业务方可用原生 Aspire API 按命名约定手动补）。注册表开放扩展点，业务方可为自定义模块类型注册资源贡献器。

`girvs-eventbus-db`（CAP 存储库）不自动创建——通常复用业务库，需要独立库时手动添加。

**（4）工程约束**：`typeof(OrderModule)` 要求 AppHost 能编译引用根模块所在程序集（Aspire 的 `IsAspireProjectResource="true"` 引用不暴露类型）。推荐把根模块类放在服务的 Application 层或共享程序集，AppHost 对其加普通 `ProjectReference`；接入文档写明该模式。

### 3.5 接入文档

- 在 `docs/aspire/` 提供接入指南：`[DependsOn]` 声明方式、`AddGirvsProject` 用法、资源命名约定、根模块程序集引用模式；
- README 增补 Aspire 接入章节。

## 4. 工程约束

- 版本：纳入 `Directory.Build.props` 统一版本体系（当前 `10.0.0-rc.1`）；`Girvs.Aspire.csproj` 单独覆写 `<TargetFramework>net10.0</TargetFramework>`（注意用单数 `TargetFramework` 覆盖 props 中的复数 `TargetFrameworks`）。
- 依赖（均为 net10 兼容的最新稳定版）：
  - `OpenTelemetry.Extensions.Hosting`、`OpenTelemetry.Instrumentation.AspNetCore`、`OpenTelemetry.Instrumentation.Http`、`OpenTelemetry.Instrumentation.Runtime`、`OpenTelemetry.Exporter.OpenTelemetryProtocol`
  - `Microsoft.Extensions.ServiceDiscovery`（13.x）
  - `Microsoft.Extensions.Http.Resilience`
  - `Serilog.Sinks.OpenTelemetry`
  - `DotNetCore.Cap.OpenTelemetry`（可选特性）
- `Girvs.Aspire`（服务端包）不引入任何 `Aspire.Hosting.*` 包；`Girvs.Aspire.Hosting`（AppHost 端包，仅 net10.0）引入 `Aspire.Hosting` 及 Redis/RabbitMQ/MySql/SqlServer 集成包（13.x），并引用 Girvs 核心（取 `DependsOnAttribute` 类型），但不引用任何 Girvs 组件工程（模块按类型全名字符串匹配）。
- `nugetpublish.ps1` 增加 `Girvs.Aspire`、`Girvs.Aspire.Hosting` 推送行。

## 5. 业务方升级路径

1. 升级 Girvs 系列包到新版本；
2. net10 服务追加 `Girvs.Aspire` 包引用，并在可被 AppHost 引用的程序集中定义根模块类 + `[DependsOn]` 声明；
3. 自建 AppHost 项目，引用 `Girvs.Aspire.Hosting`，每个服务一行 `AddGirvsProject<TProject>(name, typeof(根模块))`；
4. （可选）从 `Girvs.Consul` 切到 Aspire 服务发现时，移除 Consul 包引用与配置。

net8/net9 服务：无任何动作，无任何影响。

## 6. 测试策略

- `tests/` 下新增 `Girvs.Aspire.Tests`（net10.0）：
  - 配置适配层：各资源名 → 配置节映射、无环境变量时不覆盖原值、读库列表多条映射；
  - OTLP 开关：有/无 `OTEL_EXPORTER_OTLP_ENDPOINT` 时的注册行为；
  - `DependsOnAttribute` 声明语义。
- `tests/` 下新增 `Girvs.Aspire.Hosting.Tests`（net10.0）：
  - `DependsOn` 图遍历：递归、去重、防环；
  - 各贡献器：Cache → Redis 资源、EventBusType 分支、多命名库多类型创建、appsettings 缺失时跳过且不抛异常；
  - 资源共享：两个服务声明同一组件时只创建一个资源实例。
- Consul 共存警告逻辑由手工验证覆盖（测试工程不引用 Girvs.Consul，自动化测试只能覆盖"未加载则不警告"路径，价值有限）。
- 手工验证：最小示例 AppHost + 一个 Girvs 示例服务，确认 Dashboard 中日志/追踪/指标可见、健康检查端点可用。

## 7. 明确不做的事（YAGNI）

- 不改 `Girvs.Consul` 任何代码，不打 `[Obsolete]`；
- 不为 net8/net9 提供 Aspire 能力；
- 不替换 Serilog、不改动 `serilogsetting.json` 配置体系；
- 不改 Cache/EventBus/EFCore/Quartz 模块内部代码（`DependsOnAttribute` 加在核心包，组件模块本身不加声明）；
- 不自动创建 `girvs-eventbus-db`（CAP 存储通常复用业务库）；
- Kafka 不做本地容器编排（云 Kafka 场景直连外部集群）；
- `DependsOn` 本期不改变服务端模块加载顺序（仍由 `Order` 属性决定）。

## 8. 风险

| 风险 | 缓解 |
|------|------|
| `TargetFramework` 单数覆写与 props 复数属性的交互问题 | csproj 中显式清空 `TargetFrameworks` 再设 `TargetFramework`，构建验证三 TFM 解决方案整体可编译 |
| 反射探测挂接点脆弱 | 挂接契约集中在一个静态类型名 + 方法名常量，加测试覆盖 |
| Serilog OTLP sink 与 `ReadFrom.Configuration` 的叠加顺序 | 桥接以代码方式追加 sink，不依赖 json 配置，验证两者共存 |
| Aspire 注入的连接串格式与 Girvs 期望格式不一致（如 Redis 带密码格式） | 适配层做格式归一化，测试覆盖常见格式 |
| AppHost 读取服务 appsettings.json 与服务运行时配置行为不完全一致（环境变量覆盖、环境专属文件） | 只读基础 appsettings.json 判定资源形态（形态字段通常不随环境变化）；判定失败降级为警告 + 跳过，可手动补资源 |
| 根模块程序集无法被 AppHost 引用（服务为单程序集 exe） | 文档提供两种模式：根模块放共享程序集（推荐）；或对服务加 `IsAspireProjectResource="false"` 的普通引用 |
| `DependsOn` 声明与实际包引用不一致（声明了未引用的模块或反之） | 声明多了只会多创建资源（服务端映射按需生效），声明少了资源缺失在启动时即可发现；后续版本可在服务端加一致性校验 |

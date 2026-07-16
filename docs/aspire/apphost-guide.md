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
   - 暴露 `/health` 与 `/alive` 健康检查端点；
   - HttpClient 默认启用 Aspire 服务发现与标准弹性策略。

   连接串映射（见下文约定）不依赖 `Girvs.Aspire` 包：`Girvs.Cache`/`Girvs.EventBus`/`Girvs.EntityFrameworkCore` 各自在自己的模块启动逻辑中读取 AppHost 注入的连接串并覆盖自身配置，只要服务被 AppHost 启动即生效。

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

## 6. 权威参照实现（推荐照抄）

`samples/` 下有一个能端到端跑通的最小参照系统（AppHost + 2 个 Web 服务 + 1 个后台 Worker），覆盖缓存、数据库、事件总线、服务发现、共享配置、生产发布清单全部能力，是接入与迁移的权威模板。运行方式与各自检端点见 `samples/README.md`。以下几点是接入时最容易踩的：

- **服务需 `Properties/launchSettings.json`**：Aspire 依据 `applicationUrl` 分配/代理端点；缺失会导致所有服务回退默认 5000 端口冲突。
- **控制器需在 `Startup.Configure` 显式映射**：`CreateGirvsWebApplicationBuilder` 模型下，框架的 `ConfigureEndpointRouteBuilder` 只映射模块端点（如 `/health`），普通 MVC 控制器需 `if (app is IEndpointRouteBuilder e) e.MapControllers();`。
- **事件总线（CAP）**：CAP 存储需一个库，服务需同时声明 EFCore（让 Aspire 建库）并把 `EventBusConfig.DbConnectionString` 设为某个 `DataConnectionConfigs[].Name` 以复用其连接；订阅者的 `CapHeader` 参数必须标 `[FromCap]`；发布依赖 `IGirvsClaimManager`（真实服务由 `Girvs.AuthorizePermission` 提供）。
- **本机若配了 http 代理**：启动 AppHost 须带 `no_proxy=localhost,127.0.0.1,::1`，否则 Aspire 的 localhost 通信被塞进代理而失败。

## 7. 共享配置注入（配置统一）

在 AppHost 一处声明跨服务通用配置，所有 `AddGirvsProject` 的服务自动接收：

```csharp
var jwt = builder.AddParameter("jwt-secret", secret: true);
builder.AddGirvsSharedConfiguration(shared =>
{
    shared.AddSetting("Logging:LogLevel:Default", "Information"); // 非敏感 → 普通配置/ConfigMap
    shared.AddSecret("Jwt:Secret", jwt);                          // 敏感 → K8s Secret
});
```

服务端零改动：共享配置以环境变量注入，优先级高于服务自身 appsettings.json（同名 key 被覆盖）。不支持运行时热更新，改配置需重新发布。

## 8. 网关（Girvs.Aspire.Gateway）

自建 YARP 网关的服务发现与路由生成，与 Aspire AppHost 编排是两个独立话题：AppHost 负责本地/CI 环境编排各服务与基础设施，网关面向**生产多实例部署**（CentOS/Docker 或 K8s）做流量入口。两者可以同时使用：AppHost 跑参照实现验证接入，网关包直接用于生产网关进程。

- 用法、约定路由规则、K8s RBAC 清单示例见 `Girvs.Aspire.Gateway/README.md`；
- 端到端可跑通的验证系统（kind 集群 + 网关 + 两个 dummy 后端，验证过 K8s watch 动态路由的秒级增删）见 `samples/gateway-k8s/`（`Gateway/` 最小网关项目 + `Dockerfile` + `k8s.yaml`）；
- 部署形态与发现源对应关系：CentOS/Docker → `GatewayDiscoveryType.Consul`（轮询）；K8s → `GatewayDiscoveryType.Kubernetes`（watch，事件驱动，秒级感知）。

## 9. 生产资源（阿里云托管）

`dotnet run --project <AppHost> -- --operation publish ...`（发布模式）下，Cache/EventBus/EFCore 自动改为**引用外部连接串**，不在集群内新建容器。连接串按资源名从部署参数/配置提供（`ConnectionStrings:<资源名>` 或部署流水线）：

| 资源名 | 服务端注入名 |
|---|---|
| `girvs-cache` | `girvs-cache` |
| `girvs-eventbus-rabbitmq` / `girvs-eventbus-redis` | 同名 |
| `girvs-db-<service>-<name>`（阿里云 RDS 已建好的库） | `girvs-db-<name>` |

本地开发（Run 模式）行为不变：仍自动拉起 Docker 容器。生成的 `aspire-manifest.json` 中业务服务为 `project.v0`、基础设施为 `parameter.v0`+`secret`。

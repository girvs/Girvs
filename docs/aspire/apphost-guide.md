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

# 共享配置文件分发机制设计

## 背景与目标

`Resources` 统一资源模型(见 `2026-07-16-connection-resource-model-design.md`)落地后,资源配置仍分散在各服务自己的 `appsettings.json` 中。本设计引入**独立共享配置文件**,统一三种部署形态下的资源配置分发:

- **单体 / 老 K8s 模式**:行为完全不变;
- **K8s 生产**:共享文件通过 ConfigMap volume 挂载;
- **Aspire 编排**:AppHost 显式编排基础设施容器,并把实际地址写入共享文件分发给各服务。

核心原则:

1. 服务端获取连接信息**只有一条路**——`AppSettings.Resources[ConnectionRef]` 组装连接串,不存在任何 `ConnectionStrings__girvs-*` 环境变量注入与覆盖;
2. **服务本地配置优先级最高**,由 .NET 标准配置合并规则天然保证;
3. AppHost 编排什么容器由开发者显式声明,框架不按 `[DependsOn]` 推断;**服务用不用、用哪个资源,由服务自己的 `ConnectionRef` 决定**;
4. Girvs 各功能模块(Cache / EF / EventBus 等)的 Resources 消费逻辑不变,仅删除 Aspire 覆盖分支。

## 配置优先级链

服务端配置源加载顺序(低 → 高):

```
girvs.shared.json(共享文件,GIRVS_SHARED_CONFIG 指向)
  < appsettings.json(服务自己的)
  < serilogsetting.json
  < appsettings.{Environment}.json(Development / Production)
  < 环境变量
  < 命令行参数
```

## 服务端改动(Girvs 核心,仅一处)

`GirvsHostBuilderManager.HostUseGirvsConfig` 在加载 `appsettings.json` **之前**,读取环境变量 `GIRVS_SHARED_CONFIG`(值为共享文件路径),非空则:

```csharp
config.AddJsonFile(sharedConfigPath, optional: true, reloadOnChange: true);
```

- 未设置该环境变量时行为与现在完全一致,单体应用、老 K8s 部署零影响;
- `optional: true`:文件缺失不报错,服务按本地配置运行;
- `reloadOnChange: true`:K8s 下 ConfigMap 更新后可热重载(注意:绑定进 `Singleton<AppSettings>` 的值仍是启动时快照,热重载仅对直接读 `IConfiguration` 的消费方生效)。

## AppHost 端(Girvs.Aspire.Hosting 重构)

### API 形态

```csharp
// 显式编排容器,并登记为 Girvs 资源;资源名即服务端 ConnectionRef 引用的键
var redis = builder.AddRedis("platform-redis").AsGirvsResource();   // Type 推断为 "redis"
var mysql = builder.AddMySql("primary-mysql").AsGirvsResource();    // Type 推断为 "mysql"

// 添加 Girvs 服务:注入 GIRVS_SHARED_CONFIG,并 WaitFor 所有已登记的 Girvs 资源
builder.AddGirvsProject<Projects.ServiceA>("service-a");
```

- `AddGirvsProject` 签名简化为 `AddGirvsProject<TProject>(name)`,不再需要 `rootModuleType` 参数;
- 服务是否使用某资源、使用哪个,由服务自己配置中的 `ConnectionRef` 指向资源键决定,AppHost 不感知。

### AsGirvsResource 行为

订阅 Aspire endpoint 分配生命周期事件,容器地址确定后按资源类型生成 `Resource` 条目,Settings 键与各模块 `BuildConnectionString` 的消费约定对齐:

| Aspire 资源 | Type | Settings 键 |
|---|---|---|
| `AddRedis` | `redis` | `Endpoints`(host:port,多节点逗号分隔)、`Ssl` |
| `AddMySql` | `mysql` | `Host`、`Port`、`UserName`、`Password`、可选 `Database` |
| `AddSqlServer` | `sqlserver` | `Host`、`Port`、`Database`、`UserName`、`Password` |
| `AddRabbitMQ` | `rabbitmq` | `HostName`、`Port`、`UserName`、`Password`、`VirtualHost` |
| `AddKafka` | `kafka` | `BootstrapServers` |

`AsGirvsResource` 提供可选参数覆盖 Type 与补充 Settings(如 `redis-synchronized-memory` 场景)。

### 共享文件生成与分发

- **run 模式**:框架把 AppHost 目录手写的 `girvs.shared.json` 与登记资源合并(登记资源覆盖同名键),生成运行时文件 `obj/girvs.shared.runtime.json`,向每个 Girvs 服务注入 `GIRVS_SHARED_CONFIG=<生成文件绝对路径>`。**不改写手写文件**,避免动态端口造成 git 噪音;
- 手写 `girvs.shared.json` 中已有、且未被 `AsGirvsResource` 登记覆盖的资源(如指向公司现有 Redis 的条目)原样透传;
- **publish 模式**:不生成文件,注入约定路径 `GIRVS_SHARED_CONFIG=/girvs-config/girvs.shared.json`,由生产 ConfigMap 提供内容;
- 时序:`AddGirvsProject` 对所有已登记 Girvs 资源自动 `WaitFor`,保证服务启动读取共享文件时容器已就绪、文件已写入。

## 生产部署约定(K8s)

- 共享配置维护为一个 ConfigMap,挂载到各服务容器的 `/girvs-config/girvs.shared.json`,与 publish 模式注入的 `GIRVS_SHARED_CONFIG` 路径一致;
- `Resources` 写生产基础设施真实地址;服务自身参数放镜像内 `appsettings.Production.json`,按优先级链天然覆盖共享值;
- 现有"volume 直接挂 `appsettings.json`"的老方式继续可用,两套并存、平滑迁移。

## 回滚 / 删除清单

### 服务端各模块(删除 Aspire 连接串覆盖分支)

- `Girvs.Cache/Configuration/CacheConfig.cs`:`GetConnectionString` 删除 `configuration.GetConnectionString("girvs-cache")` 优先分支,仅保留 `DistributedCacheConfig.BuildConnectionString(resource)`;
- `Girvs.EntityFrameworkCore/Configuration/IDataConnectionStringProvider.cs`:删除 `girvs-db-{Name}` / `girvs-db-{Name}-read-{N}` 覆盖分支;
- `Girvs.EventBus/EventBusModule.cs`:删除 `girvs-eventbus-db` 覆盖分支(Redis Transport 无覆盖分支,无需处理);
- 对应测试 `tests/Girvs.Aspire.Tests/AspireConnectionStringOverrideTests.cs` 删除。

### Girvs.Aspire.Hosting(整套自动推断机制删除)

- `DependsOnGraph.cs`、`GirvsResourceRegistry.cs`、`Contributors/`(全部)、`GirvsSharedConfiguration.cs`、`GirvsSharedConfigurationExtensions.cs`、`GirvsServiceAppSettings.cs`、`GirvsProjectOptions.cs`;
- `GirvsProjectExtensions.cs` 重写为新 API;
- `tests/Girvs.Aspire.Hosting.Tests` 按新 API 重写;
- `samples/`(AppHost 与各服务配置)按新机制调整,`docs/aspire/apphost-guide.md` 同步更新。

## 不变的部分

- `Resources` 统一模型与各模块 `ConnectionRef` 消费、组装逻辑;
- `Girvs.Aspire`(服务端模块):OpenTelemetry、健康检查、服务发现均不动;
- K8s 老部署方式。

## 错误处理

- `ConnectionRef` 指向的资源不存在或 Type 不匹配:模块抛 `GirvsException`(现状,不变);
- `GIRVS_SHARED_CONFIG` 指向的文件不存在:静默跳过(optional),服务按本地配置运行;
- `AsGirvsResource` 无法从 Aspire 资源提取地址信息:AppHost 启动时抛异常,快速失败。

## 测试策略

- `tests/Girvs.Aspire.Tests`:服务端优先级链(共享文件 < appsettings.json < 环境文件)、`GIRVS_SHARED_CONFIG` 缺失/文件缺失的降级行为;
- `tests/Girvs.Aspire.Hosting.Tests`:`AsGirvsResource` 各类型 Settings 生成、手写文件与登记资源合并规则、run/publish 模式注入的环境变量、`WaitFor` 接线;
- `samples/` 端到端:ServiceA/B 通过共享文件拿到 Redis/MySQL 地址,`/selfcheck/*` 自检通过。

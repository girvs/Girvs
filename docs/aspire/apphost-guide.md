# Girvs 服务接入 .NET Aspire 指南

适用范围：目标框架为 net10.0 的 Girvs 服务。net8/net9 服务请继续使用 Girvs.Consul。
架构总览见仓库根目录 `Architect.png`。

## 1. 配置分发模型(核心概念)

连接资源(Redis / MySQL / RabbitMQ / Kafka 等)统一在 `Resources` 节点声明,各模块通过 `ConnectionRef` 引用并自行组装连接串(见 `docs/superpowers/specs/2026-07-16-connection-resource-model-design.md`)。三种部署形态共用同一份优先级链:

```
girvs.shared.json(共享文件,GIRVS_SHARED_CONFIG 指向)
  < appsettings.json(服务自己的)
  < appsettings.{Environment}.json(Development / Production)
  < 环境变量 / 命令行
```

- **单体 / 无共享文件**:`Resources` 直接写在服务 `appsettings.json`,行为与从前一致;
- **Aspire 本地编排**:AppHost 拉起容器,把实际地址就地更新进 AppHost 目录下的 `girvs.shared.json`(不存在则创建)并注入 `GIRVS_SHARED_CONFIG`;
- **K8s 生产**:共享文件由 ConfigMap 挂载到约定路径 `/girvs-config/girvs.shared.json`。

服务本地配置永远优先于共享文件——由 .NET 标准配置合并规则保证,框架无任何特殊覆盖逻辑。

## 2. 服务端接入(Girvs.Aspire)

1. 服务项目追加包引用:

```xml
<PackageReference Include="Girvs.Aspire" Version="10.0.0-rc.1" />
```

2. 无需修改任何代码。`AspireModule` 会随 Girvs 模块机制自动生效:
   - 在 Aspire 环境(存在 `OTEL_EXPORTER_OTLP_ENDPOINT` 环境变量)下自动上报日志/追踪/指标到 Dashboard;
   - 暴露 `/health` 与 `/alive` 健康检查端点;
   - HttpClient 默认启用 Aspire 服务发现与标准弹性策略。

3. 服务的 `appsettings.json` 中各模块以 `ConnectionRef` 指向资源键(资源地址无需本地声明,由共享文件提供):

```json
{
  "ModuleConfigurations": {
    "CacheConfig": {
      "DistributedCacheConfig": { "Enabled": true, "ConnectionRef": "platform-redis" }
    },
    "DbConfig": {
      "DataConnectionConfigs": [
        { "Name": "default", "ConnectionRef": "order-mysql", "ReadConnectionRefs": [] }
      ]
    },
    "EventBusConfig": {
      "PersistenceConnectionRef": "order-mysql",
      "TransportConnectionRef": "platform-rabbitmq"
    }
  }
}
```

4. 与 Girvs.Consul 互斥:迁移到 Aspire 服务发现后,移除 Girvs.Consul 包引用与相关配置。

## 3. AppHost 项目(Girvs.Aspire.Hosting)

新建 Aspire AppHost 项目(`dotnet new aspire-apphost`),引用 `Girvs.Aspire.Hosting` 包。**先编排基础资源,再添加服务**:

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// 显式编排容器并登记为 Girvs 资源:资源名即服务端 ConnectionRef 引用的键
builder.AddRedis("platform-redis").AsGirvsResource();
builder.AddMySql("mysql-server").AddDatabase("order-mysql", databaseName: "order").AsGirvsResource();
builder.AddRabbitMQ("platform-rabbitmq").AsGirvsResource();

builder.AddGirvsProject<Projects.Order_Api>("order-api");
builder.AddGirvsProject<Projects.User_Api>("user-api");

builder.Build().Run();
```

- `AsGirvsResource()`:容器地址确定后就地更新 AppHost 目录下 `girvs.shared.json`(不存在则创建,存在则只更新 `Resources` 节点、其余手写内容原样保留)的 `Resources` 节点;Type 自动推断(redis/mysql/sqlserver/rabbitmq/kafka),可用参数覆盖:`AsGirvsResource(type: "redis-synchronized-memory")`,或补充 Settings:`AsGirvsResource(settings: new Dictionary<string,string> { ["Ssl"] = "true" })`;
- `AddGirvsProject<TProject>(name)`:注入 `GIRVS_SHARED_CONFIG` 并对所有已登记资源 `WaitFor`;
- **AppHost 不感知服务依赖**:服务用不用某资源、用哪个,完全由服务自己配置里的 `ConnectionRef` 决定;
- Worker / Background Service 项目同样用 `AddGirvsProject` 编排。

### 资源类型与 Settings 映射

| Aspire 编排 | Type | 生成的 Settings 键 |
|---|---|---|
| `AddRedis` | `redis` | `Endpoints`(密码以 `,password=...` 附于其内) |
| `AddMySql` / `.AddDatabase` | `mysql` | `Host`、`Port`、`UserName`、`Password`、`Database`(database 资源才有) |
| `AddSqlServer` / `.AddDatabase` | `sqlserver` | 同上(UserName 为 `sa`) |
| `AddRabbitMQ` | `rabbitmq` | `HostName`、`Port`、`UserName`、`Password`、`VirtualHost` |
| `AddKafka` | `kafka` | `BootstrapServers` |

### 扩展其他资源类型

内置预设只覆盖上表五种;其它资源类型(sqlite、mongodb、elasticsearch 等)有两种接入方式:

- **一次性场景**:`AsGirvsResource(type: "...", settings: ...)` 显式传 Type 与 Settings;
- **可复用扩展**:实现 `IGirvsResourceSettingsProvider`——与框架模块机制一样通过 TypeFinder 反射**自动发现,定义即生效,无需注册**(要求公共无参构造,定义在 AppHost 已加载的程序集,通常就是 AppHost 项目本身);自定义提供程序优先于内置预设匹配,可接管内置类型:

```csharp
public class SqliteSettingsProvider : IGirvsResourceSettingsProvider
{
    public Task<Resource> TryBuildAsync(IResource resource, CancellationToken ct)
    {
        if (resource is not SqliteResource sqlite) return Task.FromResult<Resource>(null);
        return Task.FromResult(new Resource
        {
            Type = "sqlite",
            Settings = new Dictionary<string, string> { ["DataSource"] = sqlite.DatabasePath },
        });
    }
}
```

参照实现见 `samples/Sample.AppHost/`:`SqliteResource.cs`(无容器本地文件,实现 `IResourceWithoutLifetime` 免 WaitFor)与 `MongoSettingsProvider.cs`(官方集成包容器)。服务端对应模块的 `BuildConnectionString` 需要认识该 Type 才能消费(资源模型的 Type 是开放字符串,由消费模块解释)。

### 共享文件(girvs.shared.json)

AppHost 目录下的 `girvs.shared.json` 既是手写文件也是运行产物:不存在时 AppHost 首次启动自动创建;已存在时每次启动只**就地更新** `Resources` 节点,其余手写节点(如日志等级)原样保留。想连公司现有 Redis 而不拉容器,就在该文件里写死该资源、AppHost 里不编排它——`AsGirvsResource` 登记的同名资源会覆盖手写值,未登记的手写资源原样透传。该文件默认随仓库提交,记录了最近一次本地运行的资源快照;如不希望其内容随每次运行变动产生 git diff 噪音,可自行将其加入 `.gitignore`。

## 4. 生产部署(K8s)

- 共享配置维护为一个 ConfigMap,挂载到各服务容器 `/girvs-config/girvs.shared.json`(publish 模式注入的 `GIRVS_SHARED_CONFIG` 即指向该路径);`Resources` 写生产基础设施(如阿里云托管 Redis/RDS)的真实地址;
- 服务自身参数放镜像内 `appsettings.Production.json`,按优先级链天然覆盖共享值;
- publish 模式下 AppHost 不更新共享文件、不 WaitFor;若不希望容器资源进入发布清单,可用 `builder.ExecutionContext.IsRunMode` 包裹编排代码;
- 现有"volume 直接挂 `appsettings.json`"的老方式继续可用,两套并存、平滑迁移。

## 5. 根模块与程序集引用

`AddGirvsProject` 不再需要根模块类型参数;AppHost 只需 `Girvs.Aspire.Hosting` 一个普通引用(`IsAspireProjectResource="false"`)加各服务的编排引用(`IsAspireProjectResource="true"`)。服务端模块声明(`[DependsOn]`)只影响服务自身的模块启动,与 AppHost 无关。

## 6. 常见问题

- **Dashboard 里看不到日志?** 确认服务由 AppHost 启动(`OTEL_EXPORTER_OTLP_ENDPOINT` 由 Aspire 自动注入);框架保留 Serilog,OTLP 是追加的 sink,本地文件/控制台日志不受影响。
- **与 Consul 共存告警?** 迁移期间可忽略;完成迁移后移除 Girvs.Consul。
- **改了 girvs.shared.json 服务没生效?** 共享文件以 `reloadOnChange` 加载,但绑定进 `Singleton<AppSettings>` 的值是启动时快照,改资源配置需重启服务。
- **服务本地为什么不再回写 appsettings.json?** 设置了 `GIRVS_SHARED_CONFIG` 时框架跳过 AppSettings 回写,避免把共享文件中的动态地址固化到本地文件反向覆盖。
- **数据库连接串格式?** 由各模块的 `BuildConnectionString` 按 `Resources` 的 Settings 组装,与手工连接串等价。
- **本机若配了 http 代理**:启动 AppHost 须带 `no_proxy=localhost,127.0.0.1,::1`,否则 Aspire 的 localhost 通信被塞进代理而失败。

## 7. 权威参照实现(推荐照抄)

`samples/` 下有一个能端到端跑通的最小参照系统(AppHost + 2 个 Web 服务 + 1 个后台 Worker),覆盖缓存、数据库、事件总线、服务发现、共享配置文件全部能力,是接入与迁移的权威模板。运行方式与各自检端点见 `samples/README.md`。以下几点是接入时最容易踩的:

- **服务需 `Properties/launchSettings.json`**:Aspire 依据 `applicationUrl` 分配/代理端点;缺失会导致所有服务回退默认 5000 端口冲突。
- **控制器需在 `Startup.Configure` 显式映射**:`CreateGirvsWebApplicationBuilder` 模型下,框架的 `ConfigureEndpointRouteBuilder` 只映射模块端点(如 `/health`),普通 MVC 控制器需 `if (app is IEndpointRouteBuilder e) e.MapControllers();`。
- **事件总线(CAP)**:CAP 存储需一个库,`EventBusConfig.PersistenceConnectionRef` 通常与业务库指向同一个 mysql 资源;订阅者的 `CapHeader` 参数必须标 `[FromCap]`;发布依赖 `IGirvsClaimManager`(真实服务由 `Girvs.AuthorizePermission` 提供)。

## 8. 网关(Girvs.Aspire.Gateway)

自建 YARP 网关的服务发现与路由生成,与 Aspire AppHost 编排是两个独立话题:AppHost 负责本地/CI 环境编排各服务与基础设施,网关面向**生产多实例部署**(CentOS/Docker 或 K8s)做流量入口。两者可以同时使用:AppHost 跑参照实现验证接入,网关包直接用于生产网关进程。

- 用法、约定路由规则、K8s RBAC 清单示例见 `Girvs.Aspire.Gateway/README.md`;
- 端到端可跑通的验证系统(kind 集群 + 网关 + 两个 dummy 后端,验证过 K8s watch 动态路由的秒级增删)见 `samples/gateway-k8s/`(`Gateway/` 最小网关项目 + `Dockerfile` + `k8s.yaml`);
- 部署形态与发现源对应关系:CentOS/Docker → `GatewayDiscoveryType.Consul`(轮询);K8s → `GatewayDiscoveryType.Kubernetes`(watch,事件驱动,秒级感知)。

# Girvs + .NET Aspire 参照实现（Reference Sample）

这是 Girvs 框架接入 .NET Aspire 的**活体参照实现**：一个能端到端跑通的最小系统，既是框架 Aspire 集成的验证场，也是业务系统迁移的抄写模板。

## 组成

| 项目 | 说明 | 使用的 Girvs 组件 |
|------|------|------|
| `Sample.AppHost` | Aspire 编排入口（`AsGirvsResource` 登记资源 + `AddGirvsProject` 分发共享配置文件）；含 `IGirvsResourceSettingsProvider` 两类扩展示例：`SqliteResource.cs`（无容器本地文件）与 `MongoSettingsProvider.cs`（官方集成包容器） | Girvs.Aspire.Hosting |
| `Sample.ServiceA` | Web 服务：缓存 + 数据库 + 服务发现调用 ServiceB（HttpClient 与 Refit 两种方式） | ServiceGovernance、Cache、EntityFrameworkCore、Refit、OpenApi |
| `Sample.ServiceB` | Web 服务：事件总线（CAP）+ 数据库（CAP 存储） | ServiceGovernance、EventBus、EntityFrameworkCore、OpenApi |
| `Sample.Gateway` | 本地网关：通过统一服务目录（Aspire 发现源）发现 ServiceA/ServiceB，按 `/{service}/...` 约定转发；在 `/girvs_swagger` 集中展示各服务 OpenAPI 文档 | Gateway、ServiceGovernance |
| `gateway-k8s/` | Kubernetes 服务目录 + 网关动态路由验证（kind 集群），见 [gateway-k8s/README.md](gateway-k8s/README.md) | Gateway、ServiceGovernance |

> 独立解决方案 `GirvsAspireSample.slnx`，不进主 `Girvs.slnx`（避免"构建即打包"）。样例用 `ProjectReference` 直接引框架源码，改框架代码立即生效；样例统一为 `net10.0`。

服务名由 `ServiceNameResolver` 统一生成：`Projects.Sample_ServiceA` → `sample-servicea`，`Projects.Sample_ServiceB` → `sample-serviceb`。

## 资源配置

各服务在自己的 `appsettings.json` 中通过 `ConnectionRef` 引用资源：

| 服务 | 引用 |
|---|---|
| ServiceA | 缓存 `sample-redis`，数据库 `sample-mysql` |
| ServiceB | 数据库 `sample-mysql`，CAP 持久化 `eventbus-mysql`，CAP 传输 `sample-redis`（Redis Streams） |

资源的实际地址来自 `Sample.AppHost/girvs.shared.json` 的 `Resources` 节点，有两种提供方式：

1. **使用已有基础设施（当前默认）**：`Program.cs` 中的容器编排代码处于注释状态，直接在 `girvs.shared.json` 里手写 `sample-redis`、`sample-mysql`、`eventbus-mysql` 的地址。**请替换为自己环境的地址，并不要把真实密码提交到仓库。**
2. **由 AppHost 拉起容器**：取消 `Program.cs` 中 `AddRedis(...)`、`AddMySql(...)` 等 `AsGirvsResource()` 代码的注释。AppHost 启动容器后，把实际地址就地写入 `girvs.shared.json` 的 `Resources` 节点（同名资源覆盖手写值，其余手写内容保留），需要本机 Docker 可拉取 `redis` / `mysql` / `rabbitmq` 镜像。

无论哪种方式，AppHost 都会以 `GIRVS_SHARED_CONFIG` 把该文件路径注入各服务；服务自身配置的优先级高于共享文件。

## 运行

```bash
# 本机若配置了 http 代理，务必带 no_proxy，否则 Aspire 的 localhost 通信会被塞进代理
no_proxy=localhost,127.0.0.1,::1 NO_PROXY=localhost,127.0.0.1,::1 \
  dotnet run --project samples/Sample.AppHost
```

启动后控制台会打印 Aspire Dashboard 地址。AppHost 通过 `.WithReference(...)` 给 ServiceA 注入 ServiceB 的端点、给 Sample.Gateway 注入两个服务的端点；网关的 `ServiceGovernanceConfig` 使用 `Aspire` 发现源，并在 `Services` 中声明两个服务对网关公开（端点名 `http`）。部署到 K8s 时把 `ServiceDiscoveryProvider` 改为 `Kubernetes`，并在业务 Service 上添加 `girvs.io/*` 标签与注解即可切换发现源。

## 自检端点（验证各能力）

各服务端口由 Aspire 动态分配，可在 Dashboard 查看；以下为路径与预期结果：

| 服务 | 端点 | 验证 | 预期 |
|------|------|------|------|
| ServiceA / ServiceB | `GET /health`、`GET /alive` | ServiceGovernance 健康检查 | 200 |
| ServiceA | `GET /selfcheck/cache` | 共享文件 Redis 资源 + 缓存读写 | `{"match":true}` |
| ServiceA | `GET /selfcheck/db` | 共享文件 MySQL 资源 + EFCore 往返 | `{"match":true}` |
| ServiceA | `GET /selfcheck/callb` | HttpClient 服务发现调用 ServiceB `/ping` | `{"fromServiceB":"pong"}` |
| ServiceA | `GET /selfcheck/refit-callb` | Refit（服务目录解析）调用 ServiceB `/ping` | `{"fromServiceB":"pong"}` |
| ServiceA / ServiceB | `GET /selfcheck/config` | 共享文件非资源配置分发（girvs.shared.json 的 Logging 节点） | `{"logLevel":"Information",...}` |
| ServiceB | `GET /ping` | 被 ServiceA 服务发现调用 | `pong` |
| ServiceB | `GET /selfcheck/eventbus` | CAP 发布（Redis Streams + MySQL 存储） | `{"published":true,...}` |
| ServiceB | `GET /selfcheck/eventbus/received` | 订阅者端到端收到 | `{"lastReceived":"msg-..."}` |
| Gateway | `GET /sample-servicea/selfcheck/config` | 网关经服务目录转发到 ServiceA | 同 ServiceA `/selfcheck/config` |
| Gateway | `GET /sample-serviceb/ping` | 网关经服务目录转发到 ServiceB | `pong` |
| Gateway | `GET /girvs_swagger` | 网关集中 Swagger UI；`Select a definition` 按服务目录动态生成 | 可切换 `sample-servicea API` / `sample-serviceb API` |

## 生成生产发布清单（外部资源引用）

```bash
dotnet run --project samples/Sample.AppHost -- \
  --operation publish --publisher manifest --output-path ./publish-out
```

Publish 模式下不更新共享文件，各服务的 `GIRVS_SHARED_CONFIG` 指向约定挂载路径 `/girvs-config/girvs.shared.json`，由生产 ConfigMap 提供内容（`Resources` 写托管实例的真实地址）。若不希望容器资源进入发布清单，可用 `builder.ExecutionContext.IsRunMode` 包裹编排代码。

## 设计说明

- 本地 sample 的 AppHost 不启动服务注册中心；服务发现统一由 `Girvs.ServiceGovernance` 的服务目录完成，网关与 Refit 共用同一份目录，本地使用 `Aspire` 发现源，生产 K8s 使用 `Kubernetes`。
- `AddGirvsProject` 会读取服务 `appsettings.json` 中的 `HealthCheckPath` / `LivenessCheckPath` 声明 Aspire 健康探针，并等待所有已登记资源就绪。
- ServiceA/ServiceB 引用 `Girvs.OpenApi` 暴露 `/girvs_openapi/girvs_api.json`（Development 环境）；Sample.Gateway 的 Swagger UI definitions 在每次访问 `/girvs_swagger` 时按服务目录刷新，无需手写服务数量。
- 各服务需 `Properties/launchSettings.json` 指定端口，Aspire 据此分配/代理端点；普通 MVC 控制器需在 `Startup.Configure` 中显式 `MapControllers()`。
- 详细设计与迁移指引见 [docs/aspire/apphost-guide.md](../docs/aspire/apphost-guide.md)。

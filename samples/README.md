# Girvs + .NET Aspire 参照实现（Reference Sample）

这是 Girvs 框架接入 .NET Aspire 的参照实现，展示服务编排、服务发现和基础设施接入。

## 组成

| 项目 | 说明 | 使用的 Girvs 组件 |
|------|------|------|
| `Sample.AppHost` | Aspire 编排入口（`AsGirvsResource` 登记资源 + `AddGirvsProject` 分发共享配置文件）；含 `IGirvsResourceSettingsProvider` 两类扩展示例：`SqliteResource.cs`（无容器本地文件）与 `MongoSettingsProvider.cs`（官方集成包容器） | Girvs.Aspire.Hosting |
| `Sample.ServiceA` | Web 服务：缓存 + 数据库 + 调用 ServiceB | Girvs、Girvs.Cache、Girvs.EntityFrameworkCore、Girvs.ServiceGovernance、Girvs.OpenApi、Girvs.Refit |
| `Sample.ServiceB` | Web 服务：事件总线（CAP）+ 数据库（CAP 存储） | Girvs、Girvs.EventBus、Girvs.EntityFrameworkCore、Girvs.ServiceGovernance、Girvs.OpenApi |
| `Sample.Gateway` | 本地网关：通过 Aspire 注入端点发现 ServiceA/ServiceB，并按 `/{service}/...` 约定转发；集中展示各服务 OpenAPI 文档 | Girvs.Gateway、Girvs.OpenApi |
| `Sample.Worker` | 后台工作服务（BackgroundService 周期写缓存） | Girvs、Girvs.Cache、Girvs.ServiceGovernance |

> `GirvsAspireSample.slnx` 显式列出全部五个样例项目，主 `Girvs.slnx` 显式列出前四个项目。样例通过 `ProjectReference` 直接引用框架源码，样例本身不打包。

## 运行

默认编排读取 `Sample.AppHost/girvs.shared.json` 中的基础设施配置。运行前，将 `Resources` 中样例引用的地址改为本机可访问的实例。也可在 AppHost 中启用与 `ConnectionRef` 同名的资源编排，使用容器时需本机 Docker 能拉取相应镜像。

```bash
# 本机若配置了 http 代理，务必带 no_proxy，否则 Aspire 的 localhost 通信会被塞进代理
no_proxy=localhost,127.0.0.1,::1 NO_PROXY=localhost,127.0.0.1,::1 \
  dotnet run --project samples/Sample.AppHost
```

启动后控制台会打印 Aspire Dashboard 地址。启用 `AsGirvsResource` 登记的资源后，AppHost 把实际地址更新到 `Sample.AppHost/girvs.shared.json` 的 `Resources` 节点。AppHost 通过 `GIRVS_SHARED_CONFIG` 将文件路径注入业务服务和 Worker，各服务按 `ConnectionRef` 读取资源。Sample.Gateway 默认使用 Aspire 发现源，读取 AppHost 通过 `.WithReference(...)` 注入的服务端点以生成 YARP 路由和 Swagger UI definitions。生产网关的 Kubernetes 接入示例见 `samples/gateway-k8s/`。

## 自检端点（验证各能力）

各服务端口由 Aspire 动态分配，可在 Dashboard 查看；以下为路径与预期结果：

| 服务 | 端点 | 验证 | 预期 |
|------|------|------|------|
| ServiceA/B、Worker | `GET /health`、`GET /alive` | ServiceGovernanceModule 健康检查 | 200 |
| ServiceA | `GET /selfcheck/cache` | 共享文件 Redis 资源 + 缓存读写 | `{"match":true}` |
| ServiceA | `GET /selfcheck/db` | 共享文件 MySQL 资源 + EFCore 往返 | `{"match":true}` |
| ServiceA | `GET /selfcheck/callb` | 服务发现调用 ServiceB `/ping` | `{"fromServiceB":"pong"}` |
| ServiceA/B | `GET /selfcheck/config` | 读取合并后的 `Logging:LogLevel:Default` 和 `Jwt:Secret` | 例如共享文件设置 `Logging:LogLevel:Default=Information` 且未配置 `Jwt:Secret` 时，返回 `{"logLevel":"Information","jwtSecretPresent":false}` |
| ServiceB | `GET /ping` | 被 ServiceA 服务发现调用 | `pong` |
| ServiceB | `GET /selfcheck/eventbus` | CAP 发布（RabbitMQ + MySQL 存储） | `{"published":true,...}` |
| ServiceB | `GET /selfcheck/eventbus/received` | 订阅者端到端收到 | `{"lastReceived":"msg-..."}` |
| Gateway | `GET /sample-servicea/selfcheck/config` | 网关经 Aspire 发现转发到 ServiceA | 与直接调用 ServiceA 的配置自检一致 |
| Gateway | `GET /sample-serviceb/ping` | 网关经 Aspire 发现转发到 ServiceB | `pong` |
| Gateway | `GET /girvs_swagger` | 网关集中 Swagger UI，定义从服务目录生成 | 可切换 `sample-servicea API` / `sample-serviceb API` |
| Worker | `GET /selfcheck/heartbeat` | 后台服务写缓存的心跳 | `{"lastHeartbeat":"...ISO 时间"}` |

## 生成生产发布清单（外部资源引用）

```bash
dotnet run --project samples/Sample.AppHost -- \
  --operation publish --publisher manifest --output-path ./publish-out
```

Publish 模式下不更新共享文件，各服务的 `GIRVS_SHARED_CONFIG` 指向约定挂载路径 `/girvs-config/girvs.shared.json`，由生产 ConfigMap 提供内容（`Resources` 写阿里云托管实例的真实地址），不在集群内建容器。

## 设计说明

- Worker 使用 `CreateGirvsWebApplication`、Web SDK 和 `BackgroundService`，复用 Girvs 模块机制、健康端点和共享配置分发。
- 本地 AppHost 使用 Aspire 发现，Sample.Gateway 从 AppHost 注入的配置读取服务端点。
- ServiceA/ServiceB 通过 `Girvs.OpenApi` 暴露 `/girvs_openapi/girvs_api.json`。Sample.Gateway 在 `/girvs_swagger` 展示服务目录中的 OpenAPI 文档。
- 各服务需 `Properties/launchSettings.json` 指定端口，Aspire 据此分配/代理端点。
- 当前接入方式见 `docs/aspire/apphost-guide.md`。初版设计记录见 `docs/superpowers/plans/2026-07-14-aspire-reference-implementation.md`。

> Kubernetes 服务目录与网关的部署示例见 `samples/gateway-k8s/`。

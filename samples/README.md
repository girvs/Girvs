# Girvs + .NET Aspire 参照实现（Reference Sample）

这是 Girvs 框架接入 .NET Aspire 的**活体参照实现**：一个能端到端跑通的最小系统，既是框架 Aspire 集成的验证场，也是业务系统迁移的抄写模板。

## 组成

| 项目 | 说明 | 使用的 Girvs 组件 |
|------|------|------|
| `Sample.AppHost` | Aspire 编排入口（`AsGirvsResource` 登记资源 + `AddGirvsProject` 分发共享配置文件）；含 `IGirvsResourceSettingsProvider` 两类扩展示例：`SqliteResource.cs`（无容器本地文件）与 `MongoSettingsProvider.cs`（官方集成包容器） | Girvs.Aspire.Hosting |
| `Sample.ServiceA` | Web 服务：缓存 + 数据库 + 调用 ServiceB | Cache、EntityFrameworkCore |
| `Sample.ServiceB` | Web 服务：事件总线（CAP）+ 数据库（CAP 存储） | EventBus、EntityFrameworkCore |
| `Sample.Worker` | 后台工作服务（BackgroundService 周期写缓存） | Cache |
| `Sample.Modules` | 各服务的根模块声明（`[DependsOn]`），供 AppHost 引用 | — |

> 独立解决方案 `GirvsAspireSample.slnx`，不进主 `Girvs.slnx`（避免"构建即打包"）。样例用 `ProjectReference` 直接引框架源码，改框架代码立即生效。

## 运行

前置：本机 Docker 可拉取 `redis`/`mysql`/`rabbitmq` 镜像（国内需配镜像加速/代理）。

```bash
# 本机若配置了 http 代理，务必带 no_proxy，否则 Aspire 的 localhost 通信会被塞进代理
no_proxy=localhost,127.0.0.1,::1 NO_PROXY=localhost,127.0.0.1,::1 \
  dotnet run --project samples/Sample.AppHost
```

启动后控制台会打印 Aspire Dashboard 地址。AppHost 拉起 Redis / MySQL / RabbitMQ 容器后,把实际地址写入运行时共享文件(`Sample.AppHost/obj/girvs.shared.runtime.json`)并以 `GIRVS_SHARED_CONFIG` 注入各服务;服务按自己配置中的 `ConnectionRef` 从 `Resources` 组装连接串。

## 自检端点（验证各能力）

各服务端口由 Aspire 动态分配，可在 Dashboard 查看；以下为路径与预期结果：

| 服务 | 端点 | 验证 | 预期 |
|------|------|------|------|
| 全部 | `GET /health`、`GET /alive` | AspireModule 健康检查 | 200 |
| ServiceA | `GET /selfcheck/cache` | 共享文件 Redis 资源 + 缓存读写 | `{"match":true}` |
| ServiceA | `GET /selfcheck/db` | 共享文件 MySQL 资源 + EFCore 往返 | `{"match":true}` |
| ServiceA | `GET /selfcheck/callb` | 服务发现调用 ServiceB `/ping` | `{"fromServiceB":"pong"}` |
| ServiceA/B | `GET /selfcheck/config` | 共享文件非资源配置分发（girvs.shared.json 的 Logging 节点） | `{"logLevel":"Information","jwtSecretPresent":false}` |
| ServiceB | `GET /ping` | 被 ServiceA 服务发现调用 | `pong` |
| ServiceB | `GET /selfcheck/eventbus` | CAP 发布（RabbitMQ + MySQL 存储） | `{"published":true,...}` |
| ServiceB | `GET /selfcheck/eventbus/received` | 订阅者端到端收到 | `{"lastReceived":"msg-..."}` |
| Worker | `GET /selfcheck/heartbeat` | 后台服务写缓存的心跳 | `{"lastHeartbeat":"...ISO 时间"}` |

## 生成生产发布清单（外部资源引用）

```bash
dotnet run --project samples/Sample.AppHost -- \
  --operation publish --publisher manifest --output-path ./publish-out
```

Publish 模式下不生成运行时共享文件，各服务的 `GIRVS_SHARED_CONFIG` 指向约定挂载路径 `/girvs-config/girvs.shared.json`，由生产 ConfigMap 提供内容（`Resources` 写阿里云托管实例的真实地址），不在集群内建容器。

## 设计说明

- Girvs 启动机制（`CreateGirvsWebApplicationBuilder`）是 Web 宿主，故 Worker 亦用 Web SDK + `BackgroundService`，以复用 Girvs 模块机制与共享配置文件分发。
- 各服务需 `Properties/launchSettings.json` 指定端口，Aspire 据此分配/代理端点。
- 详细设计与迁移指引见 `docs/aspire/apphost-guide.md` 与 `docs/superpowers/plans/2026-07-14-aspire-reference-implementation.md`。

> 网关（`Girvs.Aspire.Gateway`，与本 AppHost 参照实现是独立话题）的 kind 集群端到端验证见 `samples/gateway-k8s/`。

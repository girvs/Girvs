# Girvs Aspire 生产化增强设计文档（K8s 网关发现 / 共享配置 / 外部资源引用）

- 日期：2026-07-14
- 状态：已确认
- 前置文档：`docs/superpowers/specs/2026-07-13-aspire-integration-design.md`（框架基础能力，状态"已确认"，本文档不改动其内容，属于增量补充）
- 关联业务落地文档：`docs/aspire/升级方案.md`（业务系统整体迁移指南，依赖本文档描述的框架能力）
- 相关模块：新增 `Girvs.Gateway`；扩展 `Girvs.Aspire`、`Girvs.Aspire.Hosting`

## 0. 实施策略（2026-07-14 补充）：参照实现优先

本文档识别的三项能力，此前均只有单元测试覆盖，从未在一个真实运行的多服务系统里端到端验证过——`Girvs.Aspire`/`Girvs.Aspire.Hosting` 能被单测证明"资源图正确、连接串映射正确"，但从未 `dotnet run` 真正跑起来、从未 `aspire publish` 生成过 K8s 清单、从未在 Aspire Dashboard 里看到过日志/追踪。这是"一结合真实业务就完全不是这么回事"的根本原因：框架本身的自洽性从未被验证，却直接拿真实业务当第一个试验场，等于同时调试"框架对不对"和"业务迁移对不对"两个变量。

因此确定实施策略：**先在 Girvs 仓库内建一个最小但完整的参照实现（Reference/Sample），把它作为框架的活体端到端验证 + 业务迁移的抄写模板 + 三项能力的真实验证场，再谈真实业务迁移。** 参照实现构成：1 个自建 YARP 网关 + 2 个业务微服务（各用 Cache/EventBus/EFCore 并互调一次）+ 1 个 Worker + 1 个 AppHost。推进顺序为"每步能跑通再进下一步"：骨架跑通 → 接入真实组件 → 服务间调用与发现 → 共享配置注入 → 生产外部资源引用与 `aspire publish` → 网关 K8s 动态发现。

由此，实施计划的组织从"按三个缺口切"调整为"按参照实现的分步搭建切"：
- 计划 1（`2026-07-14-aspire-reference-implementation.md`）：参照实现搭建 + 分步验证，把本文档 3.2（共享配置）、3.3（外部资源引用）两项能力融入其中端到端验证；
- 计划 2：`Girvs.Gateway`（本文档 3.1），在计划 1 的参照系统之上叠加；
- 计划 3：真实业务 NewOnlineRegistration 迁移（`docs/aspire/升级方案.md`），照参照实现改。

## 1. 背景与目标

`docs/superpowers/specs/2026-07-13-aspire-integration-design.md` 确认并实现了 Girvs 服务接入 Aspire 的基础能力（`AddGirvsProject`、连接串映射、OTel、健康检查），但设计目标聚焦"单服务接入 + 本地开发资源编排"，未覆盖真正把一个多服务系统（如 NewOnlineRegistration，20+ 服务）**整体迁移到生产 K8s 环境**所需的三项能力：

1. 自建 YARP 网关（考生端 / 管理端两套，分流以增强安全隔离）目前靠轮询 Consul Catalog 做运行时动态路由，迁移后要改为读取 K8s 原生服务发现数据，但 Aspire 现有能力只解决编译期资源接线，没有提供运行时可查询的服务目录给网关用；
2. 跨服务共享的通用配置（日志等级、JwtSecret 等）目前无统一注入机制，现有资源注册表只覆盖 Cache/EventBus/EFCore 连接串；
3. 生产环境的 MySQL/Redis/消息队列均为阿里云托管服务（RDS/云 Redis/消息队列产品），而现有 `AddGirvsProject` 默认语义是"Aspire 创建并管理该资源"，发布到 K8s 时会尝试在集群内新建对应资源，与生产实际形态不符。

目标：在不改动已确认的框架基础能力前提下，新增上述三项能力，使 Girvs 服务能够以 `aspire publish`（Kubernetes publisher）为生产发布路径，真正在 K8s 上跑起来。

## 2. 已确认的关键决策

| 决策点 | 结论 |
|--------|------|
| 网关服务发现来源 | 新增 `Girvs.Gateway` 包，提供 `KubernetesProxyConfigProvider`，通过 K8s API watch Service/EndpointSlice 动态生成 YARP 路由配置，替代 Consul Catalog 轮询 |
| 路由元数据来源 | K8s Service 的 Annotation（`girvs.io/route-prefix` 等），由 `AddGirvsProject` 在发布阶段按 `GirvsProjectOptions` 声明自动写入 |
| 共享配置注入 | `Girvs.Aspire.Hosting` 新增 `AddGirvsSharedConfiguration`；非敏感值走 K8s ConfigMap，敏感值走 K8s Secret；服务端由 `Girvs.Aspire` 新增读取逻辑覆盖 AppSettings |
| 生产基础设施资源形态 | 本地 Run 模式沿用现有容器语义不变；Publish 模式下 Cache/EventBus/EFCore 三个资源贡献器改为 `AddConnectionString(...)` 引用外部参数（阿里云托管实例），不在集群内新建资源 |
| 与已确认文档的关系 | 增量补充，不修改 2026-07-13 文档任何内容；`Girvs.Aspire`/`Girvs.Aspire.Hosting` 版本号随之提升，`Girvs.Gateway` 为全新包 |

## 3. 架构设计

### 3.1 `Girvs.Gateway`（新包，仅 net10.0，服务端）

**（2026-07-15 依现有 YarpGateway 代码现实重写）**

**现状核对**（`NewOnlineRegistration/netcoresrc` 的 `ZhuoFan.Wb.YarpGateway` 考生端 + `ZhuoFan.Wb.Management.YarpGateway` 管理端，两者 Provider/Consul/K8s 发现代码逐字节相同，仅 `RequestFilterMiddleware` 不同）：

- 网关**已有** K8s 服务发现：`Services/KubernetesClientService.cs` 用 `InClusterConfig()` + `ListServiceForAllNamespacesAsync()` 拿 Service，由 `ServiceDiscoveryConfig.ServiceDiscoveryType` 在 Consul / K8s 间切换。即"用 K8s 替代 Consul"已做一半，但与 Consul 一样是 **30s Timer 轮询（一次性 list）**，非 watch。
- **路由是约定式**：`CustomProxyConfigProvider.UpdateConfig` 对每个服务名生成 `RouteConfig{ Match.Path="/{服务名}/{**catch-all}", Transforms=[PathRemovePrefix={服务名}] }` + `ClusterConfig{ Destinations }`，**不读任何 Tag/Meta/Annotation**。
- **双网关分流靠中间件、不靠 Label**：两网关拉同一份全量服务列表，`RequestFilterMiddleware` 按 URL 前缀 allow/deny（管理端前缀 `/api/wb_management`）区分。
- **变更令牌有潜在 bug**：YARP 监听的 `CustomProxyConfig.ChangeToken` 包装的 `_cts` 从不 Cancel；Provider 里 Cancel 的是另一个无关 `_cts`。当前靠 30s 新建 `_config` + YARP 自身周期重读兜底，热更新信号实际未接通。

**修正后的职责（比原设计窄）**：把分散在两网关里的 K8s/Consul 发现 + YARP 路由生成逻辑提取为可复用的 `Girvs.Gateway` 包；提供一个**可插拔的服务发现抽象**，其中 K8s 来源从"30s 轮询"升级为 **watch 事件驱动**；Consul 来源保留原样。**不引入 Label 分流、不引入 Annotation 路由**（现实里分流在中间件、路由是约定式，无需要）。

**多部署形态（回应"兼顾 CentOS/docker 部署"）**：服务发现来源由 `ServiceDiscoveryType` 选择，同一份网关代码适配三种部署——

| 部署形态 | 服务发现来源 |
|---|---|
| CentOS 编译部署 / docker(-compose) 部署（无 K8s） | Consul（沿用现有轮询，不变） |
| 阿里云 K8s 部署 | K8s **watch**（本包新增，替代 30s 轮询、事件驱动更实时） |

**核心组件**：

- `IGatewayServiceDiscoverySource`（抽象）：产出 `IReadOnlyList<GatewayServiceEndpoint>{ ServiceName, Destinations }`，并暴露"服务集变化"事件/变更令牌；
- `ConsulGatewayServiceSource`：封装现有 `ConsulClientService` 逻辑（`Agent.Services` 拉取 + 30s 轮询），供非 K8s 部署；
- `KubernetesGatewayServiceSource`：用官方 `KubernetesClient`（`k8s` 包）对 Service（视需要含 EndpointSlice）建立 **watch**，事件驱动增量更新，参考 K8s Informer 标准做法（断线重连 + 全量 relist）；
- `GirvsGatewayProxyConfigProvider : IProxyConfigProvider`：消费选定的 source，按现有约定（服务名→路径前缀 + PathRemovePrefix）生成 `RouteConfig`/`ClusterConfig`，并**正确接通 `CancellationChangeToken`** 通知 YARP 热更新（修掉现有 bug）；
- 注册扩展：`services.AddGirvsGateway()`（内部按 `ServiceDiscoveryType` 选 source + 注册 Provider + `AddReverseProxy().AddTransforms(...)` 保留现有转换：`AddOriginalHost(false)`/`CopyRequestHeaders`/`AddXForwarded`），替换两网关中重复的 `CustomProxyConfigProvider` + `YarpGatewayModule` 注册代码。

**保留的现有能力**（新 Provider 须等价）：路径去前缀 `PathRemovePrefix`、X-Forwarded 头转发、CORS/维护模式/RequestFilter 中间件（保留在网关侧，不进本包）。负载均衡/健康检查/超时重试现状未配置（每服务单 Destination），本包暂不新增。

**RBAC 要求**：K8s 部署时网关 ServiceAccount 需对 `services`（含 `endpointslices`，若用）具备 `list`/`watch` 权限（命名空间级）。对应 Role/RoleBinding 由部署清单提供（业务迁移计划 C 落地；本包文档给出清单示例）。

### 3.2 共享配置注入（`Girvs.Aspire.Hosting` + `Girvs.Aspire` 扩展）

**AppHost 侧**（`Girvs.Aspire.Hosting` 新增 API）：

```csharp
builder.AddGirvsSharedConfiguration(shared =>
{
    shared.AddSetting("Logging:LogLevel:Default", "Information");   // 非敏感 → ConfigMap
    shared.AddSecret("Jwt:Secret", jwtSecretParameter);              // 敏感 → Secret
});
```

内部按 `IsSecret` 拆成两组，Publish 阶段分别生成 `girvs-shared-config`（ConfigMap）与 `girvs-shared-secret`（Secret）两个 K8s 资源；`AddGirvsProject` 声明的每个服务自动 `WithReference` 这两个资源（无需业务方逐服务声明）。本地 Run 模式下以环境变量形式注入，行为与生产一致，只是承载方式不同（K8s 原生资源 vs 进程环境变量），对服务端读取逻辑透明。

**服务端**（`Girvs.Aspire` 新增）：新增 `SharedConfigurationModule`，在 `AspireModule` 之后加载，读取约定注入源并合并进 `IConfiguration`（作为一个高优先级 Configuration Provider，允许服务自身 appsettings.json 中的同名 key 被覆盖，覆盖方向：共享配置 > 服务本地配置，与现有连接串覆盖逻辑的优先级一致）。未接入 Aspire 环境时（无对应注入源）该模块空跑，不影响现状。

### 3.3 生产资源外部引用模式

`Girvs.Aspire.Hosting` 现有的 Cache/EventBus/EFCore 三个资源贡献器新增发布模式分支：

```csharp
if (builder.ExecutionContext.IsPublishMode)
{
    var redisConn = builder.AddParameter("girvs-cache-connection-string", secret: true);
    builder.AddConnectionString("girvs-cache", redisConn);
}
else
{
    builder.AddRedis("girvs-cache"); // 本地容器语义不变
}
```

`AddParameter` 对应的实际值（阿里云 RDS/Redis/消息队列连接串）通过 Aspire 标准的参数配置源提供（`appsettings.json` 的 `Parameters` 节或部署流水线注入的环境变量），不落入源码仓库。三个贡献器（Cache/EventBus/EFCore）均按此模式统一处理，EFCore 因涉及"共享数据库服务器 + 每服务独立 database"的既有设计，Publish 模式下改为直接引用每服务在阿里云 RDS 上已建好的独立库连接串（约定参数名 `girvs-db-<service>-<name>-connection-string`），不再有"服务器 + 独立 database 资源"两层概念（阿里云 RDS 场景下数据库需提前手工建库，Aspire 不负责建库）。

## 4. 工程约束

- `Girvs.Gateway` 仅 `net10.0`，依赖 `Yarp.ReverseProxy`、`KubernetesClient`（官方 `k8s` 包）；不引用 `Girvs.Aspire.Hosting`（服务端包与 AppHost 端包保持既有的单向依赖约束不变）。
- `Girvs.Aspire.Hosting` 新增依赖 Aspire K8s publisher 的清单自定义扩展点（`Aspire.Hosting.Kubernetes` 的 manifest customization API，具体 API 面在实现阶段以当时 Aspire 13.x 稳定版为准，若接口有出入以实现时的实际签名为准）。
- `nugetpublish.ps1` 增加 `Girvs.Gateway` 推送行。

## 5. 业务方使用方式

详见 `docs/aspire/升级方案.md`。本文档只描述框架侧新增能力，不重复业务迁移步骤。

## 6. 测试策略

- `Girvs.Gateway`：`KubernetesProxyConfigProvider` 对 Service/EndpointSlice 变更事件到 YARP RouteConfig/ClusterConfig 的映射逻辑，用 K8s Fake Client（`KubernetesClient` 测试替身或内存 watch 事件回放）覆盖增删改场景；Annotation 缺失/格式错误时的跳过与警告行为。
- 共享配置：ConfigMap/Secret 拆分逻辑、服务端 `SharedConfigurationModule` 的覆盖优先级（共享配置 > 服务本地配置）。
- 外部资源引用模式：`IsPublishMode` 分支下生成的资源图（用 Aspire 的 manifest 快照测试，验证 Publish 模式下不产生容器资源、只产生 `AddConnectionString` 引用）。
- 手工验证：至少一个网关 + 两个业务服务的最小拓扑，`aspire publish` 生成清单后部署到测试 K8s 命名空间，验证路由随服务上下线动态更新、共享配置正确落到 ConfigMap/Secret、外部连接串正确注入。

## 7. 明确不做的事（YAGNI）

- 不实现 K8s 之外的其他生产发布目标（Docker Swarm、裸机等）；
- 不在 `Girvs.Gateway` 里重新实现 K8s Ingress Controller 的完整能力（如 TLS 证书自动签发），只做路由发现这一层，TLS/证书仍由现有基础设施（阿里云 SLB 等）承担；
- 共享配置机制不支持运行时热更新（与业务方已确认的"改配置重新发布可接受"一致）；
- 不负责阿里云 RDS/Redis/消息队列实例本身的创建与运维，只负责连接串的声明与注入；
- 不改动已确认的 2026-07-13 文档所覆盖的能力（本地资源自动编排、连接串映射逻辑本身）。

## 8. 风险

| 风险 | 缓解 |
|------|------|
| Aspire K8s publisher 的清单自定义扩展点（用于追加 RBAC、Annotation）API 面随版本变化 | 实现阶段锁定具体 Aspire 版本并记录所用 API；封装成 Girvs 自己的扩展方法，未来 Aspire 升级只需改一处 |
| `KubernetesProxyConfigProvider` watch 连接断线重连期间路由数据可能短暂过期 | 参考 K8s Informer 标准做法实现重连 + 全量 relist，测试覆盖断线恢复场景 |
| 共享配置覆盖优先级与服务本地 appsettings.json 环境专属文件（如 `appsettings.Production.json`）的叠加顺序可能引发困惑 | 文档明确写出最终优先级链（共享配置 > 环境专属文件 > 基础 appsettings.json），并在 `SharedConfigurationModule` 启动日志中打印生效来源，便于排查 |
| 阿里云 RDS 独立库需要提前手工建库，若遗漏会导致服务启动时连接失败 | 升级方案文档给出建库检查清单；服务端连接失败时的错误信息需明确提示"检查阿里云 RDS 是否已建库" |
| 网关 RBAC 权限配置遗漏导致 watch 权限不足 | Provider 启动时对权限不足有明确的异常信息（区分"连接失败"与"权限不足"），文档给出所需 RBAC 清单示例 |

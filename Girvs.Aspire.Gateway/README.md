# Girvs.Aspire.Gateway

Girvs 自建 YARP 网关的**服务发现与路由生成**：可插拔 Consul（轮询）/ K8s（watch）两种发现源，按约定生成 YARP 路由与集群配置，替代各业务网关里重复的 `CustomProxyConfigProvider`/`ConsulClientService`/`KubernetesClientService`。

## 用途

一个网关进程要把请求转发到后端一批动态变化的服务上（服务会上下线、扩缩容），需要：

1. 持续发现当前有哪些后端服务（及其地址）；
2. 服务集变化时，让 YARP 的路由/集群配置跟着热更新（不重启网关、不轮询等待）。

本包把这两件事抽成 `IGatewayServiceDiscoverySource`（发现）+ `IProxyConfigProvider`（YARP 配置生成），并提供两个开箱即用的发现源实现，覆盖两类常见部署形态：

| 部署形态 | `GatewayDiscoveryType` | 发现源 | 机制 |
|---|---|---|---|
| CentOS / Docker（非 K8s） | `Consul` | `ConsulGatewayServiceSource` | 轮询 Consul Agent 服务目录（30 秒一次） |
| Kubernetes | `Kubernetes` | `KubernetesGatewayServiceSource` | relist 建初态 + watch K8s Service 增量事件，事件驱动、无轮询等待 |

## 用法

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGirvsGateway(new GatewayDiscoveryConfig
{
    DiscoveryType = GatewayDiscoveryType.Kubernetes, // 或 GatewayDiscoveryType.Consul
    ConsulAddress = "http://192.168.51.166:8500",    // 仅 Consul 模式需要
});

var app = builder.Build();

// 关键：AddGirvsGateway 只注册服务，不会自动开始发现，必须在启动时显式调用 StartAsync
// （K8s 模式下这里会启动后台 relist+watch 循环；Consul 模式下启动轮询定时器）
await app.Services.GetRequiredService<IGatewayServiceDiscoverySource>().StartAsync(CancellationToken.None);

app.MapReverseProxy();
app.Run();
```

`AddGirvsGateway` 做了什么：

- 按 `DiscoveryType` 注册对应的 `IGatewayServiceDiscoverySource`（单例）；
- 注册 `GirvsGatewayProxyConfigProvider`（`IProxyConfigProvider`），依据发现源的服务快照生成约定式路由；
- `AddReverseProxy().AddTransforms(...)`：追加 `X-Forwarded-*`、透传原始请求头。

## 约定路由规则

发现到一个后端服务（服务名 `ServiceName`，如 K8s Service 名 / Consul 注册的服务名），自动生成：

- 路由：`RouteId=ServiceName`，`Match.Path = "/{ServiceName}/{**catch-all}"`，`Transforms: PathRemovePrefix=ServiceName`
- 集群：`ClusterId=ServiceName`，`Destinations` 为该服务的实际地址

即约定 **请求路径以服务名开头，转发时去掉该前缀**。例如集群中有一个名为 `echo-a` 的服务：

- 客户端请求 `GET /echo-a/foo`
- 网关匹配到路由 `echo-a`，去掉 `echo-a` 前缀后转发 `GET /foo` 到该服务的地址

K8s 模式下地址固定为集群内 DNS：`http://<ServiceName>.<Namespace>.svc.cluster.local:<Port>`（K8s Service 声明几个端口就生成几个 Destination）。Consul 模式下地址为 `http://<AgentService.Address>:<AgentService.Port>`。

不支持基于 Label/Annotation 的分流或自定义路由规则——这类需求留给网关侧自行叠加中间件，本包只负责"服务名 → 约定路由"这一层。

## 变更令牌与热更新

`GirvsGatewayProxyConfigProvider` 订阅发现源的 `ServicesChanged` 事件：每次触发，先构建携带新 `ChangeToken` 的新配置、再取消旧配置持有的令牌，YARP 收到令牌取消通知后会重新调用 `GetConfig()` 拿到新路由——全程不重启进程、不轮询。K8s 模式下这意味着 Service 增删/端口变化能在**秒级**（watch 事件到达后）反映到网关路由，而不是等下一次轮询周期。

## K8s 部署要求

### 1. 启动时调用 `StartAsync`

`KubernetesGatewayServiceSource` 的 relist+watch 循环由显式 `StartAsync` 触发（不在构造函数/DI 注册时自动启动），务必在 `app.Build()` 之后、`app.Run()` 之前调用，见上方用法示例。

### 2. RBAC：ClusterRole，不是 Role

`KubernetesGatewayServiceSource` 用 `ListServiceForAllNamespacesAsync`/watch 做**集群范围**的 Service 列表与监听（而非单一命名空间），因此网关 Pod 的 ServiceAccount 必须绑定 `ClusterRole`（命名空间级 `Role`会被拒绝，报 `... is forbidden: ... at the cluster scope`）：

```yaml
apiVersion: v1
kind: ServiceAccount
metadata:
  name: girvs-gateway
  namespace: default
---
apiVersion: rbac.authorization.k8s.io/v1
kind: ClusterRole
metadata:
  name: girvs-gateway-services-reader
rules:
  - apiGroups: [""]
    resources: ["services"]
    verbs: ["list", "watch"]
---
apiVersion: rbac.authorization.k8s.io/v1
kind: ClusterRoleBinding
metadata:
  name: girvs-gateway-services-reader-binding
subjects:
  - kind: ServiceAccount
    name: girvs-gateway
    namespace: default
roleRef:
  kind: ClusterRole
  name: girvs-gateway-services-reader
  apiGroup: rbac.authorization.k8s.io
```

Deployment 的 Pod spec 需 `serviceAccountName: girvs-gateway`。集群内运行时 `KubernetesGatewayServiceSource` 用 `KubernetesClientConfiguration.InClusterConfig()` 自动读取该 ServiceAccount 的挂载凭据，无需额外配置。

### 3. 断线重连

watch 连接异常（网络抖动、API Server 重启等）会被捕获，2 秒后自动重新 relist+watch，不需要外部干预；重连期间的错误只打到 stderr，不影响已生效的路由配置。

## 端到端验证

`samples/gateway-k8s/` 是一个可在 kind 集群里跑通的最小验证系统（网关 + `echo-a`/`echo-b` 两个 dummy 后端），包含完整 K8s 清单（RBAC/Deployment/Service）与 Dockerfile，并验证过"删除 Service → 路由秒级消失（404）→ 重新创建 → 路由恢复（200）"的动态更新场景。可直接照抄用于验证自己的部署。

## 与 Consul 模式的区别

Consul 模式面向非 K8s 部署（如 CentOS 直接部署、Docker Compose），不需要 RBAC，但发现是**轮询**（默认 30 秒一次），服务上下线到网关感知之间有最多 30 秒延迟；K8s 模式是事件驱动，感知延迟通常是秒级。两种模式共用同一套 `AddGirvsGateway` API 与约定路由规则，仅 `GatewayDiscoveryConfig.DiscoveryType` 不同。

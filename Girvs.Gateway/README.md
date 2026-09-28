# Girvs.Gateway

基于 [YARP](https://github.com/microsoft/reverse-proxy) 的 Girvs 网关：依据 `Girvs.ServiceGovernance` 提供的统一服务目录 `IServiceDirectory` 生成约定式路由，服务增删或端点变化时热更新 YARP 配置；可选启用基于 Redis Lua 的严格流程防护。

目标框架：`net10.0`。依赖 `Girvs`、`Girvs.Cache`、`Girvs.ServiceGovernance`、`Yarp.ReverseProxy`。

## 工作方式

```
Aspire 配置 / Consul / Kubernetes
            │  （ServiceGovernance 按 DiscoveryRefreshInterval 定时拉取）
            ▼
     IServiceDirectory（服务快照，变化时触发 ServicesChanged）
            │
            ▼
GirvsGatewayProxyConfigProvider（IProxyConfigProvider，生成路由与集群）
            │  ChangeToken 通知
            ▼
          YARP
```

网关本身不直接对接任何注册中心：服务发现统一由 `Girvs.ServiceGovernance` 完成，网关与 `Girvs.Refit` 共用同一份服务目录。发现源通过 `ServiceGovernanceConfig.ServiceDiscoveryProvider` 选择：

| 部署形态 | `ServiceDiscoveryProvider` | 服务来源 | 网关公开方式 |
|---|---|---|---|
| 本地 Aspire AppHost | `Aspire`（默认） | AppHost 注入的 `services:{name}:{endpoint}:{i}` 配置 | `ServiceGovernanceConfig.Services[name]` 中的 `GatewayEnabled` / `GatewayEndpointName` |
| 传统注册中心 | `Consul` | 带 tag `girvs.business=true` 的健康实例 | tag `girvs.gateway-enabled=true`、`girvs.gateway-endpoint=<端点名>` |
| Kubernetes | `Kubernetes` | 匹配 `KubernetesLabelSelector`（默认 `girvs.io/business=true`）的 Service | 注解 `girvs.io/gateway-enabled: "true"`、`girvs.io/gateway-endpoint: "<端口名>"` |

服务目录按 `DiscoveryRefreshInterval`（默认 30 秒）轮询刷新，快照发生变化时才触发路由更新。单次刷新失败会保留上一次成功的快照，不影响已生效的路由。

## 用法

### 在 Girvs 服务中使用

引用 `Girvs.Gateway` 后，`ServiceGovernanceModule` 会随模块机制自动注册服务目录，只需在 Startup 中注册网关并映射反向代理：

```csharp
public class Startup(IConfiguration configuration, IWebHostEnvironment env) : IGirvsStartup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // 传入 configuration 时会同时读取 FlowProtection 配置节
        services.AddGirvsGateway(configuration);
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (app is IEndpointRouteBuilder endpoints)
            endpoints.MapReverseProxy();
    }
}
```

`appsettings.json`（Aspire 模式需要显式声明哪些服务对网关公开）：

```json
{
  "ModuleConfigurations": {
    "ServiceGovernanceConfig": {
      "ServiceDiscoveryProvider": "Aspire",
      "Services": {
        "sample-servicea": { "GatewayEnabled": true, "GatewayEndpointName": "http" },
        "sample-serviceb": { "GatewayEnabled": true, "GatewayEndpointName": "http" }
      }
    }
  }
}
```

切换到 Kubernetes 或 Consul 时只需修改 `ServiceDiscoveryProvider`，并按上表在 Service 注解或 Consul tag 中声明网关公开信息。

### 独立网关（不使用 Girvs 启动器）

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGirvsServiceDirectory(new ServiceGovernanceConfig
{
    ServiceDiscoveryProvider = ServiceDiscoveryProvider.Kubernetes,
    DiscoveryRefreshInterval = 10,
});
builder.Services.AddGirvsGateway(builder.Configuration);

var app = builder.Build();
app.MapReverseProxy();
app.Run();
```

`AddGirvsServiceDirectory` 会注册服务目录、对应的发现源、后台刷新服务 `ServiceDirectoryRefreshService` 以及 `service-directory` 健康检查。刷新由 Hosted Service 自动启动，**无需手动调用任何启动方法**。

### `AddGirvsGateway` 做了什么

- 注册 `GirvsGatewayProxyConfigProvider` 作为 `IProxyConfigProvider`；
- `AddReverseProxy().AddTransforms(...)`：透传原始请求头，追加 `X-Forwarded-*`、`X-Forwarded-For`；
- 传入 `configuration` 时调用 `AddFlowProtection(configuration)`（`FlowProtection:Enabled=false` 时不注册任何流程防护服务）。

前提：容器中已存在 `IServiceDirectory`（由 `ServiceGovernanceModule` 或 `AddGirvsServiceDirectory` 提供）。

## 约定路由规则

只为 `GatewayEnabled = true` 且 `GatewayEndpointName` 非空的服务生成路由：

- 路由：`RouteId = ServiceName`，`Match.Path = "/{ServiceName}/{**catch-all}"`，Transform `PathRemovePrefix = ServiceName`；
- 集群：`ClusterId = ServiceName`，Destinations 为该服务所有端点名等于 `GatewayEndpointName` 的实例（key 为 `{服务名}-{端点名}-{序号}`）；
- 没有匹配端点的服务不生成路由。

即 **请求路径以服务名开头，转发时去掉该前缀**。例如：

```
GET /sample-servicea/selfcheck/cache  →  sample-servicea 的 GET /selfcheck/cache
```

各发现源的地址与端点名：

| 发现源 | 地址 | 端点名 |
|---|---|---|
| Aspire | `services:{name}:{endpoint}:{i}` 中的 http / https 地址 | 配置中的 `{endpoint}` 键，如 `http`、`https` |
| Consul | 实例的 `Address:Port` | tag `girvs.endpoint=<名称>`，缺省为 `http` |
| Kubernetes | `http://{name}.{namespace}.svc.cluster.local:{port}` | Service 端口名；未命名端口为 `port-{port}` |

服务名统一由 `ServiceNameResolver` 生成（小写连字符形式，如程序集 `Sample.ServiceA` → `sample-servicea`），网关路由前缀、Refit 服务名、Consul 注册名应保持一致。

不支持基于 Header、权重等的自定义分流；这类需求请在网关侧叠加中间件或自定义 YARP 配置。

## 变更令牌与热更新

`GirvsGatewayProxyConfigProvider` 订阅 `IServiceDirectory.ServicesChanged`：每次触发，先构建携带新 `ChangeToken` 的配置，再取消旧配置的令牌，YARP 收到通知后重新调用 `GetConfig()`。整个过程无需重启网关，路由变化的延迟上限约为一个 `DiscoveryRefreshInterval`。

## Kubernetes 部署要求

### 1. 业务 Service 的标签与注解

```yaml
apiVersion: v1
kind: Service
metadata:
  name: order-api
  labels:
    girvs.io/business: "true"            # 匹配 KubernetesLabelSelector
  annotations:
    girvs.io/gateway-enabled: "true"     # 对网关公开
    girvs.io/gateway-endpoint: "http"    # 使用名为 http 的端口
spec:
  selector:
    app: order-api
  ports:
    - name: http
      port: 80
      targetPort: 8080
```

没有业务标签的 Service（包括网关自身、`kubernetes`、`kube-dns` 等）不会进入服务目录。

### 2. RBAC

发现源使用 `KubernetesClientConfiguration.InClusterConfig()` 读取 Pod 的 ServiceAccount 凭据，只需要 Service 的 `list` 权限：

- `KubernetesNamespace` 为空（默认）：调用 `ListServiceForAllNamespacesAsync`，需要 **ClusterRole + ClusterRoleBinding**；
- 指定 `KubernetesNamespace`：调用 `ListNamespacedServiceAsync`，命名空间级 `Role + RoleBinding` 即可（推荐，权限最小化）。

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
    verbs: ["list"]
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

网关 Deployment 的 Pod spec 需设置 `serviceAccountName: girvs-gateway`。权限不足时刷新会失败并记录警告日志，服务目录保持为空或上一次成功的快照。

### 3. 健康检查

在 Girvs 服务中使用时，`ServiceGovernanceModule` 暴露 `/health` 与 `/alive`；服务目录从未成功加载或超过 `MaxStaleDuration`（默认 90 秒）未成功刷新时，`/health` 返回 Unhealthy，可作为 Readiness 探针。

## 流程防护

网关可选启用严格线性流程防护，阻止已登录用户跳过前置接口直接调用后续接口。流程定义独立放在顶层 `FlowProtection` 配置节，不写入 YARP Route Metadata；状态只保存在 Redis，所有 Reserve / Commit / Release / Delete 都通过 Lua 原子执行。

启用前置条件：

- 使用 `Girvs.Cache`，且 `CacheConfig.DistributedCacheConfig.Enabled = true`、`ConnectionRef` 指向 `Resources` 中 `Type=redis` 的资源；
- `redis-synchronized-memory`、内存缓存均不接受；
- 调用 `AddGirvsGateway` 前容器中已注册 `IRedisConnectionWrapper`（由 `Girvs.Cache` 提供）；
- 下游受保护接口不能绕过网关暴露；
- 本功能不替代认证、授权、资源权限校验和业务幂等。

示例配置：

```json
{
  "FlowProtection": {
    "Enabled": true,
    "DefaultTtlSeconds": 300,
    "LockTtlSeconds": 30,
    "CompletedTtlSeconds": 60,
    "TicketHeaderName": "X-Flow-Ticket",
    "BusinessIdHeaderName": "X-Business-Id",
    "Flows": [
      {
        "FlowId": "user-role-abc",
        "TtlSeconds": 300,
        "Steps": [
          { "Method": "POST", "Path": "/api/user1" },
          { "Method": "POST", "Path": "/api/role1" },
          { "Method": "POST", "Path": "/api/abc1" }
        ]
      }
    ]
  },
  "ModuleConfigurations": {
    "CacheConfig": {
      "DistributedCacheConfig": {
        "Enabled": true,
        "ConnectionRef": "gateway-redis"
      }
    }
  },
  "Resources": {
    "gateway-redis": {
      "Type": "redis",
      "Settings": {
        "Endpoints": "redis:6379"
      }
    }
  }
}
```

配置校验（启动时失败即抛出）：

- 每个流程至少 2 步，`FlowId` 与步骤不能重复，同一 `Method Path` 不能出现在多个流程中；
- `Path` 必须以 `/` 开头；
- 所有 TTL 在 1 ~ 1800 秒之间，`LockTtlSeconds` 不超过 `DefaultTtlSeconds`。

接入代码：

```csharp
builder.Services.AddGirvsServiceDirectory(
    new ServiceGovernanceConfig { ServiceDiscoveryProvider = ServiceDiscoveryProvider.Kubernetes });
builder.Services.AddGirvsGateway(builder.Configuration);

builder.Services.AddCors(options => options.AddPolicy("gateway", policy => policy
    .AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod()
    .WithExposedHeaders(FlowProtectionHeaders.ResponseHeaders)));

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.UseCors("gateway");

app.MapReverseProxy(proxyPipeline =>
{
    proxyPipeline.UseMiddleware<FlowTicketMiddleware>();
});
```

步骤 Path 使用下游服务路径。例如约定路由 `/{ServiceName}/{**catch-all}` 加 `PathRemovePrefix=order` 时，客户端请求 `/order/api/pay1`，流程配置写 `/api/pay1`。仅支持精确 Path，不支持模板、通配符、Query 或 JSON Body 提取。

响应 Header：

- 未完成：`X-Flow-Ticket`、`X-Flow-Next-Index`、`Cache-Control: no-store`
- 完成：`X-Flow-Completed: true`、`Cache-Control: no-store`
- 下游可在第一步成功响应中返回 `X-Flow-Business-Id`，网关会消费并写入 Redis，不透传给客户端。

错误码均返回 RFC 7807 ProblemDetails，HTTP 403：

| code | 含义 |
|---|---|
| `FLOW_NOT_FOUND` | Ticket 不存在、缺失或已过期 |
| `FLOW_EXPIRED` | 流程状态已过期 |
| `FLOW_BUSINESS_MISMATCH` | `X-Business-Id` 与 Redis 绑定值不一致 |
| `FLOW_STEP_NOT_ALLOWED` | 当前步骤不是流程允许的下一步 |
| `FLOW_IN_PROGRESS` | 同一步已有未过期处理锁 |
| `FLOW_COMPLETED` | 流程已完成，重复调用被拒绝 |

受保护路由缺少唯一的 `PathRemovePrefix` 时返回 500"流程保护路由配置错误"。

前端只保存服务端返回的 Ticket，建议在内存中按 `flowId + businessId` 作为 key 保存；后续受保护请求自动附加 `X-Flow-Ticket` 和 `X-Business-Id`。收到 `FLOW_*` 403 后清除本地 Ticket 并提示用户重新开始。Ticket 是持有者凭据，当前不绑定 `userId/tenantId`，因此不要长期放入 LocalStorage，也不要记录到普通业务日志。

## 端到端验证

- 本地 Aspire：`samples/Sample.Gateway` 由 `samples/Sample.AppHost` 编排，经 Aspire 发现转发 ServiceA / ServiceB，并在 `/girvs_swagger` 聚合各服务 OpenAPI 文档，见 [samples/README.md](../samples/README.md)。
- Kubernetes：`samples/gateway-k8s/` 是可在 kind 集群中跑通的最小验证系统（网关 + `echo-a` / `echo-b` 两个 dummy 后端），包含 RBAC、Deployment、Service 清单与 Dockerfile，验证"删除 Service → 路由消失（404）→ 重新创建 → 路由恢复（200）"，见 [samples/gateway-k8s/README.md](../samples/gateway-k8s/README.md)。

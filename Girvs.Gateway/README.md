# Girvs.Gateway

`Girvs.Gateway` 面向 `net10.0` 和 `net11.0`，基于 `Yarp.ReverseProxy` 2.3.0。它从 `Girvs.ServiceGovernance` 的统一服务目录生成动态路由和集群，并通过 `Girvs.Cache` 支持可选的 Redis 流程防护。

## 接入网关

网关依赖 DI 中的 `IServiceDirectory`。Girvs 宿主由 `ServiceGovernanceModule` 注册目录。独立 ASP.NET Core 宿主先从配置绑定 `ServiceGovernanceConfig`，再显式注册目录。

```csharp
using Girvs.Gateway;
using Girvs.ServiceGovernance.Configuration;
using Girvs.ServiceGovernance.Discovery;

var builder = WebApplication.CreateBuilder(args);

var serviceGovernanceConfig = builder.Configuration
    .GetSection("ModuleConfigurations:ServiceGovernanceConfig")
    .Get<ServiceGovernanceConfig>() ?? new ServiceGovernanceConfig();
builder.Services.AddGirvsServiceDirectory(serviceGovernanceConfig);
builder.Services.AddGirvsGateway(builder.Configuration);

var app = builder.Build();
app.MapReverseProxy();
app.MapHealthChecks("/health");
app.Run();
```

注册入口为 `services.AddGirvsGateway(IConfiguration configuration = default)`。

- 按 `ModuleConfigurations:GirvsGatewayConfig` → 顶级 `GirvsGatewayConfig` → 默认实例的顺序绑定并注册 `GirvsGatewayConfig`。
- 将 `GirvsGatewayProxyConfigProvider` 注册为单例 `IProxyConfigProvider`，从服务目录快照生成 YARP 配置。
- 注册 YARP，并全局配置 `AddOriginalHost(false)`（使用目标 Host）、`CopyRequestHeaders = true`（复制请求头）和 `AddXForwarded(ForwardedTransformActions.Append)`（追加 `X-Forwarded-*`）。
- 传入非空 `configuration` 时调用 `AddFlowProtection(configuration)`。

## 服务发现与网关准入

在 `ModuleConfigurations:ServiceGovernanceConfig` 中用 `ServiceDiscoveryProvider` 选择 `Aspire`（默认）、`Consul` 或 `Kubernetes`。各发现源提供服务端点和网关准入信息。

- **Aspire** 读取 AppHost 注入的 `services:{serviceName}:{endpointName}:...`，仅接收绝对 `http` 或 `https` 地址。在 `ModuleConfigurations:ServiceGovernanceConfig:Services:{serviceName}` 中设置 `GatewayEnabled: true`，并把 `GatewayEndpointName` 设置为注入的端点名（如 `http`）。
- **Consul** 使用 `ConsulAddress`（默认 `http://127.0.0.1:8500`），通过 `Catalog.Services` 筛选带 `girvs.business=true` 标签的服务，再通过 `Health.Service` 以 `passingOnly: true` 获取健康实例。目标为 `http://{Address}:{Port}`。`Service.Address` 为空白时使用 `Node.Address`，端点名取每个实例的 `girvs.endpoint=<名>` 标签，缺省为 `http`。准入元数据取第一个健康实例的 `girvs.gateway-enabled=true` 和 `girvs.gateway-endpoint=<名>` 标签。
- **Kubernetes** 通过 `KubernetesLabelSelector`（默认 `girvs.io/business=true`）筛选 Service。端点名取 `port.Name`，为空白时使用 `port-{port}`。目标为 `http://{name}.{namespace}.svc.cluster.local:{port}`，其中 `{port}` 是 Service 端口。通过 `girvs.io/gateway-enabled: "true"` 和 `girvs.io/gateway-endpoint: "<端点名>"` 注解控制准入。

## 约定路由规则

网关仅处理 `GatewayEnabled == true` 且 `GatewayEndpointName` 非空白的服务。过滤后，同名服务取首次出现项，服务名去重区分大小写。只保留 `EndpointName` 与 `GatewayEndpointName` 忽略大小写匹配的端点，无匹配目标时跳过该服务。

每个符合条件的服务生成两条路由和一个集群。

| `RouteId` | `Match.Path` |
|---|---|
| `{ServiceName}` | `/{ServiceName}/{**catch-all}` |
| `{ServiceName}-root` | `/{ServiceName}`（无尾斜杠的服务根路径） |

两条路由均设置 `PathRemovePrefix = ServiceName`，并指向 `ClusterId = ServiceName`。`Destinations` 的键为 `{ServiceName}-{EndpointName}-{index}`，其中 `index` 是匹配端点从 0 开始的序号。以 `echo-a` 为例，`GET /echo-a/foo` 转发到下游 `/foo`，`GET /echo-a` 转发到下游根路径。

Label、Annotation 和 Tag 用于服务发现与网关准入。约定路由仅按服务名前缀转发，不支持基于这些元数据的分流或自定义路由规则。

## 转发空闲超时

`ActivityTimeout` 默认 5 分钟，写入每个动态集群的 `HttpRequest.ActivityTimeout`，限制两次转发 I/O 操作之间的最长间隔。大文件上传完成后等待后端处理，或下载时等待后端生成首批数据，如果需要更长时间，可以调大此值。

在网关的 `appsettings.json` 中按 `TimeSpan` 格式填写。以下示例将空闲超时设为 10 分钟。

```json
{
  "ModuleConfigurations": {
    "GirvsGatewayConfig": {
      "ActivityTimeout": "00:10:00"
    }
  }
}
```

`AddGirvsGateway(builder.Configuration)` 按网关配置的回退顺序读取该值。

## 流程防护

网关可选启用流程防护，阻止调用方跳过前置接口直接调用后续接口。既支持单条线性步骤序列，也支持按前置参数（Flow Selector）在同一组接口上选择流程组合（Flow Variant）。定义放在顶级 `FlowProtection` 配置节，内置状态存储通过 Redis Lua 原子执行票据创建、步骤占用、提交、释放和删除。`IFlowStateStore` 是公开的存储扩展接口。传入配置但未启用流程防护时，注册无操作实现。

启用前需要满足以下条件。

- 启用 `Girvs.Cache` 的分布式缓存，让 `CacheConfig.DistributedCacheConfig.ConnectionRef` 指向 `Resources` 中 `Type=redis` 的资源。宿主还需注册 `IRedisConnectionWrapper`，Girvs 宿主由缓存模块提供。
- 受保护的下游接口应通过 YARP 对外提供。
- 本功能不替代认证、授权、资源权限校验和业务幂等。

注册时从 `ModuleConfigurations:CacheConfig` 读取缓存配置，缺省时回退到顶级 `CacheConfig`。校验分布式缓存是否启用、`ConnectionRef` 是否已设置及其引用的资源是否为 `redis`。流程定义在 `FlowDefinitionRegistry` 首次解析时校验并编译为只读定义。以下接入示例在启动时显式解析注册表，使两类配置未通过校验时都在启动阶段抛出 `GirvsException`。流程配置变更需重启网关。

在 `appsettings.json` 中启用流程防护并配置 Redis。

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
        "ConnectionRef": ["gateway-redis"]
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

沿用前文的服务目录注册方式，再把流程防护中间件加入 YARP 管道。示例使用默认的响应头名称，并假定宿主已注册认证和授权服务。

```csharp
using Girvs.Gateway.FlowProtection;

// Girvs 宿主由 ServiceGovernanceModule 注册目录，独立宿主先调用 AddGirvsServiceDirectory。
builder.Services.AddGirvsGateway(builder.Configuration);

builder.Services.AddCors(options => options.AddPolicy("gateway", policy => policy
    .AllowAnyOrigin()
    .AllowAnyHeader()
    .AllowAnyMethod()
    .WithExposedHeaders(FlowProtectionHeaders.ResponseHeaders)));

var app = builder.Build();
_ = app.Services.GetRequiredService<FlowDefinitionRegistry>();

app.UseCors("gateway");
app.UseAuthentication();
app.UseAuthorization();

app.MapReverseProxy(proxyPipeline =>
{
    proxyPipeline.UseMiddleware<FlowTicketMiddleware>();
});
```

步骤 `Path` 使用下游服务路径。例如客户端请求 `/order/api/pay1` 时，配置写 `/api/pay1`。前置参数从请求头提取。

### 通用校验规则

- `DefaultTtlSeconds`、`CompletedTtlSeconds` 和流程级 `TtlSeconds` 均须在 1 至 1800 秒之间。流程级 `TtlSeconds` 省略时使用 `DefaultTtlSeconds`。`LockTtlSeconds` 必须大于 0 且不超过 `DefaultTtlSeconds`。
- `FlowId` 去除首尾空白后必填且忽略大小写唯一。每条线性流程或流程组合至少有两步。
- `Method` 仅支持 `GET`、`POST`、`PUT`、`DELETE`、`PATCH`、`HEAD`、`OPTIONS`。同一流程可在不同组合中引用同一个接口。

### 路径模板

线性流程的 `Steps` 和参数化流程的 `Interfaces` 都支持静态 `Path` 或整段 `{param}` 模板。配置路径以 `/` 开头，可带一个尾斜杠，不能包含空段或 `*`、`?`、`#`。参数名符合 `[A-Za-z_][A-Za-z0-9_]*`，在同一模板中忽略大小写唯一。`{id}` 匹配一个非空段。`v{id}`、`{id:int}` 等写法会在启动校验时报错。

以下线性流程配置中的 `/api/orders/{id}/confirm` 可匹配 `/api/orders/42/confirm` 或 `/API/orders/42/confirm/`。网关只按路径段匹配，不读取 `id` 作为业务标识。

```json
{
  "FlowId": "order-confirmation",
  "Steps": [
    { "Method": "POST", "Path": "/api/orders/{id}/prepare" },
    { "Method": "POST", "Path": "/api/orders/{id}/confirm" }
  ]
}
```

匹配时忽略方法名和字面路径段的大小写，非根请求路径移除一个尾斜杠。静态路径优先于模板，例如同一方法下 `/orders/create` 优先于 `/orders/{id}`。同一方法下静态路径不得重复。同段数的两个模板，只有在至少一对对应段均为字面量且互不相等时才可并存。例如 `/orders/{id}/items` 与 `/orders/{id}/history` 可并存，`/orders/{id}/items` 与 `/orders/export/{type}` 会在启动校验时报告冲突，并给出见证路径 `/orders/export/items`。

### 参数化多组合流程

同一流程需要按前置参数执行不同接口顺序时，使用 `ParameterHeaderName`、`Interfaces` 和 `Variants` 声明接口池与各组合的步骤顺序。

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
        "FlowId": "order-variants",
        "TtlSeconds": 300,
        "ParameterHeaderName": "X-Mode",
        "Interfaces": [
          { "Name": "A", "Method": "POST", "Path": "/api/a" },
          { "Name": "B", "Method": "POST", "Path": "/api/b" },
          { "Name": "C", "Method": "POST", "Path": "/api/c" },
          { "Name": "D", "Method": "POST", "Path": "/api/d" },
          { "Name": "E", "Method": "POST", "Path": "/api/e" }
        ],
        "Variants": {
          "ABE": ["A", "B", "E"],
          "ACE": ["A", "C", "E"],
          "ADE": ["A", "D", "E"],
          "DE": ["D", "E"]
        }
      }
    ]
  }
}
```

编译参数化流程时执行下列校验。

- 同一流程不能同时配置 `Steps` 与 `ParameterHeaderName`、`Interfaces`、`Variants`。
- `ParameterHeaderName` 不能为空白，也不能与 `TicketHeaderName`、`BusinessIdHeaderName`、`X-Flow-Next-Index`、`X-Flow-Completed`、`X-Flow-Business-Id` 冲突（忽略大小写）。
- `Interfaces` 必须非空，接口名 `Trim()` 后忽略大小写唯一，接口池内遵循上述路径冲突规则。
- `Variants` 必须非空，组合键 `Trim()` 后忽略大小写唯一。每条组合仅可引用已定义的接口，且不重复引用同一接口。
- 每个接口至少被一条组合引用。调用方必须选定已配置的组合。

运行期按以下规则选择和推进组合。例中的请求头使用默认名称。

- **流程票据**。请求带有非空白 `X-Flow-Ticket` 时，按票据锁定的流程组合推进。票据头缺失或为空白时，按新流程首步校验。
- **新流程首步**。`ParameterHeaderName` 指定的请求头必须是单值，`Trim()` 后忽略大小写匹配某条组合。参数缺失、空白、同名多值或未配置取值时返回 `403 FLOW_SELECTOR_INVALID`。当前接口并非该组合首步时返回 `403 FLOW_NOT_FOUND`。
- **推进已有流程**。必须携带非空 `X-Business-Id`，缺失时返回 `403 FLOW_BUSINESS_MISMATCH`。前置参数可以省略，携带时必须与票据锁定组合一致。空白、同名多值或冲突取值返回 `403 FLOW_SELECTOR_MISMATCH`。
- **重叠入口**。`D` 同时是 `DE` 第 1 步和 `ADE` 第 2 步。无票且带 `DE` 参数调用 `D` 会开启 `DE`。持 `ADE` 首步签发的票据调用 `D` 会推进到 `ADE` 第 2 步。两种情形都只允许下一步调用 `E`，票据始终绑定各自的组合。
- **越序与切换**。跳步、跨组合调用（如持 `ACE` 票据调 `B`）、持未完成票据重调首步，均返回 `403 FLOW_STEP_NOT_ALLOWED`。

### 请求头、响应头与结算

`TicketHeaderName`（默认 `X-Flow-Ticket`）用于请求和未完成的响应，`BusinessIdHeaderName`（默认 `X-Business-Id`）用于请求。`X-Flow-Next-Index`、`X-Flow-Completed` 和下游回传用的 `X-Flow-Business-Id` 是固定常量。使用自定义票据头时，跨域配置的 `WithExposedHeaders` 也要使用实际名称。

- 流程未完成时，响应包含票据头、`X-Flow-Next-Index` 和 `Cache-Control: no-store`。
- 流程完成时，响应包含 `X-Flow-Completed: true` 和 `Cache-Control: no-store`。
- 下游可在首步成功时通过 `X-Flow-Business-Id` 回传业务标识。网关在提交时写入或核对 Redis 中的绑定值，并从客户端响应中移除该头。
- 下游返回 2xx 时，网关提交状态并推进步骤或标记完成。首步返回非 2xx 时删除整张票据，后续步骤返回非 2xx 时仅释放当前步骤的处理锁。结算异常时，响应转换将状态设为 HTTP 503、抑制下游响应体并重新抛出异常。
- 自定义日志可用 `FlowTicketService.GetTicketFingerprint` 记录票据的 SHA-256 摘要前 12 位。当前内置中间件的兜底清理失败警告仍会记录原始票据，应按敏感日志处理。

### 错误码与前端处理

流程拒绝和路由配置错误以 `application/problem+json` 返回 `ProblemDetails`，其中 `code` 为下列值。

| HTTP | `code` | 含义 |
|---|---|---|
| 403 | `FLOW_NOT_FOUND` | 票据缺失或不存在（包括 Redis 键 TTL 到期），或当前接口并非所选组合的首步 |
| 403 | `FLOW_EXPIRED` | 已过期状态的错误码映射项。Redis 键 TTL 到期后实际返回 `FLOW_NOT_FOUND` |
| 403 | `FLOW_BUSINESS_MISMATCH` | 推进时业务标识缺失，或与 Redis 绑定值不一致 |
| 403 | `FLOW_STEP_NOT_ALLOWED` | 当前步骤不是流程允许的下一步 |
| 403 | `FLOW_IN_PROGRESS` | 同一步已有未过期处理锁 |
| 403 | `FLOW_COMPLETED` | 流程已完成，重复调用被拒绝 |
| 403 | `FLOW_SELECTOR_INVALID` | 参数化流程新开时前置参数缺失、空白、同名多值或未配置 |
| 403 | `FLOW_SELECTOR_MISMATCH` | 推进时携带的前置参数与票据锁定组合不一致，或为空白、同名多值 |
| 500 | `FLOW_CONFIGURATION_ERROR` | 路由未配置唯一有效的 `PathRemovePrefix`，或此前缀无法从请求路径移除 |

前端只保存服务端返回的票据，建议以内存按 `flowId + businessId` 为键保存。后续受保护请求附加配置的票据头和业务标识头。收到 `FLOW_*` 403 后清除本地票据并提示用户重新开始。票据是持有者凭据，当前不绑定 `userId` 或 `tenantId`。避免持久化到 LocalStorage 或将原值写入业务日志。

## 目录刷新、热更新与健康检查

`AddGirvsServiceDirectory` 注册后台服务 `ServiceDirectoryRefreshService`。宿主启动后自动执行首次发现，每轮结束后等待 `DiscoveryRefreshInterval` 再刷新，该值以秒为单位，默认 30 秒，最小 1 秒。刷新失败时通过 `ILogger` 记录警告、保留最后一次成功快照，并在下一轮重试。

`ServiceDirectory` 仅在快照实际变化时触发 `ServicesChanged`。`GirvsGatewayProxyConfigProvider` 订阅该事件，在锁内构建并发布携带新 `ChangeToken` 的配置，再取消旧令牌。YARP 随后调用 `GetConfig()` 加载新路由和集群。变更发现延迟取决于轮询间隔和查询耗时。

目录注册 `service-directory` 健康检查，标签为 `ready`。尚无成功刷新记录，或距上次成功刷新超过 `MaxStaleDuration`（默认 90 秒，最小 1 秒）时返回 `Unhealthy`。每次成功刷新都会更新健康检查所用的时间，包括成功获取空目录或内容未变化的快照。Girvs 宿主的 `HealthCheckPath`（默认 `/health`）包含目录检查，`LivenessCheckPath`（默认 `/alive`）只执行带 `live` 标签的检查。独立宿主可按接入示例映射 `/health`。

## Kubernetes 部署

将 `ServiceDiscoveryProvider` 设为 `Kubernetes`。客户端通过 `KubernetesClientConfiguration.InClusterConfig()` 读取网关 Pod 的 ServiceAccount 凭据。Kubernetes API Server 短暂不可用时，目录沿用最后一次成功快照并在下一轮刷新时自动重试。

`KubernetesNamespace` 默认为空，目录通过 `ListServiceForAllNamespacesAsync` 跨所有命名空间列出 Service，使用 `ClusterRole` 和 `ClusterRoleBinding`。配置了命名空间时，目录通过 `ListNamespacedServiceAsync` 查询该命名空间，在该命名空间创建 `Role` 和 `RoleBinding` 即可。两种方式都只需 `services` 的 `list` 权限。以下为跨命名空间配置示例。

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

此示例的网关 Deployment 应位于 `default` 命名空间，并在 Pod spec 中指定 `serviceAccountName: girvs-gateway`。

以下业务 Service 使用默认筛选标签，并通过注解开放名为 `http` 的端口给网关。

```yaml
apiVersion: v1
kind: Service
metadata:
  name: echo-a
  labels:
    girvs.io/business: "true"
  annotations:
    girvs.io/gateway-enabled: "true"
    girvs.io/gateway-endpoint: "http"
spec:
  selector:
    app: echo-a
  ports:
    - name: http
      port: 5678
```

## 示例与部署方式

优先参考 [samples/Sample.Gateway/](../samples/Sample.Gateway/)，它使用 Girvs 宿主和 Aspire 服务目录，提供集中 Swagger UI 和 `/health`。[samples/gateway-k8s/](../samples/gateway-k8s/) 提供 Kubernetes 部署参考清单。使用该清单时，为后端 Service 补齐 `girvs.io/business` 标签和 `girvs.io/gateway-*` 注解，并使准入端点与 Service 端口名一致。

| `ServiceDiscoveryProvider` | 适用场景 |
|---|---|
| `Aspire` | 由 AppHost 注入服务端点的本地开发和联调 |
| `Consul` | 使用 Consul 注册中心和实例健康检查的部署 |
| `Kubernetes` | 网关在集群内运行，通过 Service 发现后端 |

三种模式共用同一套网关接入代码和路由约定。在网关配置中，通过 `ServiceGovernanceConfig` 切换发现方式及其对应参数。

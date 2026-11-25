# Girvs.Gateway

`Girvs.Gateway` 面向 `net10.0` 和 `net11.0`，基于 `Yarp.ReverseProxy` 2.3.0，引用 `Girvs`、提供 `IServiceDirectory` 的 `Girvs.ServiceGovernance` 和提供 Redis 连接封装的 `Girvs.Cache`。网关根据统一服务目录按服务名前缀生成路由和集群，并支持可选的流程防护。

动态集群未配置主动或被动健康检查、自定义负载均衡策略、自动重试或限流。Host、Header 和 Label 均不参与约定路由分流。发现源的健康过滤与网关的转发行为分别生效。

## 接入网关

| 接入项 | Girvs 宿主 | 独立 ASP.NET Core 宿主 |
|---|---|---|
| 服务目录注册 | `ServiceGovernanceModule` 调用 `AddGirvsServiceDirectory` | 从配置绑定 `ServiceGovernanceConfig`，显式调用 `AddGirvsServiceDirectory` |
| 发现启动 | 宿主启动 `ServiceDirectoryRefreshService` | 同左 |
| 网关注册 | 在 `IGirvsStartup.ConfigureServices` 调用 `AddGirvsGateway(configuration)`，在端点配置中调用 `MapReverseProxy` | 在 `builder.Services` 调用 `AddGirvsGateway(builder.Configuration)`，构建后调用 `app.MapReverseProxy` |
| 健康端点 | 模块自动映射 `HealthCheckPath` 及 `LivenessCheckPath`，并注册带 `live` 标签的 `self` 检查 | 宿主自行映射健康端点，并按需注册存活检查 |
| Redis 连接封装 | 启用 Redis 分布式缓存后由 `GirvsCacheModule` 注册 `IRedisConnectionWrapper` | 启用流程防护时由宿主注册 `IRedisConnectionWrapper` 并提供连接配置 |
| 全局中间件顺序 | 在路由与端点执行之间启用 CORS、认证、授权，按此顺序配置。使用 `GirvsAuthorizeModule` 时由该模块添加认证、授权中间件 | 按需设置 `UseRouting` → `UseCors` → `UseAuthentication` → `UseAuthorization`，随后执行映射的代理端点 |

`AddGirvsGateway(IConfiguration configuration = default)` 注册以下组件。

- `GirvsGatewayConfig` 实现 `IAppModuleConfig`。扩展方法按 `ModuleConfigurations:GirvsGatewayConfig` → 顶级 `GirvsGatewayConfig` → 默认实例的顺序绑定并注册单例。
- `IProxyConfigProvider` 使用单例 `GirvsGatewayProxyConfigProvider`。
- YARP 全局转换使用 `AddOriginalHost(false)`（使用目标 Host）、`CopyRequestHeaders = true`（复制请求头）和 `AddXForwarded(ForwardedTransformActions.Append)`（追加 `X-Forwarded-*`）。
- 只有传入非空 `configuration` 才会调用 `AddFlowProtection(configuration)`。无参调用后若直接挂载 `FlowTicketMiddleware`，YARP 会在 `MapReverseProxy` 构建代理管道时因缺少依赖而抛出 DI 异常。可通过公开扩展 `AddFlowProtection(configuration)` 单独注册，或直接将配置传给 `AddGirvsGateway`。

`AddGirvsServiceDirectory` 一并注册后台刷新服务和 `service-directory` 健康检查。每个宿主调用一次，避免重复的注册项和同名健康检查。后台服务由宿主自动启动。首轮发现成功前目录为空，请求约定代理路径会返回 404。

独立宿主的最小接入示例使用默认关闭的流程防护。

```csharp
using Girvs.Gateway;
using Girvs.ServiceGovernance.Configuration;
using Girvs.ServiceGovernance.Discovery;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);
var governance = builder.Configuration
    .GetSection("ModuleConfigurations:ServiceGovernanceConfig")
    .Get<ServiceGovernanceConfig>() ?? new ServiceGovernanceConfig();

builder.Services.AddGirvsServiceDirectory(governance);
builder.Services.AddGirvsGateway(builder.Configuration);
builder.Services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);

var app = builder.Build();
app.MapHealthChecks(governance.HealthCheckPath);
app.MapHealthChecks(governance.LivenessCheckPath, new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("live"),
});
app.MapReverseProxy();
app.Run();
```

## 配置参考

### `GirvsGatewayConfig`

配置位置及回退顺序见 [接入网关](#接入网关)。进程启动时读取配置快照，修改后重启网关。

| 字段 | 类型 | 默认值 | 用途和约束 |
|---|---|---|---|
| `ActivityTimeout` | `TimeSpan` | `00:05:00` | 写入每个动态集群的 `HttpRequest.ActivityTimeout`，限制相邻两次转发 I/O 之间的最大空闲间隔。请求总时长可以超过此值。网关无内置范围校验，请配置正值。 |

```json
{
  "ModuleConfigurations": {
    "GirvsGatewayConfig": { "ActivityTimeout": "00:10:00" }
  }
}
```

### `ServiceGovernanceConfig`

配置位于 `ModuleConfigurations:ServiceGovernanceConfig`。整数时长均以秒计。

| 字段 | 类型 | 默认值 | 适用范围 | 用途和约束 |
|---|---|---|---|---|
| `ServiceDiscoveryProvider` | 枚举 | `Aspire` | 全部 | 可选 `Aspire`、`Consul`、`Kubernetes`。 |
| `DiscoveryRefreshInterval` | `int` | `30` | 全部 | 每轮查询结束后到下一轮开始的等待时间，运行时下限为 `1`。 |
| `MaxStaleDuration` | `int` | `90` | 全部 | 目录最大可接受陈旧时长，检查时下限为 `1`。 |
| `Services` | `Dictionary<string, ServiceGovernanceServiceConfig>` | `{}` | Aspire 发现 | 按服务名提供准入元数据。Consul 和 Kubernetes 的目录提供程序忽略此节。Girvs 的 Consul 自注册单独读取本服务对应项来生成准入标签。 |
| `Services:<服务名>:GatewayEnabled` | `bool` | `false` | Aspire 发现、Consul 自注册 | 控制服务是否开放给网关。 |
| `Services:<服务名>:GatewayEndpointName` | `string?` | `null` | Aspire 发现、Consul 自注册 | 选择开放的端点名。 |
| `ConsulAddress` | `string` | `http://127.0.0.1:8500` | Consul 发现和自注册 | Consul API 地址。 |
| `ConsulRegistrationAddress` | `string?` | `http://127.0.0.1` | Consul 自注册 | 本服务的绝对 HTTP(S) URI，注册时取主机和端口。空白时记录警告并跳过自注册。 |
| `ServerName` | `string` | `""` | Consul 自注册 | 服务名来源，空白时从程序集名推导。 |
| `CurrentServerModel` | 枚举 | `WebApi` | Consul 自注册 | `WebApi` 使用 HTTP 健康检查，`GrpcService` 使用 gRPC 健康检查。 |
| `Interval` | `int` | `10` | Consul 自注册 | Consul 检查间隔。 |
| `Timeout` | `int` | `30` | Consul 自注册 | Consul 单次检查超时。 |
| `DeregisterCriticalServiceAfter` | `int` | `90` | Consul 自注册 | Consul 持续判定实例为 `critical` 后注销实例的时长。 |
| `KubernetesLabelSelector` | `string` | `girvs.io/business=true` | Kubernetes | 查询 Service 时使用的 Label Selector。 |
| `KubernetesNamespace` | `string?` | `null` | Kubernetes | 为空或空白时跨全集群查询，否则仅查询指定命名空间。 |
| `HealthCheckPath` | `string` | `/health` | 全部 | Girvs 就绪端点包含目录检查。Consul HTTP 自注册和 Aspire 就绪探针也使用此路径。Girvs 端点映射要求非空且以 `/` 开头，否则抛 `GirvsException`。 |
| `LivenessCheckPath` | `string` | `/alive` | 全部 | Girvs 存活端点只运行 `live` 检查，Aspire 存活探针也使用此路径。Girvs 端点映射要求非空且以 `/` 开头，否则抛 `GirvsException`。 |

### `FlowProtection`

配置位于顶级 `FlowProtection` 节。`Flows` 元素使用 `FlowDefinitionOptions`，其 `Steps` 和 `Interfaces` 元素分别使用 `FlowStepOptions` 和 `FlowInterfaceOptions`。

| 类型 | 字段 | 字段类型 | 默认值 | 约束和用途 |
|---|---|---|---|---|
| `FlowProtectionOptions` | `Enabled` | `bool` | `false` | 控制流程校验和响应结算。 |
| `FlowProtectionOptions` | `DefaultTtlSeconds` | `int` | `300` | `1`–`1800`，未指定流程级 `TtlSeconds` 时使用。 |
| `FlowProtectionOptions` | `LockTtlSeconds` | `int` | `30` | `1`–`DefaultTtlSeconds`，适用于每次步骤占用。 |
| `FlowProtectionOptions` | `CompletedTtlSeconds` | `int` | `60` | `1`–`1800`，末步成功结算后的票据保留期。 |
| `FlowProtectionOptions` | `TicketHeaderName` | `string` | `X-Flow-Ticket` | 票据请求头及非末步成功时使用的响应头。 |
| `FlowProtectionOptions` | `BusinessIdHeaderName` | `string` | `X-Business-Id` | 调用方提供业务标识的请求头。 |
| `FlowProtectionOptions` | `Flows` | `List<FlowDefinitionOptions>` | `[]` | 流程定义列表，元素不能是 `null`。 |
| `FlowDefinitionOptions` | `FlowId` | `string` | `""` | `Trim()` 后必填，全局忽略大小写唯一。 |
| `FlowDefinitionOptions` | `TtlSeconds` | `int?` | `null` | `1`–`1800`，省略时使用 `DefaultTtlSeconds`。 |
| `FlowDefinitionOptions` | `Steps` | `List<FlowStepOptions>` | `[]` | 线性步骤序列，至少两步。非空列表与参数化字段互斥。 |
| `FlowDefinitionOptions` | `ParameterHeaderName` | `string` | `""` | 参数化流程的前置参数请求头，须非空且与保留头名无冲突。 |
| `FlowDefinitionOptions` | `Interfaces` | `List<FlowInterfaceOptions>` | `[]` | 参数化流程的接口池，至少一项，每个接口须被引用。 |
| `FlowDefinitionOptions` | `Variants` | `Dictionary<string, List<string>>` | `{}` | 参数化流程的组合映射，至少一个组合，每个组合至少引用两个接口。 |
| `FlowStepOptions` | `Method` | `string` | `""` | 必填，使用 HTTP 方法白名单。 |
| `FlowStepOptions` | `Path` | `string` | `""` | 必填，下游绝对路径，支持整段 `{param}`。 |
| `FlowInterfaceOptions` | `Name` | `string` | `""` | 接口池中的引用名称，`Trim()` 后必填且忽略大小写唯一。 |
| `FlowInterfaceOptions` | `Method` | `string` | `""` | 必填，使用 HTTP 方法白名单。 |
| `FlowInterfaceOptions` | `Path` | `string` | `""` | 必填，下游绝对路径，支持整段 `{param}`。 |

## 服务发现与网关准入

| 发现模式 | 发现源和业务筛选 | 上游地址和端点名 | 准入元数据 |
|---|---|---|---|
| `Aspire` | AppHost 注入的 `services:{serviceName}:{endpointName}:...`。仅收录至少有一个有效地址的服务。 | 接受绝对 `http` 或 `https` URI，端点名取配置节键。 | 从治理配置的 `Services:{serviceName}` 读取 `GatewayEnabled`、`GatewayEndpointName`。缺少元数据时默认不准入。 |
| `Consul` | `Catalog.Services` 逐字匹配业务标签 `girvs.business=true`，区分大小写。再以 `passingOnly: true` 调用 `Health.Service`，无健康实例时跳过。 | 固定 `http://{Address}:{Port}`。`Service.Address` 空白时回退 `Node.Address`。各实例从 `girvs.endpoint=<名称>` 取端点名，缺少该标签时用 `http`。 | 取查询返回的首个健康实例的 `girvs.gateway-enabled=true` 和 `girvs.gateway-endpoint=<名称>`。 |
| `Kubernetes` | 按 `KubernetesLabelSelector` 定期列出 Service。缺少名称、命名空间或端口的 Service 会被跳过。 | 固定 `http://{name}.{namespace}.svc.cluster.local:{port}`。使用 Service 的 `port` 和 `port.Name`，未命名时用 `port-{port}`。 | 从 Service 注解 `girvs.io/gateway-enabled: "true"` 和 `girvs.io/gateway-endpoint: "<端点名>"` 读取。 |

- Aspire 提供程序未发现任何带有效 HTTP(S) 地址的服务时记录 Warning。若目录有服务但都未准入，仍可能没有代理路由。Girvs 宿主在 Aspire 模式还会为 `HttpClient` 启用 `AddServiceDiscovery()` 和 `AddStandardResilienceHandler()`，这是宿主的 `HttpClient` 配置。YARP 动态集群未由此获得重试策略。
- Consul 端点和准入标签的键前缀忽略大小写，`girvs.gateway-enabled` 的 `true` 也忽略大小写。Girvs 宿主在 Consul 模式会按配置尝试向 Consul 注册网关自身。
- Kubernetes 使用 `InClusterConfig()` 读取 Pod 的 ServiceAccount 凭据，在集群外无法初始化。地址构造忽略 `port.Protocol` 和 `appProtocol`，始终使用 `http://`。

## 约定路由与转发

1. `ServiceDirectory.Publish` 按 `ServiceName` 排序，各服务的端点按 `EndpointName`、`Address.AbsoluteUri` 排序。三处均使用 `OrdinalIgnoreCase`。
2. 网关先过滤 `GatewayEnabled == true` 且 `GatewayEndpointName` 非空白的服务，再以区分大小写的服务名去重，保留首次出现项。应避免注册名称仅大小写不同的服务。在 Kubernetes 跨命名空间发现时，也应避免开放同名 Service。
3. 仅保留端点名与 `GatewayEndpointName` 忽略大小写匹配的目标。匹配后的顺序确定从零开始的 `{index}`。没有匹配目标时，整个服务跳过。

| 生成项 | 标识 | 匹配或配置 |
|---|---|---|
| 通配路由 | `{ServiceName}` | `Match.Path = /{ServiceName}/{**catch-all}`，指向同名集群。 |
| 根路由 | `{ServiceName}-root` | `Match.Path = /{ServiceName}`，指向同名集群，显式覆盖无尾斜杠的服务根入口。 |
| 路径转换 | 两条路由共用 | `PathRemovePrefix = ServiceName`。 |
| 动态集群 | `{ServiceName}` | `HttpRequest.ActivityTimeout` 和筛选后的 `Destinations`。 |
| 集群目标 | `{ServiceName}-{EndpointName}-{index}` | `Address = endpoint.Address.AbsoluteUri`。 |

例如 `echo-a` 准入端点名为 `http`，两个实例排序后分别为 `http://10.0.0.11:8080/` 和 `http://10.0.0.12:8080/`。生成路由 `echo-a`、`echo-a-root` 和集群 `echo-a`，目标依次为 `echo-a-http-0`、`echo-a-http-1`。

| 客户端请求 | 路由 | 下游请求路径 |
|---|---|---|
| `GET /echo-a/foo` | `echo-a` | `/foo` |
| `GET /echo-a/foo?limit=5` | `echo-a` | `/foo?limit=5` |
| `GET /echo-a` | `echo-a-root` | `/` |

网关保留 `-root` 路由显式映射 `GET /{ServiceName}`。在 YARP 2.3.0 与 ASP.NET Core 11 的验证中，仅保留通配路由也能匹配无尾斜杠的服务根路径。目录移除服务后，网关同步移除对应路由和集群，YARP 加载新快照后该服务路径返回 404。后端实例失效直到被发现源剔除前，转发可能间歇返回 502。

## 流程防护（FlowProtection）

流程防护约束受保护的下游接口按预定顺序调用。它支持固定步骤的线性流程，以及通过请求头前置参数（Flow Selector）选择流程组合（Flow Variant）的参数化流程。认证、授权、资源权限校验和业务幂等仍由宿主及下游实现。内置 `RedisFlowStateStore` 通过 Lua 原子执行票据创建、步骤占用、提交、释放和删除。`IFlowStateStore` 是公开的存储接口。

### 启用条件与校验时机

- 设置 `FlowProtection:Enabled = true` 和 `CacheConfig:DistributedCacheConfig:Enabled = true`。`ConnectionRef` 为非空字符串数组，首项为资源键或 `string.Format` 模板，其余项为格式参数，例如 `["{0}-redis", "gateway"]` 指向 `gateway-redis`。
- `Resources` 对应项的 `Type` 必须为 `redis`（忽略大小写），`redis-synchronized-memory` 不支持。Redis 连接需要非空 `Settings:Endpoints`，并由宿主提供 `IRedisConnectionWrapper`。
- 流程防护先从 `ModuleConfigurations:CacheConfig` 绑定缓存配置，再回退到顶级 `CacheConfig`。Girvs 宿主的 `GirvsCacheModule` 从模块配置读取缓存设置。Girvs 宿主请统一使用 `ModuleConfigurations:CacheConfig`，让流程防护和缓存模块读取同一配置。
- `AddGirvsGateway(configuration)` 或 `AddFlowProtection(configuration)` 在注册期检查分布式缓存开关、`ConnectionRef`、对应资源及资源类型。`Settings:Endpoints` 由连接配置处理，Girvs 宿主在缓存模块注册时读取。注册期资源检查不连接 Redis，也不检查封装服务是否已注册。
- 业务定义在首次解析 `FlowDefinitionRegistry` 时校验并编译。可在 `Build()` 后显式解析，使配置错误在启动时暴露。流程配置在注册期绑定，修改后需重启网关。
- `Enabled = false` 时以默认空定义构造注册表，跳过已配置流程定义及 Redis 资源的校验，注册无操作存储，不注册响应转换。中间件直接放行。

以下配置可用于 Girvs 宿主。`Steps.Path` 填写剥离服务名前缀后的路径，客户端访问 `/order/api/pay` 时，此处填写 `/api/pay`。查询字符串和前置参数头不属于步骤路径。

```json
{
  "FlowProtection": {
    "Enabled": true,
    "Flows": [
      {
        "FlowId": "order-payment",
        "Steps": [
          { "Method": "POST", "Path": "/api/prepare" },
          { "Method": "POST", "Path": "/api/pay" }
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
      "Settings": { "Endpoints": "redis:6379" }
    }
  }
}
```

### 代理管道与 CORS

`FlowTicketMiddleware` 依赖 `IReverseProxyFeature.Route`，必须通过 `MapReverseProxy(proxyPipeline => proxyPipeline.UseMiddleware<FlowTicketMiddleware>())` 挂在代理管道内。全局 `UseMiddleware<FlowTicketMiddleware>()` 无法提供约定的代理路由上下文。

下面以独立宿主为例。服务目录和健康端点沿用前文。示例沿用上面的 `gateway-redis` 配置，并假设宿主已注册认证和授权服务。Girvs 宿主使用缓存模块提供的 Redis 封装，按宿主入口配置全局中间件。

```csharp
using Girvs.Cache.CacheImps;
using Girvs.Gateway;
using Girvs.Gateway.FlowProtection;

// builder 是已注册服务目录的 WebApplicationBuilder。
var redisEndpoints = builder.Configuration["Resources:gateway-redis:Settings:Endpoints"];
if (string.IsNullOrWhiteSpace(redisEndpoints))
    throw new InvalidOperationException("缺少 gateway-redis 的 Settings:Endpoints");

builder.Services.AddStackExchangeRedisCache(options => options.Configuration = redisEndpoints);
builder.Services.AddSingleton<IRedisConnectionWrapper, RedisConnectionWrapper>();
builder.Services.AddGirvsGateway(builder.Configuration);

var ticketHeader = builder.Configuration["FlowProtection:TicketHeaderName"] ?? FlowProtectionHeaders.Ticket;
builder.Services.AddCors(options => options.AddPolicy("gateway", policy => policy
    .WithOrigins("https://frontend.example.com")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .WithExposedHeaders(ticketHeader, FlowProtectionHeaders.NextIndex, FlowProtectionHeaders.Completed)));

var app = builder.Build();
_ = app.Services.GetRequiredService<FlowDefinitionRegistry>();

app.UseRouting();
app.UseCors("gateway");
app.UseAuthentication();
app.UseAuthorization();
app.MapReverseProxy(proxyPipeline => proxyPipeline.UseMiddleware<FlowTicketMiddleware>());
app.Run();
```

`FlowProtectionHeaders.ResponseHeaders` 是由默认头名常量组成的静态只读数组，包含 `X-Flow-Ticket`、`X-Flow-Next-Index`、`X-Flow-Completed`。默认配置可直接将该数组传给 `WithExposedHeaders`。自定义 `TicketHeaderName` 时必须显式暴露实际头名，如上例。

### 通用校验与路径模板

- `FlowId` 去除首尾空白后必填且全局忽略大小写唯一。线性流程和每个参数化组合均至少有两步。
- `DefaultTtlSeconds`、`CompletedTtlSeconds` 和流程级 `TtlSeconds` 的范围均为 `1`–`1800`。`LockTtlSeconds` 范围为 `1`–`DefaultTtlSeconds`，上限使用全局默认 TTL。
- `Method` 去除首尾空白并转成大写，可选 `GET`、`POST`、`PUT`、`DELETE`、`PATCH`、`HEAD`、`OPTIONS`。
- `Flows`、`Steps`、`Interfaces` 列表不能含 `null` 项。非空 `Steps` 与任何已设置的参数化字段互斥。`Steps` 为空且无参数化字段时按线性流程处理，报步骤不足。
- 同一个静态 `Method + Path` 在整个注册表只能归属一个流程入口。线性步骤序列及接口池内也禁止重复。参数化流程可在多个组合中引用同一个接口。

`Steps` 和 `Interfaces` 的 `Path` 支持相同语法。配置先执行 `Trim()`，再规范化单个尾斜杠。

| 配置值 | 结果 | 规则 |
|---|---|---|
| `/` | 合法 | 根路径。 |
| `/orders/{id}/` | 合法 | 规范化为 `/orders/{id}`。 |
| `/orders/{_order2}` | 合法 | 参数名符合 `[A-Za-z_][A-Za-z0-9_]*`。 |
| `orders/{id}` | 非法 | 缺少起始 `/`。 |
| `/orders//items`、`/orders//` | 非法 | `//` 产生空段。 |
| `/orders/*`、`/orders?x=1`、`/orders#items` | 非法 | 禁用 `*`、`?`、`#`。 |
| `/orders/v{id}`、`/orders/{id`、`/orders/{id:int}` | 非法 | 参数须独占一个段且成对闭合，类型约束不支持。 |
| `/orders/{订单}`、`/orders/{2id}`、`/orders/{order-id}` | 非法 | 参数名只能使用指定的 ASCII 字符，首字符须为字母或下划线。 |
| `/orders/{id}/items/{Id}` | 非法 | 同一模板的参数名忽略大小写唯一。 |

| 匹配或冲突类型 | 判定 | 示例 |
|---|---|---|
| 请求匹配 | 方法及字面段忽略大小写，请求路径最多去掉一个尾斜杠。参数段匹配任意非空段。 | `/API/orders/42/confirm/` 命中 `/api/orders/{id}/confirm`。`/a/b//` 含残留空段，不命中。 |
| 请求形状无效 | 空路径、缺少起始 `/`、不支持的方法或规范化后仍含空段的路径在匹配阶段返回未命中。匹配器无需按配置路径语法解析请求，因此这些输入不会引发配置异常。 | 路径参数只供匹配，票据及业务标识仍按各自请求头处理。 |
| 模板重复 | 同方法、同段数，模板路径规范化后忽略大小写相同。 | `/orders/{id}/items` 与 `/Orders/{ID}/items/`。 |
| 同形异名冲突 | 同方法、同段数，参数位置一致且对应字面段相同，参数名有差异。 | `/orders/{id}/items` 与 `/orders/{orderId}/items`。 |
| 交叉重叠冲突 | 同方法、同段数，参数位置不同，所有相对的字面段均兼容。 | `/orders/{id}/items` 与 `/orders/export/{type}`，报错包含见证路径 `/orders/export/items`。 |
| 可并存的模板 | 方法不同、段数不同，或至少一对对应的字面段值不同。 | `/orders/{id}/items` 与 `/orders/{id}/history`。 |
| 静态优先 | 同方法下先查全局静态路径，再查模板。静态和模板相交时配置通过，静态路径静默遮蔽模板的对应请求，即使二者属于不同流程。 | `POST /orders/create` 优先于 `POST /orders/{id}`。 |

模板冲突检查覆盖流程内的线性步骤、接口池内的接口和不同流程之间的接口。配置时核对静态路径的优先级，避免模板步骤被其他流程的静态路径接管。请求匹配不会从 `{id}` 提取业务标识，也不会用其限制票据可访问的资源。

线性流程模板示例，作为一个 `FlowProtection:Flows` 元素使用。

```json
{
  "FlowId": "order-confirmation",
  "Steps": [
    { "Method": "POST", "Path": "/api/orders/{id}/prepare" },
    { "Method": "POST", "Path": "/api/orders/{id}/confirm" }
  ]
}
```

### 参数化多组合流程

下面的接口池示例使用模板。`D` 同时是 `DE` 的首步和 `ADE` 的第二步。将示例对象加入 `FlowProtection:Flows`。

```json
{
  "FlowId": "order-variants",
  "ParameterHeaderName": "X-Mode",
  "Interfaces": [
    { "Name": "A", "Method": "POST", "Path": "/api/orders/{id}/a" },
    { "Name": "D", "Method": "POST", "Path": "/api/orders/{id}/d" },
    { "Name": "E", "Method": "POST", "Path": "/api/orders/{id}/e" }
  ],
  "Variants": {
    "ADE": ["A", "D", "E"],
    "DE": ["D", "E"]
  }
}
```

编译参数化流程时执行以下检查。

- `ParameterHeaderName` 经 `Trim()` 后必填，不得与 `TicketHeaderName`、`BusinessIdHeaderName`、`X-Flow-Next-Index`、`X-Flow-Completed`、`X-Flow-Business-Id` 冲突，比较忽略大小写。
- `Interfaces` 非空。接口名称经 `Trim()` 后必填且忽略大小写唯一，并遵循前述路径匹配和冲突规则。`Variants` 非空，组合键也经 `Trim()` 后必填且忽略大小写唯一。
- 每个组合至少引用两个已定义接口，引用名称去除首尾空白并忽略大小写。同一组合不能重复引用同一接口，每个接口至少被一个组合引用。

运行期按票据判断当前调用是启动还是推进。

| 请求 | 判定与结果 |
|---|---|
| 无票，调用组合首步 | `ParameterHeaderName` 指定的值必须合法且忽略大小写匹配已配置组合，否则 403 `FLOW_SELECTOR_INVALID`。选定组合后，仅允许其首步签发票据并锁定组合。 |
| 无票，调用所选组合的后续步骤 | 403 `FLOW_NOT_FOUND`。例如带 `X-Mode: ADE` 且无票调用 `D`。 |
| 持票，调用组合的下一步 | 必须携带 `BusinessIdHeaderName` 指定的业务标识。前置参数可省略，若携带，格式必须有效且匹配已锁定组合，否则 403 `FLOW_SELECTOR_MISMATCH`。 |
| 持同一票据并发重调当前步骤 | 请求通过前置校验且仍占用该步骤时，返回 403 `FLOW_IN_PROGRESS`。若首步票据尚可被调用方取得，同一规则适用。正常客户端在首步 2xx 结算后才收到票据，无票重调首步可另外开一条流程。 |
| 持票重调已提交首步、跳步或调用其他组合独有接口 | 票据尚未进入完成态时，返回 403 `FLOW_STEP_NOT_ALLOWED`。完成态优先返回 `FLOW_COMPLETED`。 |

线性流程按固定顺位识别首步，调用其首步总是尝试新建票据。上述持票重调首步的判定适用于参数化流程。

#### `ADE` 与 `DE` 的重叠入口报文

假设网关开放 `order` 服务，头名均使用默认值。`<ticket-ade>` 和 `<ticket-de>` 代表服务端分别返回的两张票据，后续请求必须使用实际值。报文省略通用头。

`ADE` 从 `A` 获得票据，携票调用 `D` 推进到 `E`。`X-Flow-Next-Index` 从零开始，`1` 表示下一步为组合的第二步。

```http
POST /order/api/orders/42/a HTTP/1.1
Host: gateway.example.com
X-Mode: ADE
X-Business-Id: order-42
Content-Length: 0

HTTP/1.1 204 No Content
X-Flow-Ticket: <ticket-ade>
X-Flow-Next-Index: 1
Cache-Control: no-store

POST /order/api/orders/42/d HTTP/1.1
Host: gateway.example.com
X-Flow-Ticket: <ticket-ade>
X-Business-Id: order-42
Content-Length: 0

HTTP/1.1 204 No Content
X-Flow-Ticket: <ticket-ade>
X-Flow-Next-Index: 2
Cache-Control: no-store

POST /order/api/orders/42/e HTTP/1.1
Host: gateway.example.com
X-Flow-Ticket: <ticket-ade>
X-Business-Id: order-42
Content-Length: 0

HTTP/1.1 204 No Content
X-Flow-Completed: true
Cache-Control: no-store
```

`DE` 直接以无票请求调用 `D`。此时网关将 `D` 作为该组合首步，`E` 是紧随其后的末步。

```http
POST /order/api/orders/43/d HTTP/1.1
Host: gateway.example.com
X-Mode: DE
X-Business-Id: order-43
Content-Length: 0

HTTP/1.1 204 No Content
X-Flow-Ticket: <ticket-de>
X-Flow-Next-Index: 1
Cache-Control: no-store

POST /order/api/orders/43/e HTTP/1.1
Host: gateway.example.com
X-Mode: DE
X-Flow-Ticket: <ticket-de>
X-Business-Id: order-43
Content-Length: 0

HTTP/1.1 204 No Content
X-Flow-Completed: true
Cache-Control: no-store
```

### 请求头、票据生命周期与响应结算

| 请求头 | 解析规则 |
|---|---|
| `ParameterHeaderName` 指定的前置参数头 | 单值并执行 `Trim()`。参数化流程新开时，缺失、空白或多值均拒绝。持票推进时可省略，携带空白或多值则拒绝。 |
| `TicketHeaderName`（默认 `X-Flow-Ticket`） | 读取票据时多值取首个，仅检查是否非空白，保留原值。参数化流程先以整组头值转成字符串判断是否进入持票分支，再读取首个值。因此首值空白但其余值非空时，按持票分支返回 `FLOW_NOT_FOUND`。 |
| `BusinessIdHeaderName`（默认 `X-Business-Id`） | 多值取首个并执行 `Trim()`。首步可省略，也可直接绑定此值。推进时必填，已绑定的值按原样比较。 |

如果首步请求未绑定业务标识，下游可在任意 2xx 步骤的响应中通过 `X-Flow-Business-Id` 完成首次绑定。网关先从下游响应头读取首个值，再回退到内容头。已经绑定时，下游回传不同的非空值会导致结算失败。票据一直未绑定时，推进请求仍须提供业务标识，但 Redis 不比对该值，也不会用该值补写绑定。

| 场景 | 票据和状态 | 网关写入的头及响应 |
|---|---|---|
| 非末步 2xx | 提交当前步骤，推进顺位并释放锁，原 TTL 保持。 | 回写配置的票据头、`X-Flow-Next-Index`、`Cache-Control: no-store`。移除 `X-Flow-Business-Id`。 |
| 末步 2xx | 提交并标记完成，改用 `CompletedTtlSeconds`。 | 写入 `X-Flow-Completed: true` 和 `Cache-Control: no-store`。网关不回写票据或顺位。移除 `X-Flow-Business-Id`。 |
| 首步非 2xx（含 3xx） | 删除该次创建的票据。 | 转发下游响应，不生成流程成功头。移除 `X-Flow-Business-Id`。 |
| 后续步骤非 2xx（含 3xx） | 释放当前占用，保留票据和当前顺位。 | 转发下游响应，不生成流程成功头。移除 `X-Flow-Business-Id`。 |
| 403 流程拒绝 | 预检或步骤占用失败，请求未转发。 | `application/problem+json`，无流程成功头。 |
| 500 `FLOW_CONFIGURATION_ERROR` | 无法还原下游路径，请求未转发。 | `ProblemDetails` JSON，无流程成功头。当前写入方法的 Content-Type 为 `application/json; charset=utf-8`。 |
| 响应结算异常 | 提交、删除或释放抛出异常。中间件对未结算尝试进行兜底清理。 | 转换先设置 503，抑制下游响应体，移除 `X-Flow-Business-Id` 并重新抛出异常。YARP 2.3.0 在响应尚未发出时将转换异常处理为空体 502。 |
| 未命中流程防护 | 直接转发，不操作票据。 | 流程转换不改写响应，也不剥离下游的 `X-Flow-Business-Id`。 |

- 票据由 32 字节加密安全随机数经 Base64Url 编码生成，长 43 字符。有效期从创建时起绝对计时，中间步骤不续期。末步提交后重设为完成态保留期，保留期间重复推进返回 `FLOW_COMPLETED`，过期后返回 `FLOW_NOT_FOUND`。
- Redis 使用 `flow:ticket:{ticket}` 键，无 `InstanceName` 前缀。Lua 从 Redis `TIME` 获取时间，避免多网关实例的本地时钟漂移影响步骤占用。
- 代理管道退出时，未完成的结算会用独立的 5 秒取消令牌尝试清理。自定义日志可用 `FlowTicketService.GetTicketFingerprint` 记录 SHA-256 十六进制摘要的前 12 位。当前兜底清理失败警告记录原票据，应按敏感日志处理。

### 错误码与前端处理

以下是流程拒绝的 `ProblemDetails` 响应示例。`traceId` 取当前请求的 `HttpContext.TraceIdentifier`。

```json
{
  "type": "https://errors.example.com/flow/step-not-allowed",
  "title": "流程步骤不允许执行",
  "status": 403,
  "code": "FLOW_STEP_NOT_ALLOWED",
  "traceId": "0HNEXAMPLE:00000007"
}
```

| HTTP | `code` | `type` slug | 触发条件 |
|---|---|---|---|
| 403 | `FLOW_NOT_FOUND` | `not-found` | 票据首值缺失或空白、Redis 键不存在（包括 TTL 到期），或无票调用的接口并非所选组合的首步。 |
| 403 | `FLOW_EXPIRED` | `expired` | 错误映射项，内置 Redis 流程的键过期按 `FLOW_NOT_FOUND` 处理。 |
| 403 | `FLOW_BUSINESS_MISMATCH` | `business-mismatch` | 推进请求未提供非空业务标识，或首个值与已绑定值不一致。下游响应回传的值与绑定冲突属于结算异常。 |
| 403 | `FLOW_STEP_NOT_ALLOWED` | `step-not-allowed` | 票据属于其他流程、缺少已锁定组合、当前接口不在票据组合中、顺位不符，或存储返回的组合及顺位与接口定义不一致。未识别的存储拒绝也映射到此码。 |
| 403 | `FLOW_IN_PROGRESS` | `in-progress` | 同一步骤存在未到期的占用、状态不允许重新占用，或创建票据时键已存在。提交、释放和删除时 `AttemptId` 不匹配也使用此存储错误码，提交时还检查持锁状态。这些结算阶段的错误走响应转换异常分支。 |
| 403 | `FLOW_COMPLETED` | `completed` | 票据处于完成保留期，拒绝再次推进。 |
| 403 | `FLOW_SELECTOR_INVALID` | `selector-invalid` | 参数化流程新开时，前置参数缺失、空白、多值或未匹配到配置的组合。 |
| 403 | `FLOW_SELECTOR_MISMATCH` | `selector-mismatch` | 参数化流程持票推进时，携带的前置参数为空白、多值，或与票据锁定组合不同。 |
| 500 | `FLOW_CONFIGURATION_ERROR` | `configuration-error` | 代理路由缺少唯一可用的 `PathRemovePrefix`，或请求路径无法按段边界移除该前缀。启用后先对所有经过该中间件的代理请求检查此条件，再查流程匹配。 |

拒绝顺序分两层。参数化新开先校验前置参数格式及组合，再查当前接口是否为首步。持票推进先在中间件读取票据首值、检查业务标识必填，以及校验已携带的前置参数格式。随后 Redis 按票据存在性 → 完成态 → `flowId` → 已绑定业务标识 → 组合及前置参数一致性 → 顺位 → 锁的顺序判定。因此业务标识缺失或前置参数格式无效时，请求可能在查询 Redis 键之前被拒绝。

前端可按 `flowId + businessId` 在内存保存服务端回传的票据。推进时发送配置的票据头和业务标识头，收到完成标记后清除本地票据。遇到 `FLOW_IN_PROGRESS` 时等待当前请求结果，或在处理锁过期后按原步骤重试。票据不存在、已完成或业务标识不匹配时，清除失效票据并重新核对流程上下文。下游明确返回非 2xx 且结算成功时，可用原票据按原顺位重试后续步骤。代理出错且结算结果未知时，先核对下游业务结果，再按业务幂等约定决定是否重试。票据是持有者凭据，无 `userId` 或 `tenantId` 绑定，应避免写入 LocalStorage 和日志。

## 目录刷新、热更新与健康检查

`DiscoveryRefreshInterval`、`MaxStaleDuration`、`HealthCheckPath` 和 `LivenessCheckPath` 均在 `ModuleConfigurations:ServiceGovernanceConfig`。首次发现随宿主启动执行。每轮结束后等待 `DiscoveryRefreshInterval`（默认 30 秒，最短 1 秒）。刷新失败时记录警告，沿用最后一次成功快照，并在下一轮重试。

目录仅在快照内容变化时触发 `ServicesChanged`。`GirvsGatewayProxyConfigProvider` 在锁内构建并发布携带新 `ChangeToken` 的配置，然后取消旧令牌，YARP 随后通过 `GetConfig()` 获取新路由和集群。变更生效时间取决于轮询和发现查询耗时。

| 检查 | Girvs 默认路径 | 判定 |
|---|---|---|
| 就绪 | `/health` | 执行包括 `service-directory`（`ready` 标签）在内的健康检查。目录从未成功加载，或距上一次成功加载的时间严格大于 `MaxStaleDuration`（默认 90 秒，最短 1 秒）时，目录检查为 `Unhealthy`。成功加载空快照或相同内容也会更新成功时间。 |
| 存活 | `/alive` | 只执行 `live` 标签检查，包括宿主的 `self` 检查。目录陈旧只影响就绪检查，存活探针不会因此重启 Pod。 |

独立宿主可沿用前文示例映射两个端点。健康通过表示目录近期成功刷新，业务服务能否路由仍取决于准入和端点。

## Kubernetes 部署

将 `ServiceDiscoveryProvider` 设为 `Kubernetes`，并将网关部署在集群内。`InClusterConfig()` 读取 Pod 的 ServiceAccount 令牌及集群连接信息，集群外无法使用此发现提供程序。RBAC 的查询权限取决于 `KubernetesNamespace`。

| 发现范围 | API | RBAC |
|---|---|---|
| 全集群，`KubernetesNamespace` 为空或空白 | `ListServiceForAllNamespacesAsync` | `ClusterRole` 和 `ClusterRoleBinding`，授予核心 API 组的 `services` `list` 权限。 |
| 指定命名空间 | `ListNamespacedServiceAsync` | 在目标命名空间创建 `Role` 和 `RoleBinding`，授予同样的 `list` 权限。生产环境仅需发现一个命名空间时优先采用此方案。 |

以下是全集群发现的完整 RBAC。网关 Deployment 位于 `default` 命名空间，Pod spec 指定 `serviceAccountName: girvs-gateway`。

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
  apiGroup: rbac.authorization.k8s.io
  kind: ClusterRole
  name: girvs-gateway-services-reader
```

单命名空间部署可使用以下 RBAC，复用上面 `default` 中的 ServiceAccount，并将 `KubernetesNamespace` 配置为 `apps`。

```yaml
apiVersion: rbac.authorization.k8s.io/v1
kind: Role
metadata:
  name: girvs-gateway-services-reader
  namespace: apps
rules:
  - apiGroups: [""]
    resources: ["services"]
    verbs: ["list"]
---
apiVersion: rbac.authorization.k8s.io/v1
kind: RoleBinding
metadata:
  name: girvs-gateway-services-reader-binding
  namespace: apps
subjects:
  - kind: ServiceAccount
    name: girvs-gateway
    namespace: default
roleRef:
  apiGroup: rbac.authorization.k8s.io
  kind: Role
  name: girvs-gateway-services-reader
```

业务 Service 应满足 Label Selector，带启用注解并选定一个端点。`girvs.io/gateway-endpoint` 与端口名匹配，未命名端口使用 `port-{port}`。网关连接 Service 的 `port`，后端需通过该端口提供 HTTP。`protocol` 和 `appProtocol` 不会改变网关使用的协议。

```yaml
apiVersion: v1
kind: Service
metadata:
  name: echo-a
  namespace: apps
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
      protocol: TCP
      port: 5678
      targetPort: 5678
```

将 `readinessProbe` 指向包含 `service-directory` 的 `/health`，将 `livenessProbe` 指向只执行 `live` 检查的 `/alive`，或使用各自配置的路径。示例部署中直接返回 200 的 `/healthz` 无法反映目录是否已加载或陈旧。

## 常见问题与排错

| 现象 | 检查项 |
|---|---|
| 代理路径返回 404 | 确认首轮发现已成功，服务存在于快照中，`GatewayEnabled` 为真且 `GatewayEndpointName` 匹配。一并检查服务名前缀，以及服务是否已被刷新移除。 |
| 间歇性 502 | 检查下游协议、地址和端口，确认失效实例是否仍在发现快照中。 |
| 500 `FLOW_CONFIGURATION_ERROR` | 检查代理路由是否有唯一有效的 `PathRemovePrefix`，以及请求路径能否移除此完整段前缀。确认流程中间件位于 `MapReverseProxy` 内。 |
| 403 `FLOW_NOT_FOUND` | 确认请求有正确的票据头及实际票据，检查 Redis 键和 TTL。无票启动参数化流程时确认当前接口是所选组合的首步。 |
| 403 `FLOW_BUSINESS_MISMATCH` | 确认 `BusinessIdHeaderName` 实际配置、请求首值和票据绑定值。推进请求必须提供非空业务标识。 |
| 受保护请求返回空体 502 或 503 | 检查代理错误和流程结算日志。结算转换内部设置 503，但 YARP 2.3.0 会将重新抛出的转换异常处理为 502。核对 Redis 连接、占用状态及下游业务结果后再重试。 |
| `/health` 为 `Unhealthy` | 查找 `service-directory` 初次刷新或后续刷新失败的日志，比较距上次成功时间与 `MaxStaleDuration`。 |
| Kubernetes API 返回 `Forbidden` | 检查 Pod 的 ServiceAccount、`KubernetesNamespace` 及对应的 Role/ClusterRole 绑定。使用命名空间级 `Role` 时必须配置相同的目标命名空间。 |
| 浏览器读不到票据 | 核对实际 `TicketHeaderName` 已加入 CORS `WithExposedHeaders`。末步成功时读取 `X-Flow-Completed`。 |
| 需要确认 Redis 是否有票据 | 在网关使用的 Redis 数据库运行 `SCAN 0 MATCH flow:ticket:*`，按返回的游标继续查询至游标为 `0`。票据键无 `InstanceName` 前缀，扫描输出中的票据按敏感数据处理。 |

## 示例与部署选型

- [samples/Sample.Gateway/](../samples/Sample.Gateway/) 使用 Girvs 宿主和 Aspire 服务目录，集中 Swagger UI 位于 `/girvs_swagger`。
- [samples/gateway-k8s/](../samples/gateway-k8s/) 提供独立宿主和 Kubernetes 部署清单。`Gateway/appsettings.FlowProtection.json` 默认关闭流程防护。部署前按上文为业务 Service 补齐 Label、Annotation 和端口元数据，并映射健康检查以替换示例探针使用的 `/healthz`。启用流程防护时还须注册 Redis 连接封装。

| `ServiceDiscoveryProvider` | 适用场景 | 需要提供 |
|---|---|---|
| `Aspire` | AppHost 编排的本地开发和联调 | AppHost 注入的端点地址，以及网关配置中的服务准入元数据。 |
| `Consul` | 使用 Consul 维护服务注册及实例健康的环境 | Consul 地址、业务标签、各实例端点标签和准入标签。 |
| `Kubernetes` | 网关随业务服务部署在 Kubernetes 集群中 | ServiceAccount、Service 查询权限、筛选标签和准入注解。 |

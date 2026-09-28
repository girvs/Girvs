# 移除 Consul，统一为 Aspire 服务发现设计

## 背景

Girvs 新架构已经引入 `Girvs.Aspire`、`Girvs.Aspire.Hosting` 与 `Girvs.Gateway`。当前仓库仍保留 `Girvs.Consul`，样例 AppHost 也会在本地 Run 模式启动 Consul 容器，并让业务服务通过 `ConsulConfig` 注册服务。这会让框架同时暴露 Aspire 与 Consul 两套服务发现模型，使用者难以判断推荐路径。

本变更选择彻底移除 Consul 路线：本地开发、Docker 基础设施、网关路由与生产 K8s 都统一走 Aspire/K8s 模型。

## 目标

1. 从框架主线删除 `Girvs.Consul` 项目、包引用、样例引用、文档推荐与测试契约。
2. `Girvs.Gateway` 不再支持 `GatewayDiscoveryType.Consul`。
3. 本地 AppHost Run 模式下，网关从 Aspire 注入的服务端点配置生成 YARP 路由。
4. 生产 Publish/K8s 模式下，网关继续通过 K8s Service/EndpointSlice 与 Annotation 生成 YARP 路由。
5. 业务服务不再执行“注册到服务中心”的动作，只声明自身和依赖，由 AppHost/K8s 负责服务发现数据来源。

## 非目标

- 不保留 Consul 兼容开关。
- 不迁移旧 net8/net9 部署模型；此框架主线面向 Aspire 统一架构。
- 不引入 Nacos、Apollo 或其他配置中心。
- 不让网关硬编码样例服务地址。

## 架构决策

### 服务端

业务服务只引用 `Girvs.Aspire`。`AspireModule` 继续负责：

- 注册 `Microsoft.Extensions.ServiceDiscovery`；
- 为 `HttpClient` 默认启用服务发现与标准弹性策略；
- 暴露 `/health` 与 `/alive`；
- 在 Aspire 环境下接入 OpenTelemetry。

删除 `AspireModule` 中对 `Girvs.Consul` 共存的检测与告警，因为新架构不再存在合法共存状态。

### AppHost

`Girvs.Aspire.Hosting` 继续作为唯一编排入口。`AddGirvsProject` 需要承载两类信息：

- 资源依赖：现有 `GIRVS_SHARED_CONFIG` 与 `Resources` 机制不变；
- 网关元数据：服务名、网关归属、路由前缀、Swagger/OpenAPI 路径等。

本地 Run 模式下，AppHost 不再启动 Consul 容器。网关项目通过 `.WithReference(serviceA)`、`.WithReference(serviceB)` 接收 Aspire 注入的 `services:{serviceName}:...` 端点配置；同时通过共享配置或环境变量接收网关路由元数据。

生产 Publish/K8s 模式下，`AddGirvsProject` 继续把网关元数据写到 K8s Service Annotation，供 K8s 发现源读取。

### Gateway

`GatewayDiscoveryType` 调整为：

```csharp
public enum GatewayDiscoveryType
{
    Aspire,
    Kubernetes
}
```

默认值改为 `Aspire`，服务于本地 AppHost Run 模式。

新增 `AspireGatewayServiceSource`：

- 从 `IConfiguration` 读取 Aspire 注入的服务发现配置，例如 `services:{serviceName}:{endpointName}:{index}`；
- 结合网关元数据筛选哪些服务暴露到当前网关；
- 生成 `GatewayServiceEndpoint` 快照；
- 由于本地 AppHost 端点变化主要发生在启动阶段，首版只需启动时构建快照；后续如 Aspire 配置源支持变更令牌，再补热更新。

保留 `KubernetesGatewayServiceSource`：

- 生产 K8s 下继续 watch Service/EndpointSlice；
- 读取 Service Annotation 生成路由；
- 不依赖 Consul。

删除 `ConsulGatewayServiceSource`、`IConsulClient` 注册、`Consul` NuGet 包引用与相关测试。

## 数据流

### 本地 Run 模式

1. 开发者运行 AppHost。
2. AppHost 启动业务服务与 Docker 基础设施。
3. AppHost 通过 `.WithReference(...)` 给网关注入服务端点配置。
4. AppHost 给网关注入网关元数据配置。
5. `AspireGatewayServiceSource.StartAsync` 读取配置并生成服务快照。
6. `GirvsGatewayProxyConfigProvider` 根据服务快照生成 YARP Route/Cluster。

### 生产 K8s 模式

1. `aspire publish` 生成 K8s Deployment/Service/ConfigMap/Secret。
2. 业务服务通过 K8s Service 暴露集群内地址。
3. AppHost 发布逻辑把网关元数据写入 Service Annotation。
4. 网关使用 `KubernetesGatewayServiceSource` watch Service/EndpointSlice。
5. 服务增删或端点变化后，YARP 配置通过变更令牌热更新。

## 影响范围

- `Girvs.slnx`：移除 `Girvs.Consul` 项目。
- `Girvs.Consul/`：删除项目目录。
- `Girvs.Aspire/`：移除 Consul 共存告警。
- `Girvs.Gateway/`：删除 Consul 发现源，新增 Aspire 发现源。
- `Girvs.Aspire.Hosting/`：补齐本地网关元数据注入能力。
- `samples/`：移除 Consul 容器、`ConsulConfig`、服务项目中的 `Girvs.Consul` 引用。
- `tests/`：删除 Consul 发现测试，新增 Aspire 本地发现、K8s 发现与样例契约测试。
- `docs/aspire/` 与根文档：删除“net8/net9 继续使用 Girvs.Consul”“迁移期间可忽略共存告警”等描述，改为纯 Aspire 路线。

## 错误处理

- `GatewayDiscoveryType.Aspire` 下，如果网关配置声明了服务但 Aspire 没有注入对应端点，启动发现源时记录错误并跳过该服务；如果当前网关没有任何可用服务，记录警告但允许启动。
- `GatewayDiscoveryType.Kubernetes` 下维持现有 watch/relist 错误处理。
- 配置中仍出现 `Consul` 枚举值、`ConsulAddress` 或 `ConsulConfig` 时不再兼容，测试覆盖确保样例和文档不再引用。

## 测试策略

1. `Girvs.Gateway.Tests`：
   - `AddGirvsGateway_默认使用Aspire发现源`；
   - `AddGirvsGateway_使用Kubernetes发现源_注册K8s发现源`；
   - `AspireGatewayServiceSource_读取Aspire注入端点_生成服务快照`；
   - `GatewayDiscoveryConfig_不再暴露Consul配置`。
2. `Girvs.Aspire.Hosting.Tests`：
   - AppHost 样例契约不包含 Consul 容器和 `ConsulConfig`；
   - 网关本地 Run 模式包含 Aspire 发现配置；
   - Publish 模式仍设置 Kubernetes 发现。
3. 构建验证：
   - `dotnet build Girvs.slnx --no-restore --nologo`；
   - `dotnet test tests/Girvs.Gateway.Tests/Girvs.Gateway.Tests.csproj --no-restore --nologo`；
   - `dotnet test tests/Girvs.Aspire.Hosting.Tests/Girvs.Aspire.Hosting.Tests.csproj --no-restore --nologo`。

## 迁移结果

完成后，Girvs 框架只保留一种推荐服务发现模型：

- 本地：Aspire AppHost + Docker 基础设施 + Aspire 注入端点；
- 生产：Aspire 发布到 K8s + K8s 原生 Service/EndpointSlice；
- 网关：本地读取 Aspire 端点配置，生产读取 K8s 服务资源；
- Consul：从框架主线删除。

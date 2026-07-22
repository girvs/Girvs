# Girvs.Refit 服务发现兼容设计

## 背景

`Girvs.Refit` 当前通过接口特性中的 `InConsul` 参数决定是否查询 Consul。该方式将部署拓扑写入调用接口，并且 Refit 的两种调用方式没有共享 `HttpClientFactory` 的服务发现、弹性和请求处理能力。

目标是在部署时由全局配置选择历史 Consul 或现代 .NET 服务发现。后者由 `Girvs.Aspire` 已注册的 `AddServiceDiscovery()` 处理，兼容 Aspire 本地编排和 Kubernetes DNS。

## 范围

- 支持 `Consul` 与 `ServiceDiscovery` 两种全局发现模式。
- 保持现有 Refit 接口和 `RefitServiceAttribute` 的源代码兼容。
- 保留固定地址调用能力，作为单服务端点覆盖而非第三种发现模式。
- 统一构造器注入和 `IEngine.RestServiceAsync<T>()` 的客户端创建路径。

不在本次变更中修改 `Girvs.Aspire`、Kubernetes 服务发现或 Consul 的服务注册逻辑。

## 配置契约

`RefitConfig` 新增或规范化以下配置：

```json
{
  "RefitConfig": {
    "DiscoveryType": "ServiceDiscovery",
    "ConsulAddress": "http://127.0.0.1:8500",
    "ServiceEndpoints": {
      "partner-api": "https://partner.example.com/api"
    }
  }
}
```

- `DiscoveryType` 为 `Consul` 或 `ServiceDiscovery`，决定没有端点覆盖的逻辑服务如何解析。
- `ConsulAddress` 仅在 `Consul` 模式生效。
- `ServiceEndpoints` 使用逻辑服务名作为键，值必须为完整根地址。其优先级最高，适用于第三方或迁移期服务。
- 保留历史 `ServiceAddress` 与 `ConsulServiceHost` 的读取兼容；新配置使用 `ServiceEndpoints` 和 `ConsulAddress`。

## 接口与调用流程

`RefitServiceAttribute` 只表示逻辑服务名。保留构造器的第二个 `bool` 参数，标记为过时兼容参数；运行时不得依据该参数选择发现模式。

请求地址优先级如下：

1. `ServiceEndpoints[serviceName]`，直接作为请求根地址。
2. `DiscoveryType = Consul`，在每次请求前查询健康实例并选择一个地址。
3. `DiscoveryType = ServiceDiscovery`，使用 `http://{serviceName}` 作为基地址，由 `HttpClientFactory` 服务发现处理器解析为 Aspire 注入端点或 Kubernetes Service DNS。

`RefitModule` 注册的 Refit 客户端和 `IEngine.RestServiceAsync<T>()` 都必须从依赖注入容器取得同一接口实例，确保共享 `HttpClientFactory` 的服务发现、弹性、认证请求处理器和可观测性。

在 Aspire 编排部署中，`Girvs.Aspire` 已通过 `ConfigureHttpClientDefaults()` 为所有 `HttpClient` 添加 `AddServiceDiscovery()`。普通命名 `HttpClient` 可以不设置 `BaseAddress`，并在调用时使用 `http://{serviceName}/path` 形式的绝对逻辑服务地址。Refit 接口通常以相对路径声明 API，因此 Refit 注册期必须将 `BaseAddress` 设为 `http://{serviceName}`；请求仍由同一个服务发现处理器解析，接口调用方无需改写为完整 URL。该模式要求应用加载 `Girvs.Aspire` 模块。

Consul 专用处理器只在 Consul 模式且没有固定端点覆盖时查询实例、改写主机地址，并透传当前请求头。无法发现健康实例时抛出包含服务名和发现模式的 `GirvsException`。日志使用结构化模板，避免记录不必要的敏感请求头。

## 测试

增加 `Girvs.Refit.Tests`，覆盖：

- 固定端点优先于全局发现模式。
- Consul 模式的地址解析与无健康实例错误。
- `ServiceDiscovery` 模式使用逻辑服务地址 `http://{serviceName}`。
- 历史 `inConsul` 参数不影响全局 `DiscoveryType`。
- 缺少必要端点配置时的明确错误。
- `RestServiceAsync<T>()` 从依赖注入获取已注册的 Refit 客户端。

## 兼容性与迁移

现有接口无需删除 `inConsul` 参数；升级后该参数不再有运行时影响。现有固定服务地址可继续从 `ServiceAddress` 读取，建议逐步迁移到 `ServiceEndpoints`。历史 Consul 部署配置显式设置 `DiscoveryType: Consul`，Aspire 或 Kubernetes 部署设置 `DiscoveryType: ServiceDiscovery`。

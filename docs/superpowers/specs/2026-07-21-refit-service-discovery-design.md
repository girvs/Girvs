# Girvs.Refit 服务发现兼容设计

## 背景

`Girvs.Refit` 当前通过接口特性中的 `InConsul` 参数决定是否查询 Consul。该方式将部署拓扑写入调用接口，并且 Refit 的两种调用方式没有共享 `HttpClientFactory` 的服务发现、弹性和请求处理能力。

目标是将接口地址来源分为固定远程地址和内部服务发现两类。内部服务发现由部署配置选择历史 Consul 或现代 Aspire；后者由 `Girvs.Aspire` 已注册的 `AddServiceDiscovery()` 处理，兼容 Aspire 本地编排和 Kubernetes DNS。

## 范围

- 支持静态地址与内部服务发现两种接口地址来源。
- 支持在不修改业务接口或调用代码的前提下，通过部署配置将内部服务发现切换为 `Consul` 或 `Aspire`。
- 保持现有 Refit 接口和 `RefitServiceAttribute` 的源代码兼容。
- 将固定地址建模为接口级地址来源，而非内部服务发现的第三种提供者。
- 统一构造器注入和 `IEngine.RestServiceAsync<T>()` 的客户端创建路径。

不在本次变更中修改 `Girvs.Aspire`、Kubernetes 服务发现或 Consul 的服务注册逻辑。

## 配置契约

`RefitConfig` 新增或规范化以下配置：

```json
{
  "RefitConfig": {
    "DiscoveryProvider": "Aspire",
    "ConsulAddress": "http://127.0.0.1:8500",
    "ServiceEndpoints": {
      "partner-api": "https://partner.example.com/api"
    }
  }
}
```

- `DiscoveryProvider` 为 `Consul` 或 `Aspire`，仅决定内部服务发现类型为 `ServiceDiscovery` 的接口如何解析。
- `ConsulAddress` 仅在 `DiscoveryProvider = Consul` 时生效。
- `ServiceEndpoints` 使用逻辑服务名作为键，值必须为完整根地址，供地址来源为 `Static` 的接口使用。
- 保留历史 `ServiceAddress` 与 `ConsulServiceHost` 的读取兼容；新配置使用 `ServiceEndpoints` 和 `ConsulAddress`。

## 接口与调用流程

`RefitServiceAttribute` 表示逻辑服务名和接口地址来源：

```csharp
public enum RefitServiceAddressType
{
    Static,
    ServiceDiscovery
}

public enum RefitDiscoveryProvider
{
    Consul,
    Aspire
}
```

新接口通过 `RefitServiceAddressType` 指定固定远程 API 或内部服务发现。保留构造器的历史 `bool inConsul` 参数并标记为过时兼容参数：`true` 映射为 `ServiceDiscovery`，`false` 映射为 `Static`。该参数不再决定 Consul 或 Aspire；内部发现提供者只由部署配置中的 `DiscoveryProvider` 决定。

请求地址流程如下：

1. `RefitServiceAddressType = Static`：从 `ServiceEndpoints[serviceName]` 取得请求根地址；缺失配置时报告明确错误。
2. `RefitServiceAddressType = ServiceDiscovery` 且 `DiscoveryProvider = Consul`：在每次请求前查询健康实例并选择一个地址。
3. `RefitServiceAddressType = ServiceDiscovery` 且 `DiscoveryProvider = Aspire`：使用 `http://{serviceName}` 作为基地址，由 `HttpClientFactory` 服务发现处理器解析为 Aspire 注入端点或 Kubernetes Service DNS。

`Girvs.Refit` 定义内部服务端点解析契约：

```csharp
public interface IRefitServiceEndpointResolver
{
    bool CanResolve(RefitServiceAddressType addressType);

    Task<Uri?> ResolveAsync(
        string serviceName,
        RefitServiceAddressType addressType,
        CancellationToken cancellationToken);
}
```

- `StaticRefitServiceEndpointResolver`：为 `Static` 接口从 `ServiceEndpoints` 返回固定地址。
- `ConsulRefitServiceEndpointResolver`：为 `ServiceDiscovery` 接口查询 Consul 健康实例并返回实际服务地址。
- `AspireRefitServiceEndpointResolver`：为 `ServiceDiscovery` 接口返回 `null`，表示保留逻辑服务地址并交给 .NET 服务发现。
- `RefitServiceEndpointHandler`：从已注册的解析器中选择 `CanResolve()` 为真的实现，透传当前请求头并调用它；有返回地址时替换请求主机，无返回地址时保留原请求地址。
- `RefitModule` 始终注册静态地址解析器，并根据全局 `DiscoveryProvider` 注册一个内部服务发现解析器；为 Refit 客户端设置逻辑基地址 `http://{serviceName}`。

`RefitModule` 注册的 Refit 客户端和 `IEngine.RestServiceAsync<T>()` 都必须从依赖注入容器取得同一接口实例，确保共享 `HttpClientFactory` 的服务发现、弹性、认证请求处理器和可观测性。业务接口、`RefitServiceAttribute`、构造器注入和 `RestServiceAsync<T>()` 调用代码均不因 Consul、Aspire 或 Kubernetes 部署方式变化而修改。

在 Aspire 编排部署中，`Girvs.Aspire` 已通过 `ConfigureHttpClientDefaults()` 为所有 `HttpClient` 添加 `AddServiceDiscovery()`。普通命名 `HttpClient` 可以不设置 `BaseAddress`，并在调用时使用 `http://{serviceName}/path` 形式的绝对逻辑服务地址。Refit 接口通常以相对路径声明 API，因此 Refit 注册期必须将 `BaseAddress` 设为 `http://{serviceName}`；请求仍由同一个服务发现处理器解析，接口调用方无需改写为完整 URL。该模式要求应用加载 `Girvs.Aspire` 模块。

Consul 解析器无法发现健康实例时抛出包含服务名和发现模式的 `GirvsException`。日志使用结构化模板，避免记录不必要的敏感请求头。

## 测试

增加 `Girvs.Refit.Tests`，覆盖：

- 静态接口从 `ServiceEndpoints` 取得地址，且缺失配置时失败。
- 根据 `DiscoveryProvider` 注册正确的内部服务端点解析器实现。
- Consul 模式的地址解析与无健康实例错误。
- Aspire 模式的内部服务使用逻辑服务地址 `http://{serviceName}`。
- 历史 `inConsul` 参数映射为地址来源，但不影响全局 `DiscoveryProvider`。
- `RestServiceAsync<T>()` 从依赖注入获取已注册的 Refit 客户端。
- 业务 Refit 接口与调用代码不依赖具体发现实现。

## 兼容性与迁移

现有接口无需立即删除 `inConsul` 参数；升级后它仅映射接口地址来源，不再选择内部服务发现提供者。现有固定服务地址可继续从 `ServiceAddress` 读取，建议逐步迁移到 `ServiceEndpoints`。历史 Consul 部署配置显式设置 `DiscoveryProvider: Consul`，Aspire 或 Kubernetes 部署设置 `DiscoveryProvider: Aspire`。切换这两个部署配置不会修改业务接口或调用代码。

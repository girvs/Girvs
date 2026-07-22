# Girvs.Refit 服务发现兼容实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refit 接口可声明静态地址或内部服务发现，并在不改业务代码的前提下通过配置切换 Consul 与 Aspire。

**Architecture:** 接口特性声明 `Static` / `ServiceDiscovery` 地址来源。请求处理器选择端点解析器：静态解析器读取配置，Consul 解析器查询健康实例，Aspire 解析器保留逻辑 URI 交给 `AddServiceDiscovery()`。

**Tech Stack:** .NET 8/9/10、Refit、`IHttpClientFactory`、NConsul、Microsoft.Extensions.ServiceDiscovery、xUnit。

## 全局约束

- 保持 `Girvs.Refit` 的 `net8.0;net9.0;net10.0` 多目标编译。
- 静态接口只能从 `ServiceEndpoints`（兼容读取 `ServiceAddress`）取得绝对地址。
- 内部发现提供者只由 `RefitConfig.DiscoveryProvider` 决定；业务 Refit 接口和调用点不得据此分支。
- Aspire 解析器不得解析网络地址，必须保留 `http://{serviceName}` 给 `Girvs.Aspire` 注册的 `AddServiceDiscovery()`。
- 日志使用结构化模板，且不得记录认证请求头。

---

## 文件结构

- 修改：`Girvs.Refit/Configuration/RefitConfig.cs`、`RefitServiceAttribute.cs`、`RefitModule.cs`、`HttpClientHandlers/AuthenticatedHttpClientHandler.cs`、`Extensions/IEngineExtensions.cs`、`GlobalUsings.cs`、`Girvs.Refit.csproj`。
- 新建：`Girvs.Refit/Discovery/IRefitServiceEndpointResolver.cs`、`StaticRefitServiceEndpointResolver.cs`、`ConsulRefitServiceEndpointResolver.cs`、`AspireRefitServiceEndpointResolver.cs`。
- 新建：`tests/Girvs.Refit.Tests/` 测试项目及配置、特性、解析器、模块、扩展测试。
- 修改：`Girvs.slnx`，加入 `Girvs.Refit.Tests`。

### Task 1: 定义配置和接口地址来源

**Files:**
- Modify: `Girvs.Refit/Configuration/RefitConfig.cs`
- Modify: `Girvs.Refit/RefitServiceAttribute.cs`
- Create: `tests/Girvs.Refit.Tests/Girvs.Refit.Tests.csproj`
- Create: `tests/Girvs.Refit.Tests/GlobalUsings.cs`
- Create: `tests/Girvs.Refit.Tests/RefitConfigTests.cs`
- Create: `tests/Girvs.Refit.Tests/RefitServiceAttributeTests.cs`

**Produces:** `RefitDiscoveryProvider { Consul, Aspire }`、`RefitServiceAddressType { Static, ServiceDiscovery }`、配置的 `GetServiceEndpoint(string)`、新枚举构造器和旧 `bool inConsul` 兼容构造器。

- [ ] **Step 1: 创建测试项目并还原依赖**

创建 `net10.0` 的不可打包测试项目，引用 `Microsoft.AspNetCore.App`、`Microsoft.NET.Test.Sdk 18.7.0`、`xunit 2.9.3`、`xunit.runner.visualstudio 3.1.5` 及 `../../Girvs.Refit/Girvs.Refit.csproj`；在 `GlobalUsings.cs` 导入 `Girvs`、`Girvs.Configuration`、`Girvs.Infrastructure`、`Girvs.Refit`、`Girvs.Refit.Configuration`、`Microsoft.Extensions.DependencyInjection` 与 `Xunit`。

Run: `dotnet restore tests/Girvs.Refit.Tests/Girvs.Refit.Tests.csproj --nologo`

Expected: 还原成功。

- [ ] **Step 2: 创建失败测试**

```csharp
[Theory]
[InlineData(true, RefitServiceAddressType.ServiceDiscovery)]
[InlineData(false, RefitServiceAddressType.Static)]
public void 历史inConsul参数映射为地址来源(bool inConsul, RefitServiceAddressType expected)
{
    Assert.Equal(expected, new RefitServiceAttribute("orders", inConsul).AddressType);
}

[Fact]
public void 新旧静态端点配置均可读取()
{
    var config = new RefitConfig
    {
        ServiceEndpoints = new() { ["new"] = "https://new.example" },
        ServiceAddress = new() { ["old"] = "https://old.example" }
    };
    Assert.Equal("https://new.example", config.GetServiceEndpoint("new"));
    Assert.Equal("https://old.example", config.GetServiceEndpoint("old"));
}
```

- [ ] **Step 3: 确认测试失败**

Run: `dotnet test tests/Girvs.Refit.Tests/Girvs.Refit.Tests.csproj --no-restore --filter "FullyQualifiedName~RefitConfigTests|FullyQualifiedName~RefitServiceAttributeTests" --nologo`

Expected: 编译失败，缺少枚举、`AddressType` 或 `GetServiceEndpoint`。

- [ ] **Step 4: 实现最小契约**

在 `RefitConfig` 添加：

```csharp
public RefitDiscoveryProvider DiscoveryProvider { get; set; } = RefitDiscoveryProvider.Aspire;
public string ConsulAddress { get; set; } = "http://127.0.0.1:8500";
public Dictionary<string, string> ServiceEndpoints { get; set; } = new();
public Dictionary<string, string> ServiceAddress { get; set; } = new();

public string GetServiceEndpoint(string serviceName) =>
    ServiceEndpoints.TryGetValue(serviceName, out var endpoint)
        ? endpoint
        : ServiceAddress.TryGetValue(serviceName, out endpoint) ? endpoint : null;
```

新增枚举。特性的新构造器为 `RefitServiceAttribute(string, RefitServiceAddressType = RefitServiceAddressType.ServiceDiscovery)`；旧 `bool` 构造器标记 `[Obsolete]`，将 `true` 映射为 `ServiceDiscovery`、`false` 映射为 `Static`。

- [ ] **Step 5: 验证并提交**

Run: `dotnet test tests/Girvs.Refit.Tests/Girvs.Refit.Tests.csproj --no-restore --filter "FullyQualifiedName~RefitConfigTests|FullyQualifiedName~RefitServiceAttributeTests" --nologo`

Expected: 所有测试通过。

```bash
git add Girvs.Refit/Configuration/RefitConfig.cs Girvs.Refit/RefitServiceAttribute.cs tests/Girvs.Refit.Tests
git commit -m "feat: 定义 Refit 地址来源配置"
```

### Task 2: 实现端点解析器

**Files:**
- Create: `Girvs.Refit/Discovery/IRefitServiceEndpointResolver.cs`
- Create: `Girvs.Refit/Discovery/StaticRefitServiceEndpointResolver.cs`
- Create: `Girvs.Refit/Discovery/ConsulRefitServiceEndpointResolver.cs`
- Create: `Girvs.Refit/Discovery/AspireRefitServiceEndpointResolver.cs`
- Create: `tests/Girvs.Refit.Tests/ServiceEndpointResolverTests.cs`

**Produces:**

```csharp
public interface IRefitServiceEndpointResolver
{
    bool CanResolve(RefitServiceAddressType addressType);
    Task<Uri> ResolveAsync(string serviceName, CancellationToken cancellationToken);
}
```

- [ ] **Step 1: 创建失败测试**

```csharp
[Fact]
public async Task 静态解析器返回配置端点()
{
    var resolver = new StaticRefitServiceEndpointResolver(new RefitConfig
    {
        ServiceEndpoints = new() { ["partner"] = "https://partner.example/api" }
    });

    Assert.Equal(new Uri("https://partner.example/api"),
        await resolver.ResolveAsync("partner", CancellationToken.None));
}

[Fact]
public async Task Aspire解析器返回空以保留逻辑服务地址()
{
    var resolver = new AspireRefitServiceEndpointResolver();

    Assert.Null(await resolver.ResolveAsync("ordersservice", CancellationToken.None));
}
```

补充：静态地址缺失抛出 `GirvsException`；Consul 无健康实例抛出含服务名的 `GirvsException`；静态解析器仅接受 `Static`，两个内部发现解析器仅接受 `ServiceDiscovery`。

- [ ] **Step 2: 确认失败并实现**

Run: `dotnet test tests/Girvs.Refit.Tests/Girvs.Refit.Tests.csproj --no-restore --filter FullyQualifiedName~ServiceEndpointResolverTests --nologo`

Expected: 编译失败，缺少解析器类型。

实现规则：
- 静态解析器从 `GetServiceEndpoint` 创建绝对 `Uri`；缺失或无效时抛出 `GirvsException("Refit 服务 {ServiceName} 未配置静态请求地址")`。
- Consul 解析器每次查询 `Health.Service(serviceName, string.Empty, true)`，以 `Random.Shared` 选择健康实例并返回 `http://{address}:{port}`；无实例抛出 `GirvsException("Refit 服务 {ServiceName} 在 Consul 中不存在健康实例")`。
- 为 Consul 查询引入内部客户端适配接口，测试使用假实现，禁止单元测试连接真实 Consul。
- Aspire 解析器只返回 `null`，不访问网络。

- [ ] **Step 3: 验证并提交**

Run: `dotnet test tests/Girvs.Refit.Tests/Girvs.Refit.Tests.csproj --no-restore --filter FullyQualifiedName~ServiceEndpointResolverTests --nologo`

Expected: 静态、Aspire、Consul 成功和失败路径全部通过。

```bash
git add Girvs.Refit/Discovery tests/Girvs.Refit.Tests/ServiceEndpointResolverTests.cs
git commit -m "feat: 新增 Refit 服务端点解析器"
```

### Task 3: 接入 Refit 注册与请求处理器

**Files:**
- Modify: `Girvs.Refit/HttpClientHandlers/AuthenticatedHttpClientHandler.cs`
- Modify: `Girvs.Refit/RefitModule.cs`
- Modify: `Girvs.Refit/GlobalUsings.cs`
- Modify: `Girvs.Refit/Girvs.Refit.csproj`
- Create: `tests/Girvs.Refit.Tests/RefitModuleTests.cs`

**Consumes:** `IEnumerable<IRefitServiceEndpointResolver>`、`RefitServiceAttribute.AddressType`。

- [ ] **Step 1: 创建失败测试**

以带 `[RefitService("ordersservice", RefitServiceAddressType.ServiceDiscovery)]` 的测试接口调用 `RefitModule.ConfigureServices`。断言永远注册静态解析器；当 `DiscoveryProvider=Consul` 时注册 Consul 解析器，`Aspire` 时注册 Aspire 解析器。使用记录请求 URI 的主处理器断言静态调用变为 `https://partner.example/api/orders/1`，Aspire 调用保留 `http://ordersservice/orders/1`。

- [ ] **Step 2: 确认失败**

Run: `dotnet test tests/Girvs.Refit.Tests/Girvs.Refit.Tests.csproj --no-restore --filter FullyQualifiedName~RefitModuleTests --nologo`

Expected: 旧处理器仍以 `InConsul` 分支处理，断言失败。

- [ ] **Step 3: 最小实现**

处理器构造器接收接口特性、`IEnumerable<IRefitServiceEndpointResolver>` 与 `ILogger<AuthenticatedHttpClientHandler>`。以 `CanResolve(attribute.AddressType)` 选择唯一解析器；返回非空 URI 时使用 `UriBuilder` 只替换 scheme、host、port，保留路径和查询；空 URI 保留逻辑地址。透传当前请求头时跳过 `Host`、`Content-Length` 等受限头，使用结构化日志。

模块注册静态解析器和由 `DiscoveryProvider` 选择的一个内部解析器；Refit 客户端统一设置逻辑基地址：

```csharp
.ConfigureHttpClient(client =>
    client.BaseAddress = new Uri($"http://{refitService.ServiceName}"))
.AddHttpMessageHandler(provider =>
    ActivatorUtilities.CreateInstance<AuthenticatedHttpClientHandler>(provider, refitService));
```

在三个目标框架条件包组中加入兼容的 `Microsoft.Extensions.ServiceDiscovery`。Aspire 宿主继续通过 `Girvs.Aspire` 的全局默认 HttpClient 配置启用服务发现。

- [ ] **Step 4: 验证并提交**

Run: `dotnet test tests/Girvs.Refit.Tests/Girvs.Refit.Tests.csproj --no-restore --filter FullyQualifiedName~RefitModuleTests --nologo`

Expected: 解析器注册和静态/Aspire 地址行为通过。

```bash
git add Girvs.Refit/HttpClientHandlers/AuthenticatedHttpClientHandler.cs Girvs.Refit/RefitModule.cs Girvs.Refit/GlobalUsings.cs Girvs.Refit/Girvs.Refit.csproj tests/Girvs.Refit.Tests/RefitModuleTests.cs
git commit -m "feat: Refit 接入可切换服务发现"
```

### Task 4: 统一 IEngine 调用路径与回归验证

**Files:**
- Modify: `Girvs.Refit/Extensions/IEngineExtensions.cs`
- Create: `tests/Girvs.Refit.Tests/IEngineExtensionsTests.cs`
- Modify: `Girvs.slnx`

- [ ] **Step 1: 创建失败测试**

使用最小 `IEngine` 测试替身返回预注册的 Refit 接口实例，断言 `RestServiceAsync<T>()` 返回同一实例；未注册时断言抛出 `GirvsException("未注册 Refit 客户端 {InterfaceName}")`。

- [ ] **Step 2: 确认失败并实现**

Run: `dotnet test tests/Girvs.Refit.Tests/Girvs.Refit.Tests.csproj --no-restore --filter FullyQualifiedName~IEngineExtensionsTests --nologo`

Expected: 旧实现创建独立 `RestService.For<T>` 或查询 Consul，测试失败。

实现：

```csharp
public static Task<T> RestServiceAsync<T>(this IEngine engine) where T : class
{
    var client = engine.Resolve<T>();
    return client is null
        ? Task.FromException<T>(new GirvsException($"未注册 Refit 客户端 {typeof(T).Name}"))
        : Task.FromResult(client);
}
```

保留同步方法作为异步方法的兼容包装，删除扩展类中全部 Consul 查询和 `RestService.For<T>`。

- [ ] **Step 3: 加入解决方案、验证并提交**

将 `tests/Girvs.Refit.Tests/Girvs.Refit.Tests.csproj` 加入 `Girvs.slnx`。

Run: `dotnet test tests/Girvs.Refit.Tests/Girvs.Refit.Tests.csproj --no-restore --nologo`

Expected: 全部 Refit 测试通过。

Run: `dotnet build Girvs.Refit/Girvs.Refit.csproj --no-restore --nologo`

Expected: `net8.0`、`net9.0`、`net10.0` 全部成功，零错误。

Run: `git diff --check`

Expected: 无输出。

```bash
git add Girvs.Refit/Extensions/IEngineExtensions.cs Girvs.slnx tests/Girvs.Refit.Tests/IEngineExtensionsTests.cs
git commit -m "refactor: 统一 Refit 客户端调用路径"
```

## 计划自检

- 覆盖：Task 1 实现配置和兼容特性，Task 2 实现三种解析策略，Task 3 接入 Refit/HttpClientFactory，Task 4 统一调用路径并验证三目标框架。
- 一致性：地址来源统一为 `RefitServiceAddressType`，内部发现提供者统一为 `RefitDiscoveryProvider`，所有解析器统一实现 `IRefitServiceEndpointResolver`。

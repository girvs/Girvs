# Girvs.Aspire.Gateway 实施计划（计划 2）

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 新增 `Girvs.Aspire.Gateway` 包，把分散在两个 YarpGateway 里的服务发现 + YARP 路由生成逻辑提取为可复用组件；提供可插拔服务发现抽象——K8s 源从 30 秒轮询升级为 watch 事件驱动，Consul 源保留给非 K8s（CentOS/docker）部署；修掉现有变更令牌未接通的 bug。

**Architecture:** `IGatewayServiceDiscoverySource`（抽象，产出服务端点快照 + `ServicesChanged` 事件）有两个实现——`ConsulGatewayServiceSource`（30s 轮询 Consul Agent.Services）与 `KubernetesGatewayServiceSource`（K8s watch + relist/重连）。`GirvsGatewayProxyConfigProvider : IProxyConfigProvider` 消费选定 source，按现有约定（服务名 → `/{name}/{**catch-all}` + `PathRemovePrefix`）生成 YARP 路由，并**正确接通 `CancellationChangeToken`** 通知 YARP 热重载。`AddGirvsGateway()` 按 `ServiceDiscoveryType` 选择 source 并注册 Provider + 反向代理。验证：K8s watch 源在本地 kind 集群真跑通，其余逻辑单测覆盖。

**Tech Stack:** .NET 10、`Yarp.ReverseProxy`、`KubernetesClient`（`k8s` 包）、Consul（`IConsulClient`）、xUnit 2.9.3；本地验证用 kind（K8s in docker）。

## Global Constraints

- 新包 `Girvs.Aspire.Gateway`：`<TargetFramework>net10.0</TargetFramework>`（单一，覆盖根 props 复数 `TargetFrameworks`，写法同 `Girvs.Aspire`）；测试工程 `tests/Girvs.Aspire.Gateway.Tests` 亦 net10.0、`IsPackable=false`。
- 依赖：`Yarp.ReverseProxy`、`KubernetesClient`、`Consul`；`ProjectReference` 引 `..\Girvs\Girvs.csproj`（用 `IAppModuleConfig`/`IAppModuleStartup`/`EngineContext`/`Singleton<AppSettings>`）。KubernetesClient 与 Consul 版本对齐现有 `ZhuoFan.Wb.YarpGateway.csproj`（实现时核对该 csproj 的 `<PackageReference>` 版本）。
- **保留现有行为，不得改变**：① 约定路由——每个服务名生成 `RouteConfig{ RouteId=ClusterId=服务名, Match.Path="/{服务名}/{**catch-all}", Transforms=[{PathRemovePrefix:服务名}] }` + `ClusterConfig{ ClusterId=服务名, Destinations }`；② YARP 转换——`AddOriginalHost(false)` + `CopyRequestHeaders=true` + `AddXForwarded(Append)` + `AddXForwardedFor("X-Forwarded-For", Append)`；③ 不读 Tag/Meta/Annotation，不引入 Label 分流（分流仍在网关侧 `RequestFilterMiddleware`，不进本包）。
- **修 bug**：现有 `CustomProxyConfig.ChangeToken` 包装的 `_cts` 从不被 Cancel，Provider Cancel 的是另一个无关 `_cts`（`ZhuoFan.Wb.YarpGateway/Services/CustomProxyConfig.cs:12-16`、`CustomProxyConfigProvider.cs:78-79`）。新实现须：Provider 持有 CTS，`GirvsGatewayProxyConfig.ChangeToken` 用**同一个** CTS 的 `CancellationChangeToken`；服务变化时先建新 config（新 CTS），再 Cancel 旧 CTS。
- 服务端点 DTO 用 `Yarp.ReverseProxy.Configuration.DestinationConfig`；YARP 类型（`IProxyConfig`/`IProxyConfigProvider`/`RouteConfig`/`ClusterConfig`/`RouteMatch`）均来自 `Yarp.ReverseProxy.Configuration`。
- 版本号随根 `Directory.Build.props`（当前 `10.0.0-rc.2`，不单独改）。`nugetpublish.ps1` 增 `Girvs.Aspire.Gateway` 推送行。
- 加入 `Girvs.slnx` 与测试工程。
- **验证分层**：约定路由生成、变更令牌接通、Consul 源、K8s 服务→端点映射逻辑——单测覆盖；K8s **watch 连接 + 动态路由更新**——在本地 kind 集群真跑通（Task 1 搭建、Task 7 验证）。

---

### Task 1: 本地 kind 集群搭建与验证（后续 K8s 验证的前置）

**目标交付物**：本机有一个可用的 kind K8s 集群，`kubectl get nodes` 返回 Ready。这是 Task 5/7 验证 K8s watch 的前提。

**Files:**
- Create: `/tmp/claude-*/scratchpad/kind-setup.sh`（临时脚本，不入库）

**Interfaces:**
- Produces: 一个名为 `girvs-gw` 的 kind 集群 + `kubectl` 可用（后续 Task 7 用）。

- [ ] **Step 1: 安装 kind 与 kubectl 二进制**

Run:
```bash
curl -fsSL -o /usr/local/bin/kind https://kind.sigs.k8s.io/dl/v0.24.0/kind-linux-amd64 && chmod +x /usr/local/bin/kind
curl -fsSL -o /usr/local/bin/kubectl "https://dl.k8s.io/release/v1.31.0/bin/linux/amd64/kubectl" && chmod +x /usr/local/bin/kubectl
kind --version && kubectl version --client
```
Expected: 打印 kind 与 kubectl 版本。（本机代理已验证可下载 kind 二进制。）

- [ ] **Step 2: 创建 kind 集群**

Run: `kind create cluster --name girvs-gw`
Expected: 结尾 `Set kubectl context to "kind-girvs-gw"`。首次会拉 kindest/node 镜像（~1GB，通过代理，耐心等）。若因 WSL/cgroup 限制失败，记录错误并**在此 checkpoint 与用户确认**（K8s watch 验证依赖它）。

- [ ] **Step 3: 验证集群可用**

Run: `kubectl get nodes`
Expected: 一个 `girvs-gw-control-plane` 节点 `Ready`。

- [ ] **Step 4: 记录集群信息（不提交代码，仅口头 checkpoint）**

本 Task 无代码提交；确认集群就绪后进入 Task 2。若集群搭建失败且无法解决，回退方案：K8s watch 源改为仅单测（fake 事件回放）覆盖，真集群验证移交业务迁移计划 3——但须先与用户确认。

---

### Task 2: 包骨架 + 服务发现抽象

**Files:**
- Create: `Girvs.Aspire.Gateway/Girvs.Aspire.Gateway.csproj`
- Create: `Girvs.Aspire.Gateway/GlobalUsings.cs`
- Create: `Girvs.Aspire.Gateway/GatewayServiceEndpoint.cs`
- Create: `Girvs.Aspire.Gateway/Discovery/IGatewayServiceDiscoverySource.cs`
- Create: `Girvs.Aspire.Gateway/Configuration/GatewayDiscoveryConfig.cs`
- Create: `tests/Girvs.Aspire.Gateway.Tests/Girvs.Aspire.Gateway.Tests.csproj`
- Modify: `Girvs.slnx`

**Interfaces:**
- Produces:
  - `sealed class GatewayServiceEndpoint { string ServiceName {get;init;} IReadOnlyDictionary<string, DestinationConfig> Destinations {get;init;} }`
  - `interface IGatewayServiceDiscoverySource { IReadOnlyList<GatewayServiceEndpoint> GetServices(); event Action ServicesChanged; Task StartAsync(CancellationToken ct); }`
  - `enum GatewayDiscoveryType { Consul, Kubernetes }` + `class GatewayDiscoveryConfig : IAppModuleConfig { GatewayDiscoveryType DiscoveryType {get;set;} string ConsulAddress {get;set;} void Init(); }`

- [ ] **Step 1: 创建包 csproj**

`Girvs.Aspire.Gateway/Girvs.Aspire.Gateway.csproj`：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks></TargetFrameworks>
    <TargetFramework>net10.0</TargetFramework>
    <Description>Girvs 自建 YARP 网关的服务发现与路由生成：可插拔 Consul/K8s(watch) 源。</Description>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\Girvs\Girvs.csproj" />
  </ItemGroup>
  <ItemGroup>
    <PackageReference Include="Yarp.ReverseProxy" Version="2.2.0" />
    <PackageReference Include="KubernetesClient" Version="15.0.1" />
    <PackageReference Include="Consul" Version="1.7.14.7" />
  </ItemGroup>
  <ItemGroup>
    <InternalsVisibleTo Include="Girvs.Aspire.Gateway.Tests" />
  </ItemGroup>
</Project>
```

> 版本占位：`Yarp.ReverseProxy`/`KubernetesClient`/`Consul` 三个版本实现时对照 `NewOnlineRegistration/netcoresrc/ZhuoFan.Wb.YarpGateway/ZhuoFan.Wb.YarpGateway.csproj` 的实际引用版本填写，保持一致。

- [ ] **Step 2: GlobalUsings**

`Girvs.Aspire.Gateway/GlobalUsings.cs`：

```csharp
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;
global using Microsoft.Extensions.Primitives;
global using Yarp.ReverseProxy.Configuration;
```

- [ ] **Step 3: 服务端点 DTO**

`Girvs.Aspire.Gateway/GatewayServiceEndpoint.cs`：

```csharp
namespace Girvs.Aspire.Gateway;

/// <summary>网关发现到的一个后端服务：服务名 + YARP 目的地集合。</summary>
public sealed class GatewayServiceEndpoint
{
    public required string ServiceName { get; init; }
    public required IReadOnlyDictionary<string, DestinationConfig> Destinations { get; init; }
}
```

- [ ] **Step 4: 服务发现抽象**

`Girvs.Aspire.Gateway/Discovery/IGatewayServiceDiscoverySource.cs`：

```csharp
namespace Girvs.Aspire.Gateway.Discovery;

/// <summary>网关服务发现来源：产出当前服务快照，并在服务集变化时触发 ServicesChanged。</summary>
public interface IGatewayServiceDiscoverySource
{
    /// <summary>当前已发现的服务快照。</summary>
    IReadOnlyList<GatewayServiceEndpoint> GetServices();

    /// <summary>服务集发生变化（增删改）时触发。</summary>
    event Action ServicesChanged;

    /// <summary>启动发现（轮询定时器或 watch 连接）。</summary>
    Task StartAsync(CancellationToken cancellationToken);
}
```

- [ ] **Step 5: 配置类**

`Girvs.Aspire.Gateway/Configuration/GatewayDiscoveryConfig.cs`：

```csharp
using Girvs.Configuration;

namespace Girvs.Aspire.Gateway.Configuration;

public enum GatewayDiscoveryType
{
    Consul,
    Kubernetes
}

public class GatewayDiscoveryConfig : IAppModuleConfig
{
    public GatewayDiscoveryType DiscoveryType { get; set; } = GatewayDiscoveryType.Consul;
    public string ConsulAddress { get; set; } = "http://192.168.51.166:8500";

    public void Init() { }
}
```

> `IAppModuleConfig` 命名空间 `Girvs.Configuration`（与现有 `ServiceDiscoveryConfig` 一致，见 `ZhuoFan.Wb.YarpGateway/Configuration/ServiceDiscoveryConfig.cs`）。

- [ ] **Step 6: 测试工程**

`tests/Girvs.Aspire.Gateway.Tests/Girvs.Aspire.Gateway.Tests.csproj`：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net10.0</TargetFrameworks>
    <IsPackable>false</IsPackable>
    <GeneratePackageOnBuild>false</GeneratePackageOnBuild>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.7.0" />
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\Girvs.Aspire.Gateway\Girvs.Aspire.Gateway.csproj" />
  </ItemGroup>
</Project>
```

`tests/Girvs.Aspire.Gateway.Tests/GlobalUsings.cs`：

```csharp
global using Xunit;
global using Girvs.Aspire.Gateway;
global using Girvs.Aspire.Gateway.Discovery;
global using Yarp.ReverseProxy.Configuration;
```

- [ ] **Step 7: 加入解决方案，构建**

`Girvs.slnx` 增两行（`Girvs.Aspire.Gateway/Girvs.Aspire.Gateway.csproj` 与 `tests/Girvs.Aspire.Gateway.Tests/Girvs.Aspire.Gateway.Tests.csproj`）。

Run: `dotnet build Girvs.Aspire.Gateway/Girvs.Aspire.Gateway.csproj`
Expected: Build succeeded。

- [ ] **Step 8: 提交**

```bash
git add Girvs.Aspire.Gateway/ tests/Girvs.Aspire.Gateway.Tests/ Girvs.slnx
git commit -m "feat: Girvs.Aspire.Gateway 包骨架与服务发现抽象"
```

---

### Task 3: 路由生成 Provider + 正确变更令牌（核心）

**Files:**
- Create: `Girvs.Aspire.Gateway/GirvsGatewayProxyConfig.cs`
- Create: `Girvs.Aspire.Gateway/GirvsGatewayProxyConfigProvider.cs`
- Create: `tests/Girvs.Aspire.Gateway.Tests/GirvsGatewayProxyConfigProviderTests.cs`

**Interfaces:**
- Consumes: `IGatewayServiceDiscoverySource`、`GatewayServiceEndpoint`（Task 2）。
- Produces:
  - `sealed class GirvsGatewayProxyConfig : IProxyConfig`（构造 `(IReadOnlyList<RouteConfig>, IReadOnlyList<ClusterConfig>, IChangeToken)`）。
  - `sealed class GirvsGatewayProxyConfigProvider : IProxyConfigProvider, IDisposable`（构造 `(IGatewayServiceDiscoverySource)`；`IProxyConfig GetConfig()`）。

- [ ] **Step 1: 写失败测试（路由生成 + 变更令牌接通）**

`tests/Girvs.Aspire.Gateway.Tests/GirvsGatewayProxyConfigProviderTests.cs`：

```csharp
using System.Collections.Generic;

namespace Girvs.Aspire.Gateway.Tests;

public class GirvsGatewayProxyConfigProviderTests
{
    private sealed class FakeSource : IGatewayServiceDiscoverySource
    {
        public List<GatewayServiceEndpoint> Services = new();
        public IReadOnlyList<GatewayServiceEndpoint> GetServices() => Services;
        public event Action? ServicesChanged;
        public Task StartAsync(CancellationToken ct) => Task.CompletedTask;
        public void Raise() => ServicesChanged?.Invoke();
    }

    private static GatewayServiceEndpoint Endpoint(string name) => new()
    {
        ServiceName = name,
        Destinations = new Dictionary<string, DestinationConfig>
        {
            [$"{name}-80"] = new DestinationConfig { Address = $"http://{name}:80" }
        }
    };

    [Fact]
    public void 每个服务生成约定路由与集群()
    {
        var source = new FakeSource { Services = { Endpoint("order_api") } };
        var provider = new GirvsGatewayProxyConfigProvider(source);

        var config = provider.GetConfig();
        var route = Assert.Single(config.Routes);
        Assert.Equal("order_api", route.RouteId);
        Assert.Equal("order_api", route.ClusterId);
        Assert.Equal("/order_api/{**catch-all}", route.Match.Path);
        Assert.Contains(route.Transforms!, t => t.TryGetValue("PathRemovePrefix", out var v) && v == "order_api");
        var cluster = Assert.Single(config.Clusters);
        Assert.Equal("order_api", cluster.ClusterId);
    }

    [Fact]
    public void 服务变化后_旧配置变更令牌被触发且新配置反映变化()
    {
        var source = new FakeSource { Services = { Endpoint("a") } };
        var provider = new GirvsGatewayProxyConfigProvider(source);
        var oldConfig = provider.GetConfig();
        Assert.False(oldConfig.ChangeToken.HasChanged);

        source.Services.Add(Endpoint("b"));
        source.Raise();

        Assert.True(oldConfig.ChangeToken.HasChanged); // 修掉了旧 bug：令牌真的接通
        var newConfig = provider.GetConfig();
        Assert.NotSame(oldConfig, newConfig);
        Assert.Equal(2, newConfig.Routes.Count);
    }

    [Fact]
    public void 重名服务去重()
    {
        var source = new FakeSource { Services = { Endpoint("a"), Endpoint("a") } };
        var provider = new GirvsGatewayProxyConfigProvider(source);
        Assert.Single(provider.GetConfig().Routes);
    }
}
```

- [ ] **Step 2: 运行确认失败**

Run: `dotnet test tests/Girvs.Aspire.Gateway.Tests/Girvs.Aspire.Gateway.Tests.csproj`
Expected: 编译失败（`GirvsGatewayProxyConfigProvider`/`GirvsGatewayProxyConfig` 不存在）。

- [ ] **Step 3: 实现 IProxyConfig**

`Girvs.Aspire.Gateway/GirvsGatewayProxyConfig.cs`：

```csharp
namespace Girvs.Aspire.Gateway;

public sealed class GirvsGatewayProxyConfig(
    IReadOnlyList<RouteConfig> routes,
    IReadOnlyList<ClusterConfig> clusters,
    IChangeToken changeToken
) : IProxyConfig
{
    public IReadOnlyList<RouteConfig> Routes { get; } = routes;
    public IReadOnlyList<ClusterConfig> Clusters { get; } = clusters;
    public IChangeToken ChangeToken { get; } = changeToken;
}
```

- [ ] **Step 4: 实现 Provider（正确接通变更令牌）**

`Girvs.Aspire.Gateway/GirvsGatewayProxyConfigProvider.cs`：

```csharp
using Girvs.Aspire.Gateway.Discovery;

namespace Girvs.Aspire.Gateway;

public sealed class GirvsGatewayProxyConfigProvider : IProxyConfigProvider, IDisposable
{
    private readonly IGatewayServiceDiscoverySource _source;
    private volatile GirvsGatewayProxyConfig _config;
    private CancellationTokenSource _cts;

    public GirvsGatewayProxyConfigProvider(IGatewayServiceDiscoverySource source)
    {
        _source = source;
        _cts = new CancellationTokenSource();
        _config = BuildConfig(source.GetServices(), _cts);
        _source.ServicesChanged += OnServicesChanged;
    }

    public IProxyConfig GetConfig() => _config;

    private void OnServicesChanged()
    {
        // 先建新配置（新令牌），再 Cancel 旧令牌通知 YARP 重新 GetConfig —— 修掉现有令牌未接通的 bug
        var newCts = new CancellationTokenSource();
        var newConfig = BuildConfig(_source.GetServices(), newCts);
        var oldCts = _cts;
        _config = newConfig;
        _cts = newCts;
        oldCts.Cancel();
        oldCts.Dispose();
    }

    private static GirvsGatewayProxyConfig BuildConfig(
        IReadOnlyList<GatewayServiceEndpoint> services,
        CancellationTokenSource cts
    )
    {
        var routes = new List<RouteConfig>();
        var clusters = new List<ClusterConfig>();

        foreach (var svc in services)
        {
            if (routes.Exists(r => r.RouteId == svc.ServiceName))
                continue;

            routes.Add(
                new RouteConfig
                {
                    RouteId = svc.ServiceName,
                    ClusterId = svc.ServiceName,
                    Match = new RouteMatch { Path = $"/{svc.ServiceName}/" + "{**catch-all}" },
                    Transforms = new List<IReadOnlyDictionary<string, string>>
                    {
                        new Dictionary<string, string> { ["PathRemovePrefix"] = svc.ServiceName }
                    }
                }
            );

            clusters.Add(
                new ClusterConfig { ClusterId = svc.ServiceName, Destinations = svc.Destinations }
            );
        }

        return new GirvsGatewayProxyConfig(
            routes,
            clusters,
            new CancellationChangeToken(cts.Token)
        );
    }

    public void Dispose()
    {
        _source.ServicesChanged -= OnServicesChanged;
        _cts.Dispose();
    }
}
```

> `RouteConfig.Transforms` 类型是 `IReadOnlyList<IReadOnlyDictionary<string,string>>`；测试里用 `t.TryGetValue(...)` 读取。若 YARP 2.2.0 的 `Transforms` 元素类型签名不同，按实际类型调整（保持"含 `PathRemovePrefix=服务名` 一项"的行为）。

- [ ] **Step 5: 运行确认通过**

Run: `dotnet test tests/Girvs.Aspire.Gateway.Tests/Girvs.Aspire.Gateway.Tests.csproj`
Expected: 3 个测试全绿。

- [ ] **Step 6: 提交**

```bash
git add Girvs.Aspire.Gateway/ tests/Girvs.Aspire.Gateway.Tests/
git commit -m "feat: 网关路由生成 Provider 与正确的变更令牌接通（修旧 bug）"
```

---

### Task 4: Consul 服务发现源（非 K8s 部署）

**Files:**
- Create: `Girvs.Aspire.Gateway/Discovery/ConsulGatewayServiceSource.cs`
- Create: `tests/Girvs.Aspire.Gateway.Tests/ConsulMappingTests.cs`

**Interfaces:**
- Consumes: `IGatewayServiceDiscoverySource`、`GatewayServiceEndpoint`。
- Produces: `sealed class ConsulGatewayServiceSource : IGatewayServiceDiscoverySource, IDisposable`（构造 `(IConsulClient)`；30s 轮询）；内部静态 `MapAgentServices(IDictionary<string, AgentService>) → List<GatewayServiceEndpoint>`（纯函数，可测）。

- [ ] **Step 1: 写失败测试（Consul agent 服务 → 端点映射）**

`tests/Girvs.Aspire.Gateway.Tests/ConsulMappingTests.cs`：

```csharp
using System.Collections.Generic;
using Consul;
using Girvs.Aspire.Gateway.Discovery;

namespace Girvs.Aspire.Gateway.Tests;

public class ConsulMappingTests
{
    [Fact]
    public void Consul服务映射为端点_服务名连字符转下划线_地址拼接()
    {
        var agentServices = new Dictionary<string, AgentService>
        {
            ["id1"] = new AgentService { Service = "order-api", Address = "10.0.0.1", Port = 8080 }
        };

        var endpoints = ConsulGatewayServiceSource.MapAgentServices(agentServices);

        var ep = Assert.Single(endpoints);
        Assert.Equal("order_api", ep.ServiceName);
        var dest = Assert.Single(ep.Destinations);
        Assert.Equal("order_api-8080", dest.Key);
        Assert.Equal("http://10.0.0.1:8080", dest.Value.Address);
    }

    [Fact]
    public void 同名服务多实例去重()
    {
        var agentServices = new Dictionary<string, AgentService>
        {
            ["id1"] = new AgentService { Service = "a", Address = "10.0.0.1", Port = 80 },
            ["id2"] = new AgentService { Service = "a", Address = "10.0.0.2", Port = 80 }
        };
        Assert.Single(ConsulGatewayServiceSource.MapAgentServices(agentServices));
    }
}
```

- [ ] **Step 2: 运行确认失败**

Run: `dotnet test tests/Girvs.Aspire.Gateway.Tests/Girvs.Aspire.Gateway.Tests.csproj --filter "FullyQualifiedName~ConsulMappingTests"`
Expected: 编译失败（`ConsulGatewayServiceSource` 不存在）。

- [ ] **Step 3: 实现 Consul 源**

`Girvs.Aspire.Gateway/Discovery/ConsulGatewayServiceSource.cs`（映射逻辑照搬现有 `ConsulClientService.GetConsulClientServices`，见 `ZhuoFan.Wb.YarpGateway/Services/ConsulClientService.cs`，改为轮询 + 事件）：

```csharp
using Consul;

namespace Girvs.Aspire.Gateway.Discovery;

public sealed class ConsulGatewayServiceSource(IConsulClient consulClient)
    : IGatewayServiceDiscoverySource, IDisposable
{
    private volatile IReadOnlyList<GatewayServiceEndpoint> _services = new List<GatewayServiceEndpoint>();
    private Timer? _timer;

    public event Action? ServicesChanged;

    public IReadOnlyList<GatewayServiceEndpoint> GetServices() => _services;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(_ => Refresh(), null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
        return Task.CompletedTask;
    }

    private void Refresh()
    {
        try
        {
            var agentServices = consulClient.Agent.Services().Result.Response;
            _services = MapAgentServices(agentServices);
            ServicesChanged?.Invoke();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Girvs.Aspire.Gateway] Consul 刷新失败：{ex.Message}");
        }
    }

    internal static List<GatewayServiceEndpoint> MapAgentServices(
        IDictionary<string, AgentService> agentServices
    )
    {
        var result = new List<GatewayServiceEndpoint>();
        foreach (var agentService in agentServices)
        {
            var serviceName = agentService.Value.Service.Replace("-", "_");
            if (result.Exists(x => x.ServiceName == serviceName))
                continue;

            var address = agentService.Value.Address;
            var port = agentService.Value.Port;
            result.Add(
                new GatewayServiceEndpoint
                {
                    ServiceName = serviceName,
                    Destinations = new Dictionary<string, DestinationConfig>
                    {
                        [$"{serviceName}-{port}"] = new DestinationConfig
                        {
                            Address = $"http://{address}:{port}"
                        }
                    }
                }
            );
        }
        return result;
    }

    public void Dispose() => _timer?.Dispose();
}
```

- [ ] **Step 4: 运行确认通过**

Run: `dotnet test tests/Girvs.Aspire.Gateway.Tests/Girvs.Aspire.Gateway.Tests.csproj --filter "FullyQualifiedName~ConsulMappingTests"`
Expected: 2 个测试通过。

- [ ] **Step 5: 提交**

```bash
git add Girvs.Aspire.Gateway/ tests/Girvs.Aspire.Gateway.Tests/
git commit -m "feat: Consul 网关服务发现源（轮询，非 K8s 部署）"
```

---

### Task 5: K8s watch 服务发现源

**Files:**
- Create: `Girvs.Aspire.Gateway/Discovery/KubernetesGatewayServiceSource.cs`
- Create: `tests/Girvs.Aspire.Gateway.Tests/KubernetesMappingTests.cs`

**Interfaces:**
- Consumes: `IGatewayServiceDiscoverySource`、`GatewayServiceEndpoint`。
- Produces: `sealed class KubernetesGatewayServiceSource : IGatewayServiceDiscoverySource, IDisposable`；内部静态 `MapServices(IEnumerable<V1Service>) → List<GatewayServiceEndpoint>`（纯函数，可测）；watch 连接逻辑（relist + 重连），在 kind 中真验证（Task 7）。

- [ ] **Step 1: 写失败测试（K8s Service → 端点映射，纯函数）**

`tests/Girvs.Aspire.Gateway.Tests/KubernetesMappingTests.cs`：

```csharp
using System.Collections.Generic;
using Girvs.Aspire.Gateway.Discovery;
using k8s.Models;

namespace Girvs.Aspire.Gateway.Tests;

public class KubernetesMappingTests
{
    private static V1Service Svc(string name, string ns, params int[] ports) => new()
    {
        Metadata = new V1ObjectMeta { Name = name, NamespaceProperty = ns },
        Spec = new V1ServiceSpec
        {
            Ports = ports.Select(p => new V1ServicePort { Port = p }).ToList()
        }
    };

    [Fact]
    public void K8s服务映射为端点_集群内DNS地址()
    {
        var services = new[] { Svc("order-api", "default", 8080) };

        var endpoints = KubernetesGatewayServiceSource.MapServices(services);

        var ep = Assert.Single(endpoints);
        Assert.Equal("order-api", ep.ServiceName);
        var dest = Assert.Single(ep.Destinations);
        Assert.Equal("order-api-8080", dest.Key);
        Assert.Equal("http://order-api.default.svc.cluster.local:8080", dest.Value.Address);
    }

    [Fact]
    public void 多端口服务生成多目的地()
    {
        var services = new[] { Svc("api", "ns", 80, 443) };
        var ep = Assert.Single(KubernetesGatewayServiceSource.MapServices(services));
        Assert.Equal(2, ep.Destinations.Count);
    }
}
```

- [ ] **Step 2: 运行确认失败**

Run: `dotnet test tests/Girvs.Aspire.Gateway.Tests/Girvs.Aspire.Gateway.Tests.csproj --filter "FullyQualifiedName~KubernetesMappingTests"`
Expected: 编译失败（`KubernetesGatewayServiceSource` 不存在）。

- [ ] **Step 3: 实现 K8s watch 源**

`Girvs.Aspire.Gateway/Discovery/KubernetesGatewayServiceSource.cs`（映射逻辑照搬现有 `KubernetesClientService`，见 `ZhuoFan.Wb.YarpGateway/Services/KubernetesClientService.cs`；发现方式从一次性 list 改为 watch + relist）：

```csharp
using System.Collections.Immutable;
using k8s;
using k8s.Models;

namespace Girvs.Aspire.Gateway.Discovery;

public sealed class KubernetesGatewayServiceSource : IGatewayServiceDiscoverySource, IDisposable
{
    private readonly IKubernetes _client;
    private volatile IReadOnlyList<GatewayServiceEndpoint> _services = new List<GatewayServiceEndpoint>();
    private readonly CancellationTokenSource _stop = new();
    private Task? _watchLoop;

    public KubernetesGatewayServiceSource(IKubernetes? client = null)
    {
        // 集群内运行用 InClusterConfig；client 参数便于测试替身注入
        _client = client ?? new Kubernetes(KubernetesClientConfiguration.InClusterConfig());
    }

    public event Action? ServicesChanged;

    public IReadOnlyList<GatewayServiceEndpoint> GetServices() => _services;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _watchLoop = Task.Run(() => WatchLoopAsync(_stop.Token), _stop.Token);
        return Task.CompletedTask;
    }

    private async Task WatchLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                // 全量 relist 建立当前状态
                var list = await _client.CoreV1.ListServiceForAllNamespacesAsync(cancellationToken: ct);
                _services = MapServices(list.Items);
                ServicesChanged?.Invoke();

                // 从 relist 的 resourceVersion 起 watch 增量变化
                using var watcher = _client.CoreV1.ListServiceForAllNamespacesWithHttpMessagesAsync(
                    watch: true,
                    resourceVersion: list.Metadata.ResourceVersion,
                    cancellationToken: ct
                );
                await foreach (var (_, _) in watcher.WatchAsync<V1Service, V1ServiceList>(cancellationToken: ct))
                {
                    // 任一 Service 变化后重新全量拉取并映射（简单可靠，服务数量级下开销可接受）
                    var current = await _client.CoreV1.ListServiceForAllNamespacesAsync(cancellationToken: ct);
                    _services = MapServices(current.Items);
                    ServicesChanged?.Invoke();
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Girvs.Aspire.Gateway] K8s watch 中断，2s 后重连：{ex.Message}");
                await Task.Delay(TimeSpan.FromSeconds(2), ct);
            }
        }
    }

    internal static List<GatewayServiceEndpoint> MapServices(IEnumerable<V1Service> services)
    {
        var result = new List<GatewayServiceEndpoint>();
        foreach (var service in services)
        {
            var serviceName = service.Metadata.Name;
            if (result.Exists(x => x.ServiceName == serviceName))
                continue;

            var builder = ImmutableDictionary.CreateBuilder<string, DestinationConfig>();
            foreach (var port in service.Spec?.Ports ?? new List<V1ServicePort>())
            {
                builder[$"{serviceName}-{port.Port}"] = new DestinationConfig
                {
                    Address =
                        $"http://{serviceName}.{service.Metadata.NamespaceProperty}.svc.cluster.local:{port.Port}"
                };
            }

            result.Add(
                new GatewayServiceEndpoint { ServiceName = serviceName, Destinations = builder.ToImmutable() }
            );
        }
        return result;
    }

    public void Dispose()
    {
        _stop.Cancel();
        _stop.Dispose();
        (_client as IDisposable)?.Dispose();
    }
}
```

> `WatchAsync<V1Service, V1ServiceList>` 与 `ListServiceForAllNamespacesWithHttpMessagesAsync(watch: true, ...)` 是 KubernetesClient 15.x 的标准 watch API；若该版本签名不同（如返回类型、`WatchAsync` 泛型参数），实现时对照所选 `KubernetesClient` 版本调整，保持"relist 建初态 + watch 增量触发重拉 + 断线重连"语义。`IKubernetes` 是 `Kubernetes` 的接口，便于测试注入替身。

- [ ] **Step 4: 运行确认通过（映射纯函数）**

Run: `dotnet test tests/Girvs.Aspire.Gateway.Tests/Girvs.Aspire.Gateway.Tests.csproj --filter "FullyQualifiedName~KubernetesMappingTests"`
Expected: 2 个测试通过。（watch 连接逻辑在 Task 7 的 kind 集群真验证。）

- [ ] **Step 5: 提交**

```bash
git add Girvs.Aspire.Gateway/ tests/Girvs.Aspire.Gateway.Tests/
git commit -m "feat: K8s watch 网关服务发现源（relist+watch+重连）"
```

---

### Task 6: AddGirvsGateway 注册扩展

**Files:**
- Create: `Girvs.Aspire.Gateway/GirvsGatewayExtensions.cs`
- Create: `tests/Girvs.Aspire.Gateway.Tests/GirvsGatewayExtensionsTests.cs`

**Interfaces:**
- Consumes: `GatewayDiscoveryConfig`/`GatewayDiscoveryType`、`ConsulGatewayServiceSource`、`KubernetesGatewayServiceSource`、`GirvsGatewayProxyConfigProvider`。
- Produces: `static IServiceCollection AddGirvsGateway(this IServiceCollection services, GatewayDiscoveryConfig config)`——按 `DiscoveryType` 注册 source（单例）、`IProxyConfigProvider`、反向代理 + 转换。

- [ ] **Step 1: 写失败测试（按类型注册对应 source）**

`tests/Girvs.Aspire.Gateway.Tests/GirvsGatewayExtensionsTests.cs`：

```csharp
using Girvs.Aspire.Gateway.Configuration;
using Girvs.Aspire.Gateway.Discovery;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.Aspire.Gateway.Tests;

public class GirvsGatewayExtensionsTests
{
    [Fact]
    public void 配置为Kubernetes_注册K8s源与Provider()
    {
        var services = new ServiceCollection();
        services.AddGirvsGateway(new GatewayDiscoveryConfig { DiscoveryType = GatewayDiscoveryType.Kubernetes });

        var descriptors = services.ToList();
        Assert.Contains(descriptors, d => d.ServiceType == typeof(IGatewayServiceDiscoverySource)
            && d.ImplementationType == typeof(KubernetesGatewayServiceSource));
        Assert.Contains(descriptors, d => d.ServiceType == typeof(Yarp.ReverseProxy.Configuration.IProxyConfigProvider));
    }

    [Fact]
    public void 配置为Consul_注册Consul源()
    {
        var services = new ServiceCollection();
        services.AddGirvsGateway(new GatewayDiscoveryConfig { DiscoveryType = GatewayDiscoveryType.Consul });

        Assert.Contains(services.ToList(), d => d.ServiceType == typeof(IGatewayServiceDiscoverySource)
            && d.ImplementationType == typeof(ConsulGatewayServiceSource));
    }
}
```

> 注意：`KubernetesGatewayServiceSource` 的默认构造在非集群环境会因 `InClusterConfig()` 抛异常，但**注册阶段不实例化**（DI 惰性），故本测试只验证注册描述符、不解析实例，安全。

- [ ] **Step 2: 运行确认失败**

Run: `dotnet test tests/Girvs.Aspire.Gateway.Tests/Girvs.Aspire.Gateway.Tests.csproj --filter "FullyQualifiedName~GirvsGatewayExtensionsTests"`
Expected: 编译失败（`AddGirvsGateway` 不存在）。

- [ ] **Step 3: 实现注册扩展**

`Girvs.Aspire.Gateway/GirvsGatewayExtensions.cs`（转换照搬现有 `YarpGatewayModule.ConfigureServices`，见 `ZhuoFan.Wb.YarpGateway/YarpGatewayModule.cs:21-35`）：

```csharp
using Consul;
using Girvs.Aspire.Gateway.Configuration;
using Girvs.Aspire.Gateway.Discovery;
using Microsoft.Extensions.DependencyInjection;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Transforms;

namespace Girvs.Aspire.Gateway;

public static class GirvsGatewayExtensions
{
    public static IServiceCollection AddGirvsGateway(
        this IServiceCollection services,
        GatewayDiscoveryConfig config
    )
    {
        if (config.DiscoveryType == GatewayDiscoveryType.Consul)
        {
            services.AddSingleton<IConsulClient>(
                _ => new ConsulClient(c => c.Address = new Uri(config.ConsulAddress))
            );
            services.AddSingleton<IGatewayServiceDiscoverySource, ConsulGatewayServiceSource>();
        }
        else
        {
            services.AddSingleton<IGatewayServiceDiscoverySource, KubernetesGatewayServiceSource>();
        }

        services.AddSingleton<IProxyConfigProvider, GirvsGatewayProxyConfigProvider>();

        services
            .AddReverseProxy()
            .AddTransforms(context =>
            {
                context.AddOriginalHost(false);
                context.CopyRequestHeaders = true;
                context.AddXForwarded(ForwardedTransformActions.Append);
                context.AddXForwardedFor("X-Forwarded-For", ForwardedTransformActions.Append);
            });

        return services;
    }
}
```

> `KubernetesGatewayServiceSource` 的构造签名 `(IKubernetes? client = null)` 允许 DI 无参解析（用默认 `InClusterConfig`）。`ConsulGatewayServiceSource(IConsulClient)` 由 DI 注入上面注册的 `IConsulClient`。source 的 `StartAsync` 需在应用启动时调用——由使用方（网关的 Startup/模块）在 `Configure` 阶段 `serviceProvider.GetRequiredService<IGatewayServiceDiscoverySource>().StartAsync(...)`；接入文档写明（Task 7）。

- [ ] **Step 4: 运行确认通过 + 整包回归**

Run: `dotnet test tests/Girvs.Aspire.Gateway.Tests/Girvs.Aspire.Gateway.Tests.csproj`
Expected: 全部测试通过（路由 3 + Consul 2 + K8s 2 + 注册 2）。

- [ ] **Step 5: 提交**

```bash
git add Girvs.Aspire.Gateway/ tests/Girvs.Aspire.Gateway.Tests/
git commit -m "feat: AddGirvsGateway 注册扩展（按类型选源 + 反向代理转换）"
```

---

### Task 7: kind 集群真验证 K8s watch 动态路由 + 文档收尾

**目标交付物**：在 kind 集群里部署一个用 `Girvs.Aspire.Gateway` 的最小网关 + 两个 dummy 后端服务，验证 watch 能感知服务上下线、YARP 路由动态更新；补文档；把包纳入发布。

**Files:**
- Create: `samples/gateway-k8s/`（kind 验证用：Dockerfile、K8s manifest、最小网关项目或复用 samples 网关）
- Create: `Girvs.Aspire.Gateway/README.md`
- Modify: `nugetpublish.ps1`
- Modify: `docs/aspire/apphost-guide.md`（补网关一节）

**Interfaces:**
- Consumes: 全部前序产出。

- [ ] **Step 1: 建最小网关验证项目**

在 `samples/gateway-k8s/` 建一个最小 ASP.NET Core 网关：`Program.cs` 用 `services.AddGirvsGateway(new GatewayDiscoveryConfig { DiscoveryType = GatewayDiscoveryType.Kubernetes })`，`app.MapReverseProxy()`，启动时 `app.Services.GetRequiredService<IGatewayServiceDiscoverySource>().StartAsync(default)`。`ProjectReference` 引 `Girvs.Aspire.Gateway`。给它一个 `Dockerfile`（`mcr.microsoft.com/dotnet/aspnet:10.0` 基镜像）。

> 该验证网关独立于 `GirvsAspireSample.slnx`（那套走 Aspire 服务发现、非 K8s），单独构建镜像加载进 kind。

- [ ] **Step 2: 写 K8s 部署清单**

`samples/gateway-k8s/k8s.yaml`：含 ① ServiceAccount + Role（`services` 的 `list`/`watch`）+ RoleBinding；② gateway Deployment（用 ServiceAccount）+ Service；③ 两个 dummy 后端（`hashicorp/http-echo` 或 `nginx`）Deployment + Service，名字如 `echo-a`、`echo-b`。

- [ ] **Step 3: 构建网关镜像并加载进 kind**

Run:
```bash
docker build -t girvs-gw-sample:test -f samples/gateway-k8s/Dockerfile .
kind load docker-image girvs-gw-sample:test --name girvs-gw
kubectl apply -f samples/gateway-k8s/k8s.yaml
kubectl wait --for=condition=available deploy/gateway --timeout=120s
```
Expected: 网关与两个 echo 服务 Running。

- [ ] **Step 4: 验证初始路由（watch 建立初态）**

Run:
```bash
kubectl port-forward svc/gateway 18080:80 &
sleep 3
curl -s http://localhost:18080/echo-a/   # 约定路由 /{服务名}/... → 去前缀转发到 echo-a
curl -s http://localhost:18080/echo-b/
```
Expected: 分别返回 echo-a / echo-b 的响应（证明 K8s watch 拿到服务、约定路由 + PathRemovePrefix 生效）。

- [ ] **Step 5: 验证动态更新（watch 感知上下线）**

Run:
```bash
kubectl scale deploy/echo-b --replicas=0    # 下线 echo-b（其 Service 仍在，但演示 watch 响应变化）
kubectl delete svc echo-b                    # 删除 Service → watch 事件
sleep 5
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:18080/echo-b/   # 期望 404（路由已被 watch 动态移除）
kubectl apply -f samples/gateway-k8s/k8s.yaml # 重新加回
sleep 5
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:18080/echo-b/   # 期望恢复 200
```
Expected: 删除后 404、恢复后 200——证明 watch 事件驱动的动态路由更新（无 30s 轮询等待），变更令牌正确触发 YARP 热重载。

- [ ] **Step 6: 写包 README + 更新 apphost-guide + nugetpublish**

`Girvs.Aspire.Gateway/README.md`：用途、`AddGirvsGateway` 用法、`GatewayDiscoveryType` 三部署形态（CentOS/docker→Consul、K8s→watch）、需在启动调 `StartAsync`、K8s RBAC 清单示例、约定路由说明。
`docs/aspire/apphost-guide.md` 补"网关"一节指向本包与 `samples/gateway-k8s`。
`nugetpublish.ps1` 增 `Girvs.Aspire.Gateway` 推送行。

- [ ] **Step 7: 全量验证 + 提交 + 清理集群**

Run: `dotnet build Girvs.slnx -c Release`（三 TFM 整体可编译）且 `dotnet test tests/Girvs.Aspire.Gateway.Tests/Girvs.Aspire.Gateway.Tests.csproj`
Expected: 构建成功、测试全绿。

```bash
git add Girvs.Aspire.Gateway/ samples/gateway-k8s/ nugetpublish.ps1 docs/aspire/apphost-guide.md
git commit -m "test: kind 集群验证 K8s watch 动态路由 + 网关包文档与发布收尾"
kind delete cluster --name girvs-gw   # 清理（可选）
```

---

## Self-Review

**Spec 覆盖**（对照 `2026-07-14-aspire-k8s-production-design.md` 第 3.1 节修正版）：
- 可插拔服务发现抽象 → Task 2（`IGatewayServiceDiscoverySource`）✅
- Consul 源（非 K8s 部署）→ Task 4 ✅
- K8s watch 源（替代轮询）→ Task 5（映射单测）+ Task 7（kind 真验证 watch 动态更新）✅
- Provider 约定路由 + 修变更令牌 bug → Task 3 ✅
- `AddGirvsGateway` 按 `ServiceDiscoveryType` 选源 + 保留转换 → Task 6 ✅
- 多部署形态（CentOS/docker→Consul、K8s→watch）→ Task 6 注册逻辑 + Task 7 文档 ✅
- 不引入 Label 分流/Annotation 路由 → 全程未引入（分流留网关侧中间件）✅
- RBAC 清单 → Task 7 K8s manifest + README ✅

**Placeholder 扫描**：无 TBD/TODO；代码步骤含完整代码。YARP/KubernetesClient/Consul 的包版本与个别 API 签名（`Transforms` 元素类型、`WatchAsync` 泛型）标注了"对照实际版本核对"的校正点——非占位符，是依赖版本现实的必要说明。

**类型一致性**：`GatewayServiceEndpoint`（`ServiceName`/`Destinations`）、`IGatewayServiceDiscoverySource`（`GetServices`/`ServicesChanged`/`StartAsync`）、`GirvsGatewayProxyConfig`(routes,clusters,changeToken)、`GirvsGatewayProxyConfigProvider(source)`、`ConsulGatewayServiceSource.MapAgentServices`、`KubernetesGatewayServiceSource.MapServices`、`AddGirvsGateway(services, config)` 跨任务签名一致。

**范围检查**：单一子系统（网关服务发现 + 路由），聚焦，无需再拆。真业务网关（两个 YarpGateway）改用本包属业务迁移计划 3，不在本计划。

## 后续
- 计划 3：NewOnlineRegistration 两个 YarpGateway 改用 `Girvs.Aspire.Gateway`（删除各自重复的 `CustomProxyConfigProvider`/`ConsulClientService`/`KubernetesClientService`），并把 20+ 业务服务整体迁移到 Aspire。

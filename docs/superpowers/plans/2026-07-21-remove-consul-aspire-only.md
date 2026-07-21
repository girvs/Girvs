# Remove Consul Aspire Only Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 删除 Girvs 主线中的 Consul 服务发现，统一为本地 Aspire 发现源与生产 Kubernetes 发现源。

**Architecture:** `Girvs.Aspire` 继续负责服务端 `HttpClient` 服务发现；`Girvs.Aspire.Gateway` 删除 Consul 源，新增 `AspireGatewayServiceSource` 从 AppHost 注入的配置构建 YARP 服务快照；生产保留 `KubernetesGatewayServiceSource`。样例 AppHost 不再启动 Consul 容器，文档和测试同步改成纯 Aspire 路线。

**Tech Stack:** .NET 10、xUnit、Microsoft.Extensions.Configuration、Microsoft.Extensions.ServiceDiscovery、YARP、Aspire.Hosting、KubernetesClient。

## Global Constraints

- 全程使用简体中文沟通；代码标识符和已有英文注释遵循项目惯例。
- 使用 TDD：每个行为变更先写失败测试，再写实现。
- 不保留 Consul 兼容开关。
- 不迁移旧 net8/net9 部署模型；框架主线面向 Aspire 统一架构。
- 本地：Aspire AppHost + Docker 基础设施 + Aspire 注入端点。
- 生产：Aspire 发布到 K8s + K8s 原生 Service/EndpointSlice。

---

## Files

- Modify: `Girvs.Aspire.Gateway/Configuration/GatewayDiscoveryConfig.cs`，删除 Consul 配置，默认 `Aspire`。
- Modify: `Girvs.Aspire.Gateway/GirvsGatewayExtensions.cs`，按 `Aspire`/`Kubernetes` 注册发现源。
- Create: `Girvs.Aspire.Gateway/Discovery/AspireGatewayServiceSource.cs`，读取本地 AppHost 注入端点。
- Delete: `Girvs.Aspire.Gateway/Discovery/ConsulGatewayServiceSource.cs`。
- Modify: `Girvs.Aspire.Gateway/Girvs.Aspire.Gateway.csproj`，删除 `Consul` 包引用。
- Modify: `Girvs.Aspire/AspireModule.cs`，删除 Consul 共存告警。
- Modify: `Girvs.slnx`，移除 `Girvs.Consul` 项目。
- Delete: `Girvs.Consul/`。
- Modify: `samples/Sample.AppHost/Program.cs`，删除 Consul 容器和环境变量，网关 Run 模式使用 Aspire 发现。
- Modify: `samples/Sample.ServiceA/Sample.ServiceA.csproj`、`samples/Sample.ServiceB/Sample.ServiceB.csproj`，删除 `Girvs.Consul` 引用。
- Modify: `samples/Sample.ServiceA/appsettings.json`、`samples/Sample.ServiceB/appsettings.json`，删除 `ConsulConfig`。
- Modify: `samples/README.md`、`docs/aspire/apphost-guide.md`、`docs/aspire/升级方案.md`、`CLAUDE.md`，删除 Consul 推荐和共存描述。
- Modify/Delete/Create tests under `tests/Girvs.Aspire.Gateway.Tests/` and `tests/Girvs.Aspire.Hosting.Tests/`。

---

### Task 1: Gateway 配置与 DI 移除 Consul

**Files:**
- Modify: `Girvs.Aspire.Gateway/Configuration/GatewayDiscoveryConfig.cs`
- Modify: `Girvs.Aspire.Gateway/GirvsGatewayExtensions.cs`
- Modify: `Girvs.Aspire.Gateway/Girvs.Aspire.Gateway.csproj`
- Modify: `tests/Girvs.Aspire.Gateway.Tests/GirvsGatewayExtensionsTests.cs`

**Interfaces:**
- Produces: `GatewayDiscoveryType.Aspire` and `GatewayDiscoveryType.Kubernetes`
- Produces: `GatewayDiscoveryConfig.DiscoveryType` defaulting to `GatewayDiscoveryType.Aspire`

- [ ] **Step 1: Write failing tests**

Update `tests/Girvs.Aspire.Gateway.Tests/GirvsGatewayExtensionsTests.cs`:

```csharp
[Fact]
public void 默认配置_注册Aspire发现源与Provider()
{
    var services = new ServiceCollection();
    services.AddGirvsGateway(new GatewayDiscoveryConfig());

    var descriptors = services.ToList();
    Assert.Contains(descriptors, d => d.ServiceType == typeof(IGatewayServiceDiscoverySource)
        && d.ImplementationType == typeof(AspireGatewayServiceSource));
    Assert.Contains(descriptors, d => d.ServiceType == typeof(Yarp.ReverseProxy.Configuration.IProxyConfigProvider));
}

[Fact]
public void GatewayDiscoveryConfig_不再暴露Consul配置()
{
    var properties = typeof(GatewayDiscoveryConfig).GetProperties().Select(p => p.Name).ToArray();

    Assert.DoesNotContain("ConsulAddress", properties);
    Assert.DoesNotContain("ConsulConfig", properties);
    Assert.DoesNotContain("Consul", Enum.GetNames<GatewayDiscoveryType>());
}
```

- [ ] **Step 2: Verify red**

Run: `dotnet test tests/Girvs.Aspire.Gateway.Tests/Girvs.Aspire.Gateway.Tests.csproj --filter "FullyQualifiedName~GirvsGatewayExtensionsTests" --no-restore --nologo`

Expected: FAIL because `AspireGatewayServiceSource` and `GatewayDiscoveryType.Aspire` do not exist yet.

- [ ] **Step 3: Minimal implementation**

Change `GatewayDiscoveryConfig.cs` to:

```csharp
public enum GatewayDiscoveryType
{
    Aspire,
    Kubernetes
}

public class GatewayDiscoveryConfig : IAppModuleConfig
{
    public GatewayDiscoveryType DiscoveryType { get; set; } = GatewayDiscoveryType.Aspire;

    public void Init() { }
}
```

Change `GirvsGatewayExtensions.cs` to register `AspireGatewayServiceSource` for `Aspire`, `KubernetesGatewayServiceSource` for `Kubernetes`, and remove `using Consul;` plus `IConsulClient` registration.

Remove `<PackageReference Include="Consul" ... />` from `Girvs.Aspire.Gateway.csproj`.

- [ ] **Step 4: Verify green**

Run the same filtered test. Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Girvs.Aspire.Gateway/Configuration/GatewayDiscoveryConfig.cs Girvs.Aspire.Gateway/GirvsGatewayExtensions.cs Girvs.Aspire.Gateway/Girvs.Aspire.Gateway.csproj tests/Girvs.Aspire.Gateway.Tests/GirvsGatewayExtensionsTests.cs
git commit -m "feat: 网关默认使用 Aspire 服务发现"
```

---

### Task 2: 新增本地 Aspire 网关发现源

**Files:**
- Create: `Girvs.Aspire.Gateway/Discovery/AspireGatewayServiceSource.cs`
- Create: `tests/Girvs.Aspire.Gateway.Tests/AspireGatewayServiceSourceTests.cs`
- Delete: `tests/Girvs.Aspire.Gateway.Tests/ConsulMappingTests.cs`
- Delete: `Girvs.Aspire.Gateway/Discovery/ConsulGatewayServiceSource.cs`

**Interfaces:**
- Consumes: `GatewayServiceEndpoint`
- Produces: `public sealed class AspireGatewayServiceSource : IGatewayServiceDiscoverySource`
- Produces: `internal static IReadOnlyList<GatewayServiceEndpoint> MapConfiguration(IConfiguration configuration, ILogger logger)`

- [ ] **Step 1: Write failing tests**

Create `tests/Girvs.Aspire.Gateway.Tests/AspireGatewayServiceSourceTests.cs`:

```csharp
using Girvs.Aspire.Gateway.Discovery;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Girvs.Aspire.Gateway.Tests;

public class AspireGatewayServiceSourceTests
{
    [Fact]
    public void 读取Aspire注入端点_生成服务快照()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["services:service-a:http:0"] = "http://127.0.0.1:5101",
                ["services:service-b:http:0"] = "https://127.0.0.1:7102"
            })
            .Build();

        var endpoints = AspireGatewayServiceSource.MapConfiguration(configuration, NullLogger.Instance);

        Assert.Collection(
            endpoints.OrderBy(x => x.ServiceName),
            service =>
            {
                Assert.Equal("service-a", service.ServiceName);
                Assert.Equal("http://127.0.0.1:5101", Assert.Single(service.Destinations).Value.Address);
            },
            service =>
            {
                Assert.Equal("service-b", service.ServiceName);
                Assert.Equal("https://127.0.0.1:7102", Assert.Single(service.Destinations).Value.Address);
            });
    }

    [Fact]
    public async Task StartAsync_构建快照并触发变更事件()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["services:service-a:http:0"] = "http://127.0.0.1:5101"
            })
            .Build();
        var source = new AspireGatewayServiceSource(configuration, NullLogger<AspireGatewayServiceSource>.Instance);
        var changed = false;
        source.ServicesChanged += () => changed = true;

        await source.StartAsync(CancellationToken.None);

        Assert.True(changed);
        Assert.Single(source.GetServices());
    }
}
```

- [ ] **Step 2: Verify red**

Run: `dotnet test tests/Girvs.Aspire.Gateway.Tests/Girvs.Aspire.Gateway.Tests.csproj --filter "FullyQualifiedName~AspireGatewayServiceSourceTests" --no-restore --nologo`

Expected: FAIL because `AspireGatewayServiceSource` does not exist.

- [ ] **Step 3: Minimal implementation**

Create `AspireGatewayServiceSource.cs`:

```csharp
using System.Collections.Immutable;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Girvs.Aspire.Gateway.Discovery;

public sealed class AspireGatewayServiceSource : IGatewayServiceDiscoverySource
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AspireGatewayServiceSource> _logger;
    private volatile IReadOnlyList<GatewayServiceEndpoint> _services = [];

    public AspireGatewayServiceSource(IConfiguration configuration, ILogger<AspireGatewayServiceSource> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public event Action? ServicesChanged;

    public IReadOnlyList<GatewayServiceEndpoint> GetServices() => _services;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _services = MapConfiguration(_configuration, _logger);
        ServicesChanged?.Invoke();
        return Task.CompletedTask;
    }

    internal static IReadOnlyList<GatewayServiceEndpoint> MapConfiguration(IConfiguration configuration, ILogger logger)
    {
        var result = new List<GatewayServiceEndpoint>();
        foreach (var serviceSection in configuration.GetSection("services").GetChildren())
        {
            var destinations = ImmutableDictionary.CreateBuilder<string, DestinationConfig>();
            foreach (var endpointSection in serviceSection.GetChildren())
            {
                foreach (var valueSection in endpointSection.GetChildren())
                {
                    if (string.IsNullOrWhiteSpace(valueSection.Value))
                        continue;

                    destinations[$"{serviceSection.Key}-{endpointSection.Key}-{valueSection.Key}"] =
                        new DestinationConfig { Address = valueSection.Value.EndsWith('/') ? valueSection.Value : valueSection.Value + "/" };
                }
            }

            if (destinations.Count > 0)
                result.Add(new GatewayServiceEndpoint { ServiceName = serviceSection.Key, Destinations = destinations.ToImmutable() });
        }

        if (result.Count == 0)
            logger.LogWarning("Aspire 网关发现源未发现可用服务端点");

        return result;
    }
}
```

Delete Consul source and tests.

- [ ] **Step 4: Verify green**

Run: `dotnet test tests/Girvs.Aspire.Gateway.Tests/Girvs.Aspire.Gateway.Tests.csproj --filter "FullyQualifiedName~AspireGatewayServiceSourceTests|FullyQualifiedName~GirvsGatewayExtensionsTests" --no-restore --nologo`

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Girvs.Aspire.Gateway tests/Girvs.Aspire.Gateway.Tests
git commit -m "feat: 新增 Aspire 网关发现源"
```

---

### Task 3: 清理框架和样例中的 Girvs.Consul

**Files:**
- Modify: `Girvs.Aspire/AspireModule.cs`
- Modify: `Girvs.slnx`
- Delete: `Girvs.Consul/`
- Modify: `samples/Sample.AppHost/Program.cs`
- Modify: `samples/Sample.ServiceA/Sample.ServiceA.csproj`
- Modify: `samples/Sample.ServiceB/Sample.ServiceB.csproj`
- Modify: `samples/Sample.ServiceA/appsettings.json`
- Modify: `samples/Sample.ServiceB/appsettings.json`
- Modify: `tests/Girvs.Aspire.Hosting.Tests/SampleGatewayConsulContractTests.cs`

**Interfaces:**
- Consumes: `GatewayDiscoveryType.Aspire`
- Produces: sample AppHost without Consul container or `ConsulConfig`

- [ ] **Step 1: Write failing tests**

Rename `SampleGatewayConsulContractTests` intent to pure Aspire and assert:

```csharp
[Fact]
public void 样例AppHost_不再启动Consul_网关Run模式使用Aspire()
{
    var repoRoot = FindRepoRoot();
    var appHostProgram = File.ReadAllText(Path.Combine(repoRoot, "samples", "Sample.AppHost", "Program.cs"));
    var serviceAProject = File.ReadAllText(Path.Combine(repoRoot, "samples", "Sample.ServiceA", "Sample.ServiceA.csproj"));
    var serviceBProject = File.ReadAllText(Path.Combine(repoRoot, "samples", "Sample.ServiceB", "Sample.ServiceB.csproj"));
    var serviceASettings = File.ReadAllText(Path.Combine(repoRoot, "samples", "Sample.ServiceA", "appsettings.json"));
    var serviceBSettings = File.ReadAllText(Path.Combine(repoRoot, "samples", "Sample.ServiceB", "appsettings.json"));

    Assert.DoesNotContain("AddContainer(\"consul\"", appHostProgram);
    Assert.DoesNotContain("ConsulConfig", appHostProgram);
    Assert.DoesNotContain("Girvs.Consul.csproj", serviceAProject);
    Assert.DoesNotContain("Girvs.Consul.csproj", serviceBProject);
    Assert.DoesNotContain("\"ConsulConfig\"", serviceASettings);
    Assert.DoesNotContain("\"ConsulConfig\"", serviceBSettings);
    Assert.Contains("GatewayDiscovery__DiscoveryType\", \"Aspire\"", appHostProgram);
    Assert.Contains("GatewayDiscovery__DiscoveryType\", \"Kubernetes\"", appHostProgram);
}
```

- [ ] **Step 2: Verify red**

Run: `dotnet test tests/Girvs.Aspire.Hosting.Tests/Girvs.Aspire.Hosting.Tests.csproj --filter "FullyQualifiedName~SampleGateway" --no-restore --nologo`

Expected: FAIL because sample still contains Consul.

- [ ] **Step 3: Minimal implementation**

Remove Consul project, references, config sections, and `AspireModule.WarnWhenConsulCoexists`. In `samples/Sample.AppHost/Program.cs`, keep gateway `.WithReference(serviceA).WithReference(serviceB)` and set:

```csharp
if (builder.ExecutionContext.IsRunMode)
{
    gateway.WithEnvironment("GatewayDiscovery__DiscoveryType", "Aspire");
}

if (builder.ExecutionContext.IsPublishMode)
{
    gateway.WithEnvironment("GatewayDiscovery__DiscoveryType", "Kubernetes");
}
```

- [ ] **Step 4: Verify green**

Run: `dotnet test tests/Girvs.Aspire.Hosting.Tests/Girvs.Aspire.Hosting.Tests.csproj --filter "FullyQualifiedName~SampleGateway" --no-restore --nologo`

Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add Girvs.Aspire Girvs.slnx samples tests/Girvs.Aspire.Hosting.Tests
git rm -r Girvs.Consul
git commit -m "refactor: 移除 Girvs Consul 模块"
```

---

### Task 4: 文档清理与全量验证

**Files:**
- Modify: `CLAUDE.md`
- Modify: `docs/aspire/apphost-guide.md`
- Modify: `docs/aspire/升级方案.md`
- Modify: `samples/README.md`

**Interfaces:**
- Consumes: pure Aspire architecture from previous tasks
- Produces: docs with no recommended Consul path

- [ ] **Step 1: Write failing checks**

Run:

```bash
rg -n "Girvs\\.Consul|ConsulConfig|GatewayDiscoveryType\\.Consul|继续使用 Girvs\\.Consul|共存告警" CLAUDE.md docs/aspire samples tests Girvs.Aspire Girvs.Aspire.Gateway --glob '!**/bin/**' --glob '!**/obj/**'
```

Expected: output contains stale references.

- [ ] **Step 2: Minimal documentation edits**

Rewrite stale references so they describe:

- 本地 AppHost 不启动 Consul；
- 服务端引用 `Girvs.Aspire`；
- 网关本地使用 `GatewayDiscoveryType.Aspire`；
- 网关生产使用 `GatewayDiscoveryType.Kubernetes`。

- [ ] **Step 3: Verify clean search**

Run the same `rg` command.

Expected: no stale recommended Consul references. If historical design docs still mention Consul, either leave them as history under `docs/superpowers/specs/` or make the search scope exclude old specs.

- [ ] **Step 4: Build and test**

Run:

```bash
dotnet build Girvs.slnx --no-restore --nologo
dotnet test tests/Girvs.Aspire.Gateway.Tests/Girvs.Aspire.Gateway.Tests.csproj --no-restore --nologo
dotnet test tests/Girvs.Aspire.Hosting.Tests/Girvs.Aspire.Hosting.Tests.csproj --no-restore --nologo
```

Expected: all PASS.

- [ ] **Step 5: Commit**

```bash
git add CLAUDE.md docs/aspire samples/README.md
git commit -m "docs: 更新 Aspire 纯服务发现说明"
```

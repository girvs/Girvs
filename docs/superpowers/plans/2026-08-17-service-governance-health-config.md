---
plan_id: 2026-08-17-service-governance-health-config
artifact_version: 1.0.0
workflow_status: approved
approved_at: 2026-08-17
spec_path: docs/superpowers/specs/2026-08-17-service-governance-health-config-design.md
handoff_status: ready_for_implementation
---

# 服务治理健康检查与服务名配置模型重构 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 用 `ConsulRegistrationAddress` + `HealthCheckPath` + `LivenessCheckPath` 三属性替换 `HealthAddress`，统一 Consul/Aspire/K8s 健康检查路径；将 `ServerName` 升级为 Aspire 资源名与 Consul 注册名的唯一配置入口（归一化处理）。

**Architecture:** 服务侧（`Girvs.ServiceGovernance`）改配置模型、Consul 组合规则与配置驱动端点映射；核心库 `ServiceNameResolver` 新增归一化方法；AppHost 侧（`Girvs.Aspire.Hosting`）从服务 appsettings.json 读取健康检查路径与 `ServerName`，声明 `WithHttpProbe` 双探针并决定资源名。各层独立任务、独立测试周期。

**Tech Stack:** .NET 10、Aspire Hosting 13.4.6、Consul 1.7.14.9、xUnit。

## Global Constraints

- Breaking 变更：删除 `HealthAddress`，不保留任何兼容读取/反解；全仓库最终不得残留 `HealthAddress` 引用。
- 归一化规则（`ServiceNameResolver.FromServerName`）：`.` 与 `_` → `-`、全部小写化；对已是"小写字母、数字、连字符"的规范配置幂等。
- `ConsulRegistrationAddress` 必须为合法 `http`/`https` 绝对 URI；`HealthCheckPath` / `LivenessCheckPath` 必须以 `/` 开头。
- `ServerName` 读取边界：AppHost 仅读取服务项目目录的 `appsettings.json` 文件配置；环境变量 / ConfigMap 注入值读取不到，由 `AddGirvsProject<T>(name)` 显式传名兜底。
- Aspire 探针 API 为实验性（诊断 `ASPIREPROBES001`）：使用 `WithHttpProbe` / `ProbeType` 处须 `#pragma warning disable ASPIREPROBES001`（或项目级 NoWarn）。
- 依赖版本：`Aspire.Hosting` 13.4.6（满足 `WithHttpProbe` ≥ 9.5）；无新增 NuGet 包。
- 测试框架 xUnit，测试方法名用中文描述行为与预期；提交信息用 Conventional Commits 中文摘要并标注模块。

---

### Task 1: 核心库服务名归一化

**Files:**
- Modify: `Girvs/Infrastructure/ServiceNameResolver.cs`
- Test: `tests/Girvs.Aspire.Hosting.Tests/ServiceNameResolverTests.cs`

**Interfaces:**
- Consumes: 无（独立任务）。
- Produces: `public static string ServiceNameResolver.FromServerName(string serverName)`——`serverName.Replace(".", "-").Replace("_", "-").ToLowerInvariant()`。Task 2（`ConsulServiceRegistrar.GetServerName`）与 Task 3（Aspire 资源名）均依赖此方法。

- [ ] **Step 1: 写失败测试**

在 `tests/Girvs.Aspire.Hosting.Tests/ServiceNameResolverTests.cs` 追加：

```csharp
[Theory]
[InlineData("Sample.ServiceA", "sample-servicea")]
[InlineData("My_Service_01", "my-service-01")]
[InlineData("sample-servicea", "sample-servicea")]
public void FromServerName_服务名配置_生成统一服务名(string serverName, string expected) =>
    Assert.Equal(expected, ServiceNameResolver.FromServerName(serverName));
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test tests/Girvs.Aspire.Hosting.Tests/Girvs.Aspire.Hosting.Tests.csproj --filter FullyQualifiedName~FromServerName --nologo`

Expected: 编译失败，提示 `FromServerName` 不存在。

- [ ] **Step 3: 实现最小归一化方法**

在 `Girvs/Infrastructure/ServiceNameResolver.cs` 追加：

```csharp
/// <summary>根据配置的 ServerName 生成规范化服务名：. 与 _ 转 -、全部小写；对规范配置幂等。</summary>
public static string FromServerName(string serverName) =>
    serverName.Replace(".", "-").Replace("_", "-").ToLowerInvariant();
```

- [ ] **Step 4: 运行测试确认通过**

Run: `dotnet test tests/Girvs.Aspire.Hosting.Tests/Girvs.Aspire.Hosting.Tests.csproj --filter FullyQualifiedName~FromServerName --nologo`

Expected: PASS。

- [ ] **Step 5: 提交**

```bash
git add Girvs/Infrastructure/ServiceNameResolver.cs tests/Girvs.Aspire.Hosting.Tests/ServiceNameResolverTests.cs
git commit -m "feat: ServiceNameResolver 新增服务名归一化方法"
```

---

### Task 2: 服务治理配置模型与 Consul/模块改造（FR-1 ~ FR-5、FR-8）

本任务在 `Girvs.ServiceGovernance` 编译单元内互锁，按"改测试 → 改实现 → 修引用 → 全绿"推进。

**Files:**
- Modify: `Girvs.ServiceGovernance/Configuration/ServiceGovernanceConfig.cs`
- Modify: `Girvs.ServiceGovernance/Services/ConsulServiceRegistrar.cs`
- Modify: `Girvs.ServiceGovernance/ServiceGovernanceModule.cs`
- Test: `tests/Girvs.ServiceGovernance.Tests/ServiceGovernanceConfigTests.cs`
- Test: `tests/Girvs.ServiceGovernance.Tests/ConsulServiceRegistrarTests.cs`
- Test: `tests/Girvs.ServiceGovernance.Tests/ServiceGovernanceModuleTests.cs`

**Interfaces:**
- Consumes: `ServiceNameResolver.FromServerName`（Task 1）。
- Produces:
  - `ServiceGovernanceConfig.ConsulRegistrationAddress`（`string?`，默认 `"http://127.0.0.1"`）、`HealthCheckPath`（默认 `"/health"`）、`LivenessCheckPath`（默认 `"/alive"`）；移除 `HealthAddress`。
  - `ConsulServiceRegistrar.CreateWebApiRegistration(config)`：`Check.HTTP = {SchemeAndServer}{HealthCheckPath}`，`Address`/`Port` 取自 `ConsulRegistrationAddress`。
  - `ConsulServiceRegistrar.CreateGrpcRegistration(config)`：`Check.GRPC = {host}:{port}`（不含路径）。
  - `ServiceGovernanceModule.ConfigureMapEndpointRoute(builder)`：按 `config.HealthCheckPath` / `config.LivenessCheckPath` 映射。

- [ ] **Step 1: 更新配置默认值测试（红）**

`tests/Girvs.ServiceGovernance.Tests/ServiceGovernanceConfigTests.cs` 的 `默认配置使用Aspire和WebApi模式` 中替换断言：

```csharp
Assert.Equal("http://127.0.0.1", config.ConsulRegistrationAddress);
Assert.Equal("/health", config.HealthCheckPath);
Assert.Equal("/alive", config.LivenessCheckPath);
```

（删除 `Assert.Equal("http://127.0.0.1/health", config.HealthAddress);`）。在 `ModuleConfigurations节点绑定Consul模式` 的 `AddInMemoryCollection` 字典中追加：

```csharp
["ModuleConfigurations:ServiceGovernanceConfig:ConsulRegistrationAddress"] = "http://127.0.0.1:5080",
["ModuleConfigurations:ServiceGovernanceConfig:HealthCheckPath"] = "/healthz",
["ModuleConfigurations:ServiceGovernanceConfig:LivenessCheckPath"] = "/alivez",
```

并在末尾追加断言：

```csharp
Assert.Equal("http://127.0.0.1:5080", config.ConsulRegistrationAddress);
Assert.Equal("/healthz", config.HealthCheckPath);
Assert.Equal("/alivez", config.LivenessCheckPath);
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test tests/Girvs.ServiceGovernance.Tests/Girvs.ServiceGovernance.Tests.csproj --filter FullyQualifiedName~ServiceGovernanceConfigTests --nologo`

Expected: 断言失败（默认值仍为旧 `HealthAddress` 语义）。

- [ ] **Step 3: 实现配置模型（FR-1）**

`Girvs.ServiceGovernance/Configuration/ServiceGovernanceConfig.cs` 删除：

```csharp
public string HealthAddress { get; set; } = "http://127.0.0.1/health";
```

替换为：

```csharp
/// <summary>服务对外注册基址（仅 Consul 模式生效），如 http://127.0.0.1:8080，不含路径；为空时告警并跳过注册。</summary>
public string? ConsulRegistrationAddress { get; set; } = "http://127.0.0.1";

/// <summary>完整健康验证路径（readiness/部署后验证），默认 /health；Consul HTTP 检查、Aspire Readiness probe、K8s readinessProbe、框架端点映射共用。</summary>
public string HealthCheckPath { get; set; } = "/health";

/// <summary>存活探测路径（liveness），默认 /alive；Aspire Liveness probe、K8s livenessProbe、框架端点映射使用；Consul 模式忽略。</summary>
public string LivenessCheckPath { get; set; } = "/alive";
```

- [ ] **Step 4: 改造 ConsulServiceRegistrar（FR-2/3/8）**

`Girvs.ServiceGovernance/Services/ConsulServiceRegistrar.cs`：

将 `CreateWebApiRegistration` 与 `CreateGrpcRegistration` 中 `var healthUri = new Uri(config.HealthAddress);` 替换为 `var healthUri = ResolveRegistrationUri(config);`；将两个方法中的 `HTTP = config.HealthAddress` 与 `GRPC = config.HealthAddress.Replace(...)` 分别替换：

```csharp
internal static AgentServiceRegistration CreateWebApiRegistration(
    ServiceGovernanceConfig config
)
{
    var healthUri = ResolveRegistrationUri(config);
    var checkUrl =
        $"{healthUri.GetComponents(UriComponents.SchemeAndServer, UriFormat.UriEscaped)}{config.HealthCheckPath}";
    return CreateRegistration(
        config,
        healthUri,
        ".net Core WebApiService",
        new AgentServiceCheck
        {
            DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(
                config.DeregisterCriticalServiceAfter
            ),
            Interval = TimeSpan.FromSeconds(config.Interval),
            HTTP = checkUrl,
            Timeout = TimeSpan.FromSeconds(config.Timeout),
        }
    );
}

internal static AgentServiceRegistration CreateGrpcRegistration(
    ServiceGovernanceConfig config
)
{
    var healthUri = ResolveRegistrationUri(config);
    return CreateRegistration(
        config,
        healthUri,
        ".net Core GrpcService",
        new AgentServiceCheck
        {
            DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(
                config.DeregisterCriticalServiceAfter
            ),
            Interval = TimeSpan.FromSeconds(config.Interval),
            GRPC = $"{healthUri.Host}:{healthUri.Port}",
            Timeout = TimeSpan.FromSeconds(config.Timeout),
        }
    );
}

private static Uri ResolveRegistrationUri(ServiceGovernanceConfig config)
{
    if (string.IsNullOrWhiteSpace(config.ConsulRegistrationAddress))
        throw new GirvsException("ConsulRegistrationAddress 为空，无法生成 Consul 注册信息");
    if (
        !Uri.TryCreate(config.ConsulRegistrationAddress, UriKind.Absolute, out var uri)
        || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
    )
        throw new GirvsException(
            $"ConsulRegistrationAddress 必须为合法 http/https 绝对 URI：{config.ConsulRegistrationAddress}"
        );
    return uri;
}
```

将 `GetServerName` 改为归一化（FR-8）：

```csharp
private static string GetServerName(ServiceGovernanceConfig config) =>
    string.IsNullOrWhiteSpace(config.ServerName)
        ? ServiceNameResolver.FromAssemblyName()
        : ServiceNameResolver.FromServerName(config.ServerName);
```

若文件顶部无 `using Girvs;`（`GirvsException` 所在命名空间），在文件 using 区追加 `using Girvs;`。

- [ ] **Step 5: 改造 ServiceGovernanceModule（FR-4/5）**

`Girvs.ServiceGovernance/ServiceGovernanceModule.cs`：

在 `Configure` 中 `ConsulAddress` 空值检查之后、`Register(config)` 之前追加：

```csharp
if (string.IsNullOrWhiteSpace(config.ConsulRegistrationAddress))
{
    logger.LogWarning("Consul 模式已启用但 ConsulRegistrationAddress 为空，跳过服务注册");
    return;
}

if (
    !Uri.TryCreate(config.ConsulRegistrationAddress, UriKind.Absolute, out var registrationUri)
    || (registrationUri.Scheme != Uri.UriSchemeHttp && registrationUri.Scheme != Uri.UriSchemeHttps)
)
{
    throw new GirvsException(
        $"ConsulRegistrationAddress 必须为合法 http/https 绝对 URI：{config.ConsulRegistrationAddress}"
    );
}
```

将 `ConfigureMapEndpointRoute` 改为配置驱动：

```csharp
public void ConfigureMapEndpointRoute(IEndpointRouteBuilder builder)
{
    var config = Singleton<AppSettings>.Instance.Get<ServiceGovernanceConfig>();
    builder.MapHealthChecks(config.HealthCheckPath);
    builder.MapHealthChecks(
        config.LivenessCheckPath,
        new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("live"),
        }
    );
}
```

- [ ] **Step 6: 更新 ConsulServiceRegistrarTests**

`tests/Girvs.ServiceGovernance.Tests/ConsulServiceRegistrarTests.cs`：

`CreateConfig` 中 `HealthAddress = "http://127.0.0.1:5080/health"` 替换为：

```csharp
ConsulRegistrationAddress = "http://127.0.0.1:5080",
```

`Grpc注册信息使用标准GrpcHealth地址` 断言改为规范组合（FR-3）：

```csharp
Assert.Equal("127.0.0.1:5080", registration.Check.GRPC);
```

WebApi 断言 `registration.Check.HTTP == "http://127.0.0.1:5080/health"` 保持不变（`HealthCheckPath` 默认 `/health`）。追加归一化测试（FR-8）：

```csharp
[Fact]
public void 服务名使用ServerName归一化()
{
    var config = CreateConfig(ConsulServerModel.WebApi);
    config.ServerName = "Sample.ServiceA";

    var registration = ConsulServiceRegistrar.CreateWebApiRegistration(config);

    Assert.Equal("sample-servicea", registration.Name);
}
```

追加非法注册地址测试（FR-5）：

```csharp
[Fact]
public void ConsulRegistrationAddress非法时抛出GirvsException()
{
    var config = CreateConfig(ConsulServerModel.WebApi);
    config.ConsulRegistrationAddress = "not-a-uri";

    var exception = Record.Exception(
        () => ConsulServiceRegistrar.CreateWebApiRegistration(config)
    );

    Assert.IsType<GirvsException>(exception);
}
```

- [ ] **Step 7: 更新 ServiceGovernanceModuleTests**

`tests/Girvs.ServiceGovernance.Tests/ServiceGovernanceModuleTests.cs`：

`ConfigureMapEndpointRoute映射health和alive端点` 方法体开头追加 `SetupSingletonAppSettings();`（否则 `Get<ServiceGovernanceConfig>()` 抛异常）。追加两个测试：

```csharp
[Fact]
public async Task ConfigureMapEndpointRoute使用自定义HealthCheckPath()
{
    SetupSingletonAppSettings(
        new ServiceGovernanceConfig { HealthCheckPath = "/healthz", LivenessCheckPath = "/alivez" }
    );
    var builder = WebApplication.CreateBuilder();
    builder.Services.AddHealthChecks();
    await using var application = builder.Build();

    new ServiceGovernanceModule().ConfigureMapEndpointRoute(application);

    var patterns = ((IEndpointRouteBuilder)application)
        .DataSources.SelectMany(source => source.Endpoints)
        .OfType<RouteEndpoint>()
        .Select(endpoint => endpoint.RoutePattern.RawText)
        .ToArray();
    Assert.Contains("/healthz", patterns);
    Assert.Contains("/alivez", patterns);
    Assert.DoesNotContain("/health", patterns);
}

[Fact]
public void ConsulRegistrationAddress为空时跳过注册()
{
    var config = CreateConsulConfigWithEmptyRegistrationAddress();
    SetupSingletonAppSettings(config);
    var registrar = new CountingConsulServiceRegistrar();
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddSingleton<IConsulServiceRegistrar>(registrar);
    var application = new ApplicationBuilder(services.BuildServiceProvider());

    var exception = Record.Exception(
        () => new ServiceGovernanceModule().Configure(application, null)
    );

    Assert.Null(exception);
    Assert.Equal(0, registrar.RegisterCount);
}

private static ServiceGovernanceConfig CreateConsulConfigWithEmptyRegistrationAddress() =>
    new()
    {
        ServiceDiscoveryProvider = ServiceDiscoveryProvider.Consul,
        ConsulAddress = "http://127.0.0.1:8500",
        ConsulRegistrationAddress = "",
    };
```

- [ ] **Step 8: 运行测试确认通过**

Run: `dotnet test tests/Girvs.ServiceGovernance.Tests/Girvs.ServiceGovernance.Tests.csproj --nologo`

Expected: 全部 PASS（含新增/修改用例）。

- [ ] **Step 9: 提交**

```bash
git add Girvs.ServiceGovernance/Configuration/ServiceGovernanceConfig.cs Girvs.ServiceGovernance/Services/ConsulServiceRegistrar.cs Girvs.ServiceGovernance/ServiceGovernanceModule.cs tests/Girvs.ServiceGovernance.Tests/
git commit -m "refactor: 服务治理健康检查配置拆分三属性并规范 Consul 检查组合"
```

---

### Task 3: Aspire Hosting 探针接线与服务名读取（FR-6、FR-7）

**Files:**
- Create: `Girvs.Aspire.Hosting/GirvsServiceProjectConfig.cs`
- Modify: `Girvs.Aspire.Hosting/GirvsProjectExtensions.cs`
- Test: `tests/Girvs.Aspire.Hosting.Tests/GirvsProjectExtensionsTests.cs`

**Interfaces:**
- Consumes: `ServiceNameResolver.FromServerName`（Task 1）、`ServiceGovernanceConfig` 三属性（Task 2，仅节点名约定）。
- Produces:
  - `internal sealed record ServiceProjectSettings { string? ServerName; string HealthCheckPath = "/health"; string LivenessCheckPath = "/alive"; }`
  - `internal static class GirvsServiceProjectConfig.ReadFromAppSettings(IProjectMetadata)` / `ReadFromAppSettings(IResourceBuilder<ProjectResource>)`。
  - `AddGirvsProject<TProject>()` 无参重载：读 `ServerName`（归一化）优先，回退类名转换；`AddGirvsProject<TProject>(name)` 显式传名最高优先（不变）；内部重载为所有入口声明 Readiness/Liveness 双探针。

- [ ] **Step 1: 写失败测试**

`tests/Girvs.Aspire.Hosting.Tests/GirvsProjectExtensionsTests.cs` 追加（文件顶部补 `using Aspire.Hosting.ApplicationModel;`，必要时补 `using Aspire.Hosting;`）：

```csharp
[Fact]
public void 无参AddGirvsProject_读取appsettings的ServerName作为资源名()
{
    var builder = CreateBuilder();
    var projectPath = Path.Combine(_tempRoot, "Sample.ServiceA.csproj");
    File.WriteAllText(projectPath, "<Project Sdk=\"Microsoft.NET.Sdk.Web\"></Project>");
    File.WriteAllText(
        Path.Combine(_tempRoot, "appsettings.json"),
        """{"ModuleConfigurations":{"ServiceGovernanceConfig":{"ServerName":"My.Service.A"}}}"""
    );
    Sample_ServiceA.ProjectFilePath = projectPath;

    var project = builder.AddGirvsProject<Sample_ServiceA>();

    Assert.Equal("my-service-a", project.Resource.Name);
}

[Fact]
public void 显式传名覆盖appsettings的ServerName()
{
    var builder = CreateBuilder();
    var projectPath = Path.Combine(_tempRoot, "Sample.ServiceA.csproj");
    File.WriteAllText(projectPath, "<Project Sdk=\"Microsoft.NET.Sdk.Web\"></Project>");
    File.WriteAllText(
        Path.Combine(_tempRoot, "appsettings.json"),
        """{"ModuleConfigurations":{"ServiceGovernanceConfig":{"ServerName":"My.Service.A"}}}"""
    );
    Sample_ServiceA.ProjectFilePath = projectPath;

    var project = builder.AddGirvsProject<Sample_ServiceA>("explicit-name");

    Assert.Equal("explicit-name", project.Resource.Name);
}

[Fact]
public void AddGirvsProject_声明Readiness与Liveness探针()
{
    var builder = CreateBuilder();
    var project = AddServiceProject(builder, "service-a");
    builder.AddGirvsProject(project);

    var probes = project.Resource.Annotations.OfType<EndpointProbeAnnotation>().ToList();
    Assert.Contains(probes, p => p.Type == ProbeType.Readiness && p.Path == "/health");
    Assert.Contains(probes, p => p.Type == ProbeType.Liveness && p.Path == "/alive");
}

[Fact]
public void AddGirvsProject_读取appsettings的自定义健康检查路径()
{
    var builder = CreateBuilder();
    var project = AddServiceProject(builder, "service-a");
    File.WriteAllText(
        Path.Combine(_tempRoot, "appsettings.json"),
        """{"ModuleConfigurations":{"ServiceGovernanceConfig":{"HealthCheckPath":"/healthz","LivenessCheckPath":"/alivez"}}}"""
    );
    builder.AddGirvsProject(project);

    var probes = project.Resource.Annotations.OfType<EndpointProbeAnnotation>().ToList();
    Assert.Contains(probes, p => p.Type == ProbeType.Readiness && p.Path == "/healthz");
    Assert.Contains(probes, p => p.Type == ProbeType.Liveness && p.Path == "/alivez");
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test tests/Girvs.Aspire.Hosting.Tests/Girvs.Aspire.Hosting.Tests.csproj --filter "FullyQualifiedName~GirvsProjectExtensionsTests" --nologo`

Expected: 编译失败，提示 `GirvsServiceProjectConfig`/新断言相关类型缺失或探针断言失败。

- [ ] **Step 3: 创建服务项目配置读取器**

新建 `Girvs.Aspire.Hosting/GirvsServiceProjectConfig.cs`：

```csharp
using System.Text.Json;
using Aspire.Hosting.ApplicationModel;

namespace Girvs.Aspire.Hosting;

/// <summary>AppHost 视角的服务治理配置（仅文件型 appsettings.json；环境变量/ConfigMap 注入值读取不到）。</summary>
public sealed record ServiceProjectSettings
{
    public string? ServerName { get; set; }
    public string HealthCheckPath { get; set; } = "/health";
    public string LivenessCheckPath { get; set; } = "/alive";
}

/// <summary>从服务项目 appsettings.json 读取 ModuleConfigurations:ServiceGovernanceConfig 节点。</summary>
internal static class GirvsServiceProjectConfig
{
    public static ServiceProjectSettings ReadFromAppSettings(IProjectMetadata metadata) =>
        ReadFromAppSettings(Path.GetDirectoryName(metadata.ProjectPath));

    public static ServiceProjectSettings ReadFromAppSettings(
        IResourceBuilder<ProjectResource> project
    ) => ReadFromAppSettings(project.Resource.GetProjectMetadata());

    private static ServiceProjectSettings ReadFromAppSettings(string? projectDirectory)
    {
        var settings = new ServiceProjectSettings();
        if (string.IsNullOrWhiteSpace(projectDirectory))
            return settings;
        var appSettingsPath = Path.Combine(projectDirectory, "appsettings.json");
        if (!File.Exists(appSettingsPath))
            return settings;

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(appSettingsPath));
            var root = document.RootElement;
            if (
                root.TryGetProperty("ModuleConfigurations", out var modules)
                && modules.TryGetProperty("ServiceGovernanceConfig", out var governance)
            )
            {
                if (
                    governance.TryGetProperty("ServerName", out var serverName)
                    && serverName.ValueKind == JsonValueKind.String
                )
                    settings.ServerName = serverName.GetString();
                if (
                    governance.TryGetProperty("HealthCheckPath", out var healthPath)
                    && healthPath.ValueKind == JsonValueKind.String
                )
                    settings.HealthCheckPath = healthPath.GetString()!;
                if (
                    governance.TryGetProperty("LivenessCheckPath", out var livenessPath)
                    && livenessPath.ValueKind == JsonValueKind.String
                )
                    settings.LivenessCheckPath = livenessPath.GetString()!;
            }
        }
        catch (JsonException)
        {
            // appsettings.json 解析失败按缺省值处理，不阻断编排
        }

        return settings;
    }
}
```

- [ ] **Step 4: 改造 GirvsProjectExtensions（FR-6/7）**

`Girvs.Aspire.Hosting/GirvsProjectExtensions.cs`：

无参重载改为读 `ServerName`（文件顶部补 `using Aspire.Hosting.ApplicationModel;`）：

```csharp
public static IResourceBuilder<ProjectResource> AddGirvsProject<TProject>(
    this IDistributedApplicationBuilder builder
)
    where TProject : IProjectMetadata, new()
{
    var metadata = new TProject();
    var settings = GirvsServiceProjectConfig.ReadFromAppSettings(metadata);
    var name = string.IsNullOrWhiteSpace(settings.ServerName)
        ? ServiceNameResolver.FromProjectMetadataName(typeof(TProject).Name)
        : ServiceNameResolver.FromServerName(settings.ServerName);
    return builder.AddGirvsProject(builder.AddProject<TProject>(name));
}
```

内部重载开头（`IsPublishMode` 判断之前）追加探针接线：

```csharp
internal static IResourceBuilder<ProjectResource> AddGirvsProject(
    this IDistributedApplicationBuilder builder,
    IResourceBuilder<ProjectResource> project
)
{
    var settings = GirvsServiceProjectConfig.ReadFromAppSettings(project);
#pragma warning disable ASPIREPROBES001 // 实验性探针 API（Aspire 13 默认，K8s 发布器消费）
    project.WithHttpProbe(ProbeType.Readiness, settings.HealthCheckPath);
    project.WithHttpProbe(ProbeType.Liveness, settings.LivenessCheckPath);
#pragma warning restore ASPIREPROBES001

    if (builder.ExecutionContext.IsPublishMode)
    {
        // 生产:共享文件由 K8s ConfigMap 挂载到约定路径,内容与地址由运维维护
        project.WithEnvironment(
            GirvsSharedConfigFile.EnvName,
            GirvsSharedConfigFile.PublishMountPath
        );
        return project;
    }

    // ……其余 WaitFor / WithEnvironment 逻辑保持不变……
}
```

- [ ] **Step 5: 运行测试确认通过**

Run: `dotnet test tests/Girvs.Aspire.Hosting.Tests/Girvs.Aspire.Hosting.Tests.csproj --nologo`

Expected: 全部 PASS（含新增 4 个用例与既有 `无参AddGirvsProject_项目元数据类型名_使用统一服务名`——未配 appsettings 时回退行为不变）。

- [ ] **Step 6: 提交**

```bash
git add Girvs.Aspire.Hosting/GirvsServiceProjectConfig.cs Girvs.Aspire.Hosting/GirvsProjectExtensions.cs tests/Girvs.Aspire.Hosting.Tests/GirvsProjectExtensionsTests.cs
git commit -m "feat: Aspire 编排声明健康检查探针并支持 ServerName 配置资源名"
```

---

### Task 4: 集成验证与收尾

**Files:**
- 无代码改动（仅验证 + 文档迁移说明）。

**Interfaces:**
- Consumes: Task 1~3 全部产物。

- [ ] **Step 1: 全量构建**

Run: `dotnet build Girvs.slnx --nologo`

Expected: 全部项目成功。

- [ ] **Step 2: 全量测试**

Run: `dotnet test Girvs.slnx --nologo`

Expected: 全部通过（重点：`Girvs.ServiceGovernance.Tests`、`Girvs.Aspire.Hosting.Tests`）。

- [ ] **Step 3: HealthAddress 残留检查**

Run: `rg -n "HealthAddress" --glob "!docs/superpowers/**" .`

Expected: 无输出（或仅命中"本次 Spec/Plan 的文档"说明性文字；代码与配置零残留）。

- [ ] **Step 4: Aspire publish 清单探针验证（手工可选）**

Run: `dotnet run --project samples/Sample.AppHost -- --operation publish --publisher manifest --output-path d:\Temp\opencode\aspire-manifest`

Expected: 生成的 `manifest.json`（若含 project 资源）中带 `probes` 相关声明（`/health`、`/alive` 与 `EndpointProbeAnnotation` 一致）。若本地环境不具备 publish 运行条件，以 Step 2 的 `AddGirvsProject_声明Readiness与Liveness探针` 单测作为替代验收证据。

- [ ] **Step 5: 迁移说明与示例核对**

Run: `rg -n "HealthAddress|ConsulRegistrationAddress|HealthCheckPath|LivenessCheckPath" samples tests --type json --type cs`

Expected:
- `samples/*/appsettings.json` 无 `HealthAddress` 残留（现状未配置该节点，无需修改）；
- 若某示例需演示新配置，在 `samples/Sample.ServiceA/appsettings.json` 的 `ModuleConfigurations` 追加 `ServiceGovernanceConfig` 节点（可选，不阻塞验收）。

- [ ] **Step 6: 提交（如有文档/示例改动）**

```bash
git add samples/ docs/
git commit -m "docs: 服务治理健康检查与服务名配置迁移说明"
```

---

## Self-Review 结果

- **Spec 覆盖**：FR-1（Task 2 Step 3）、FR-2/3（Task 2 Step 4）、FR-4/5（Task 2 Step 5/7）、FR-6/7（Task 3）、FR-8（Task 1 + Task 2 Step 4/6）、验收 1-7（Task 4）全部对应任务。
- **无占位符**：所有步骤含精确代码、命令与预期。
- **类型一致性**：`ServiceNameResolver.FromServerName(string)` 由 Task 1 定义，Task 2/3 一致引用；`ServiceProjectSettings` 属性名在 Task 3 内一致（`ServerName`/`HealthCheckPath`/`LivenessCheckPath`）；探针注解使用 `EndpointProbeAnnotation.Type`/`.Path`（Aspire 13.4.6 实证，非 `HealthProbeAnnotation`）。

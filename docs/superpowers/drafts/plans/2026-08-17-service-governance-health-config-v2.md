---
plan_id: 2026-08-17-service-governance-health-config-v2
artifact_version: 2.0.0
workflow_status: approved
approved_at: 2026-08-17
spec_path: docs/superpowers/specs/2026-08-17-service-governance-health-config-design.md
handoff_status: ready_for_implementation
supersedes: 2026-08-17-service-governance-health-config
change_request_path: docs/superpowers/change-requests/2026-08-17-service-governance-health-config-02.md
---

# 服务治理健康检查与服务名配置重构 Implementation Plan（v2）

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 按变更申请 02 修正 v1 范围：`HealthAddress` 零残留验收收窄至 `Girvs.ServiceGovernance`；Aspire Hosting 不再自动新增 HTTP/HTTPS endpoint（无可用 endpoint 时抛 `GirvsException`）；`HealthCheckPath` / `LivenessCheckPath` 必须非空且以 `/` 开头（服务侧与 AppHost 侧均校验）。完成剩余未落地改造并全量回归。

**Architecture:** 服务侧（`Girvs.ServiceGovernance`）在 `ConsulServiceRegistrar` 与 `ServiceGovernanceModule` 增加路径格式校验；AppHost 侧（`Girvs.Aspire.Hosting`）修正 `GirvsProjectExtensions` 的 endpoint 选择策略，并在 `GirvsServiceProjectConfig` 读取配置时校验路径格式。各层独立任务、独立测试周期。

**Tech Stack:** .NET 10、Aspire Hosting 13.4.6、Consul 1.7.14.9、xUnit。

## Global Constraints

- Breaking 变更：删除 `HealthAddress`，不保留任何兼容读取/反解；**`HealthAddress` 零残留验收仅适用于 `Girvs.ServiceGovernance` 模块**（`Girvs.Consul` 旧模块不在本次范围）。
- 归一化规则（`ServiceNameResolver.FromServerName`）：`.` 与 `_` → `-`、全部小写化；对已是"小写字母、数字、连字符"的规范配置幂等。
- `ConsulRegistrationAddress` 必须为合法 `http`/`https` 绝对 URI。
- **`HealthCheckPath` / `LivenessCheckPath` 必须非空且以 `/` 开头**；非法时服务侧与 AppHost 侧均抛 `GirvsException`。
- **Aspire Hosting 不自动新增 HTTP/HTTPS endpoint**；项目无可用 HTTP/HTTPS endpoint 时抛 `GirvsException`，明确提示配置错误。
- `ServerName` 读取边界：AppHost 仅读取服务项目目录的 `appsettings.json` 文件配置；环境变量 / ConfigMap 注入值读取不到，由 `AddGirvsProject<T>(name)` 显式传名兜底。
- Aspire 探针 API 为实验性（诊断 `ASPIREPROBES001`）：使用 `WithHttpProbe` / `ProbeType` 处须 `#pragma warning disable ASPIREPROBES001`。
- 依赖版本：`Aspire.Hosting` 13.4.6；无新增 NuGet 包。
- 测试框架 xUnit，测试方法名用中文描述行为与预期；提交信息用 Conventional Commits 中文摘要并标注模块。

---

## 已完成任务（仅上下文，不重复步骤）

### Task 1（已完成）：核心库服务名归一化

- [x] `Girvs/Infrastructure/ServiceNameResolver.cs` 已新增 `FromServerName(string)`。
- [x] 测试 `tests/Girvs.Aspire.Hosting.Tests/ServiceNameResolverTests.cs` 已通过。

### Task 2（已完成）：服务治理配置模型与 Consul/模块改造

- [x] `Girvs.ServiceGovernance/Configuration/ServiceGovernanceConfig.cs`：已删除 `HealthAddress`，新增 `ConsulRegistrationAddress`、`HealthCheckPath`、`LivenessCheckPath`。
- [x] `Girvs.ServiceGovernance/Services/ConsulServiceRegistrar.cs`：已实现 WebApi/gRPC 新组合与 `ServerName` 归一化。
- [x] `Girvs.ServiceGovernance/ServiceGovernanceModule.cs`：已实现空地址 Warning 跳过、非法 URI `GirvsException`、配置驱动端点映射。
- [x] ServiceGovernance 测试 35/35 通过。

---

### Task 3: 服务侧健康路径格式校验

**Files:**
- Modify: `Girvs.ServiceGovernance/ServiceGovernanceModule.cs`
- Modify: `Girvs.ServiceGovernance/Services/ConsulServiceRegistrar.cs`
- Test: `tests/Girvs.ServiceGovernance.Tests/ConsulServiceRegistrarTests.cs`
- Test: `tests/Girvs.ServiceGovernance.Tests/ServiceGovernanceModuleTests.cs`

**Interfaces:**
- Consumes: Task 2 产物（`ServiceGovernanceConfig` 三属性）。
- Produces: `HealthCheckPath` / `LivenessCheckPath` 非空且以 `/` 开头的校验，非法时抛 `GirvsException`。

- [ ] **Step 1: 写失败测试**

`tests/Girvs.ServiceGovernance.Tests/ConsulServiceRegistrarTests.cs` 追加：

```csharp
[Theory]
[InlineData("health")]
[InlineData("")]
[InlineData(null)]
public void HealthCheckPath不以斜杠开头时抛出GirvsException(string? path)
{
    var config = CreateConfig(ConsulServerModel.WebApi);
    config.HealthCheckPath = path!;

    var exception = Record.Exception(
        () => ConsulServiceRegistrar.CreateWebApiRegistration(config)
    );

    Assert.IsType<GirvsException>(exception);
    Assert.Contains("HealthCheckPath", exception.Message);
}
```

`tests/Girvs.ServiceGovernance.Tests/ServiceGovernanceModuleTests.cs` 追加：

```csharp
[Fact]
public void LivenessCheckPath不以斜杠开头时抛出GirvsException()
{
    var config = new ServiceGovernanceConfig
    {
        ServiceDiscoveryProvider = ServiceDiscoveryProvider.Consul,
        ConsulAddress = "http://127.0.0.1:8500",
        ConsulRegistrationAddress = "http://127.0.0.1:5080",
        LivenessCheckPath = "alive",
    };
    SetupSingletonAppSettings(config);
    var services = new ServiceCollection();
    services.AddLogging();
    var application = new ApplicationBuilder(services.BuildServiceProvider());

    var exception = Record.Exception(
        () => new ServiceGovernanceModule().Configure(application, null)
    );

    Assert.IsType<GirvsException>(exception);
    Assert.Contains("LivenessCheckPath", exception.Message);
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test tests/Girvs.ServiceGovernance.Tests/Girvs.ServiceGovernance.Tests.csproj --filter "FullyQualifiedName~Path不以斜杠" --nologo`

Expected: 两个新测试失败（当前未校验路径格式）。

- [ ] **Step 3: 实现路径校验**

在 `Girvs.ServiceGovernance/Services/ConsulServiceRegistrar.cs` 的 `CreateWebApiRegistration` 开头（`ResolveRegistrationUri` 之后）追加：

```csharp
ValidateHealthCheckPath(config.HealthCheckPath);
```

在 `ConsulServiceRegistrar` 中新增私有方法：

```csharp
private static void ValidateHealthCheckPath(string path)
{
    if (string.IsNullOrWhiteSpace(path) || !path.StartsWith('/'))
        throw new GirvsException(
            $"HealthCheckPath 必须非空且以 / 开头，当前值：{path}"
        );
}
```

在 `Girvs.ServiceGovernance/ServiceGovernanceModule.cs` 的 `Configure` 中（ConsulRegistrationAddress 校验之后、`Register(config)` 之前）追加：

```csharp
ValidateHealthPath(config.HealthCheckPath, nameof(config.HealthCheckPath));
ValidateHealthPath(config.LivenessCheckPath, nameof(config.LivenessCheckPath));

```

在 `ServiceGovernanceModule` 中新增私有方法：

```csharp
private static void ValidateHealthPath(string path, string name)
{
    if (string.IsNullOrWhiteSpace(path) || !path.StartsWith('/'))
        throw new GirvsException(
            $"{name} 必须非空且以 / 开头，当前值：{path}"
        );
}
```

- [ ] **Step 4: 运行测试确认通过**

Run: `dotnet test tests/Girvs.ServiceGovernance.Tests/Girvs.ServiceGovernance.Tests.csproj --nologo`

Expected: 全部 PASS（含新增路径校验用例）。

- [ ] **Step 5: 提交**

```bash
git add Girvs.ServiceGovernance/Services/ConsulServiceRegistrar.cs Girvs.ServiceGovernance/ServiceGovernanceModule.cs tests/Girvs.ServiceGovernance.Tests/
git commit -m "feat: 服务治理健康检查路径增加格式校验"
```

---

### Task 4: Aspire Hosting endpoint 策略修正与路径校验

**Files:**
- Modify: `Girvs.Aspire.Hosting/GirvsProjectExtensions.cs`
- Modify: `Girvs.Aspire.Hosting/GirvsServiceProjectConfig.cs`
- Test: `tests/Girvs.Aspire.Hosting.Tests/GirvsProjectExtensionsTests.cs`

**Interfaces:**
- Consumes: Task 1 `ServiceNameResolver.FromServerName`。
- Produces: 无可用 HTTP/HTTPS endpoint 时抛 `GirvsException`；路径格式校验。

- [ ] **Step 1: 写失败测试**

`tests/Girvs.Aspire.Hosting.Tests/GirvsProjectExtensionsTests.cs`：

将现有的"无 endpoint 自动补充 http"测试改为断言抛异常：

```csharp
[Fact]
public void AddGirvsProject_无可用HTTPEndpoint_抛出GirvsException()
{
    var builder = CreateBuilder();
    var projectPath = Path.Combine(_tempRoot, "Sample.ServiceA.csproj");
    File.WriteAllText(projectPath, "<Project Sdk=\"Microsoft.NET.Sdk.Web\"></Project>");
    Sample_ServiceA.ProjectFilePath = projectPath;

    var exception = Assert.Throws<GirvsException>(
        () => builder.AddGirvsProject<Sample_ServiceA>()
    );

    Assert.Contains("HTTP", exception.Message);
}
```

新增路径校验测试：

```csharp
[Fact]
public void AddGirvsProject_HealthCheckPath不以斜杠开头_抛出GirvsException()
{
    var builder = CreateBuilder();
    var project = AddServiceProject(builder, "service-a");
    File.WriteAllText(
        Path.Combine(_tempRoot, "appsettings.json"),
        """{"ModuleConfigurations":{"ServiceGovernanceConfig":{"HealthCheckPath":"health"}}}"""
    );

    var exception = Assert.Throws<GirvsException>(
        () => builder.AddGirvsProject(project)
    );

    Assert.Contains("HealthCheckPath", exception.Message);
}
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test tests/Girvs.Aspire.Hosting.Tests/Girvs.Aspire.Hosting.Tests.csproj --filter "FullyQualifiedName~无可用HTTP|FullyQualifiedName~HealthCheckPath不以斜杠" --nologo`

Expected: 两个新测试失败（当前自动补充 endpoint 而非抛异常；当前未校验路径格式）。

- [ ] **Step 3: 修正 GirvsProjectExtensions endpoint 策略**

`Girvs.Aspire.Hosting/GirvsProjectExtensions.cs` 内部 `AddGirvsProject` 方法，将第 44-53 行（自动新增 endpoint 的 if 块）替换为：

```csharp
var selectedEndpoint = project.Resource.Annotations
    .OfType<EndpointAnnotation>()
    .FirstOrDefault(endpoint => endpoint.UriScheme is "http" or "https");
if (selectedEndpoint is null)
{
    throw new GirvsException(
        $"项目 {project.Resource.Name} 没有可用的 HTTP/HTTPS endpoint，无法声明健康检查探针；请在项目中配置 launchSettings.json 的 http 端点或显式声明 WithHttpEndpoint"
    );
}
```

文件顶部补 `using Girvs;`（`GirvsException` 所在命名空间，若尚无）。

- [ ] **Step 4: GirvsServiceProjectConfig 增加路径校验**

`Girvs.Aspire.Hosting/GirvsServiceProjectConfig.cs` 的 `ReadFromAppSettings` 私有方法返回 `settings` 之前追加：

```csharp
ValidateHealthPath(settings.HealthCheckPath, nameof(settings.HealthCheckPath));
ValidateHealthPath(settings.LivenessCheckPath, nameof(settings.LivenessCheckPath));
```

在 `GirvsServiceProjectConfig` 类中新增私有方法：

```csharp
private static void ValidateHealthPath(string path, string name)
{
    if (string.IsNullOrWhiteSpace(path) || !path.StartsWith('/'))
        throw new GirvsException(
            $"{name} 必须非空且以 / 开头，当前值：{path}"
        );
}
```

文件顶部补 `using Girvs;`（若尚无）。

- [ ] **Step 5: 运行测试确认通过**

Run: `dotnet test tests/Girvs.Aspire.Hosting.Tests/Girvs.Aspire.Hosting.Tests.csproj --nologo`

Expected: 全部 PASS（含新增 endpoint 抛异常用例和路径校验用例；既有 HTTPS/自定义名 endpoint 复用测试保持通过）。

- [ ] **Step 6: 提交**

```bash
git add Girvs.Aspire.Hosting/GirvsProjectExtensions.cs Girvs.Aspire.Hosting/GirvsServiceProjectConfig.cs tests/Girvs.Aspire.Hosting.Tests/GirvsProjectExtensionsTests.cs
git commit -m "fix: Aspire 探针不再自动新增 endpoint 并增加路径格式校验"
```

---

### Task 5: 集成验证与收尾

**Files:**
- 无代码改动（仅验证）。

**Interfaces:**
- Consumes: Task 1~4 全部产物。

- [ ] **Step 1: 全量构建**

Run: `dotnet build Girvs.slnx --nologo`

Expected: 全部项目成功。

- [ ] **Step 2: 全量测试**

Run: `dotnet test Girvs.slnx --nologo`

Expected: 全部通过。

- [ ] **Step 3: HealthAddress 残留检查（仅 Girvs.ServiceGovernance 范围）**

Run: `rg -n "HealthAddress" Girvs.ServiceGovernance/ tests/Girvs.ServiceGovernance.Tests/`

Expected: 无输出（`Girvs.ServiceGovernance` 模块零残留）。

注意：`Girvs.Consul` 旧模块的 `HealthAddress` 不在本次验收范围。

- [ ] **Step 4: Aspire publish 清单探针验证（手工可选）**

Run: `dotnet run --project samples/Sample.AppHost -- --operation publish --publisher manifest --output-path d:\Temp\opencode\aspire-manifest`

Expected: 生成的 `manifest.json` 中带 `probes` 相关声明。若本地环境不具备运行条件，以 Task 4 的探针单测作为替代验收证据。

---

## Self-Review 结果

- **Spec 覆盖**：变更申请 02 三项约束全部对应任务（零残留范围收窄→Task 5 Step 3；无 endpoint 抛错→Task 4 Step 3；路径校验→Task 3 + Task 4）。
- **无占位符**：所有步骤含精确代码、命令与预期。
- **类型一致性**：`GirvsException` 来自 `Girvs` 命名空间；`ValidateHealthPath` / `ValidateHealthCheckPath` 签名在服务侧与 AppHost 侧各自独立定义。

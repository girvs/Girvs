# 统一服务命名 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 让 Aspire AppHost、Consul 注册和 OpenAPI 网关地址使用同一小写连字符服务名。

**Architecture:** 在核心库提供无状态服务名转换器。Consul、OpenAPI 和 Hosting 扩展分别调用该转换器，示例改为无参 `AddGirvsProject`，从而以单一规则消除命名漂移。

**Tech Stack:** .NET 10、Aspire Hosting 13、xUnit、Microsoft OpenAPI。

## Global Constraints

- 服务名必须只含小写 ASCII 字母、数字和连字符。
- 程序集名的 `.` 与项目元数据类型名的 `_` 均转换为 `-`。
- 现有显式资源名重载必须保持兼容。

---

### Task 1: 核心服务名转换器

**Files:**
- Create: `Girvs/Infrastructure/ServiceNameResolver.cs`
- Modify: `tests/Girvs.Aspire.Hosting.Tests/GirvsProjectExtensionsTests.cs`

**Interfaces:**
- Produces: `public static string FromAssemblyName(string assemblyName)` 与 `public static string FromProjectMetadataName(string projectMetadataName)`。

- [ ] **Step 1: 写失败测试**

```csharp
[Theory]
[InlineData("Sample.ServiceA", "sample-servicea")]
public void FromAssemblyName_程序集名_返回服务名(string input, string expected) =>
    Assert.Equal(expected, ServiceNameResolver.FromAssemblyName(input));

[Theory]
[InlineData("Sample_ServiceA", "sample-servicea")]
public void FromProjectMetadataName_项目元数据类型名_返回服务名(string input, string expected) =>
    Assert.Equal(expected, ServiceNameResolver.FromProjectMetadataName(input));
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test tests/Girvs.Aspire.Hosting.Tests/Girvs.Aspire.Hosting.Tests.csproj --filter FullyQualifiedName~ServiceNameResolverTests --nologo`

Expected: 编译失败，提示 `ServiceNameResolver` 不存在。

- [ ] **Step 3: 实现最小转换器**

```csharp
public static class ServiceNameResolver
{
    public static string FromAssemblyName(string assemblyName) =>
        assemblyName.Replace(".", "-").ToLowerInvariant();

    public static string FromProjectMetadataName(string projectMetadataName) =>
        projectMetadataName.Replace("_", "-").ToLowerInvariant();
}
```

- [ ] **Step 4: 运行测试确认通过**

Run: `dotnet test tests/Girvs.Aspire.Hosting.Tests/Girvs.Aspire.Hosting.Tests.csproj --filter FullyQualifiedName~ServiceNameResolverTests --nologo`

Expected: PASS。

### Task 2: 复用规则并更新样例

**Files:**
- Modify: `Girvs.Aspire.Hosting/GirvsProjectExtensions.cs`
- Modify: `Girvs.Consul/ConsulModule.cs`
- Modify: `Girvs.Consul/Infrastructure/ApplicationExtensions/WebApiApplicationExtensions.cs`
- Modify: `Girvs.OpenApi/BearerSecuritySchemeTransformer.cs`
- Modify: `samples/Sample.AppHost/Program.cs`
- Modify: `samples/Sample.ServiceA/Controllers/SelfCheckController.cs`
- Modify: `samples/Sample.ServiceA/Clients/IServiceBRefit.cs`
- Modify: `tests/Girvs.Aspire.Hosting.Tests/GirvsProjectExtensionsTests.cs`

**Interfaces:**
- Consumes: `ServiceNameResolver.FromAssemblyName`、`ServiceNameResolver.FromProjectMetadataName`。
- Produces: 无参 `AddGirvsProject<TProject>(this IDistributedApplicationBuilder builder)`。

- [ ] **Step 1: 写失败测试**

```csharp
[Fact]
public void 无参AddGirvsProject_项目元数据类型名_使用统一服务名()
{
    var builder = CreateBuilder();
    var project = builder.AddGirvsProject<Sample_ServiceA>();
    Assert.Equal("sample-servicea", project.Resource.Name);
}

private sealed class Sample_ServiceA : IProjectMetadata { }
```

- [ ] **Step 2: 运行测试确认失败**

Run: `dotnet test tests/Girvs.Aspire.Hosting.Tests/Girvs.Aspire.Hosting.Tests.csproj --filter FullyQualifiedName~无参AddGirvsProject --nologo`

Expected: 编译失败，提示无参重载不存在。

- [ ] **Step 3: 实现最小修改**

```csharp
public static IResourceBuilder<ProjectResource> AddGirvsProject<TProject>(
    this IDistributedApplicationBuilder builder)
    where TProject : IProjectMetadata, new() =>
    builder.AddGirvsProject<TProject>(
        ServiceNameResolver.FromProjectMetadataName(typeof(TProject).Name));
```

将 Consul 两处回退逻辑和 OpenAPI 网关 Server 改为 `ServiceNameResolver.FromAssemblyName(AppDomain.CurrentDomain.FriendlyName)`；样例改用无参重载，并把 `service-b` 调用名替换为 `sample-serviceb`。

- [ ] **Step 4: 运行受影响测试确认通过**

Run: `dotnet test tests/Girvs.Aspire.Hosting.Tests/Girvs.Aspire.Hosting.Tests.csproj --nologo`

Expected: PASS。

### Task 3: 集成验证

**Files:**
- Modify: `tests/Girvs.Aspire.Hosting.Tests/SampleGatewayConsulContractTests.cs`（若样例服务名断言需要更新）

- [ ] **Step 1: 构建受影响项目**

Run: `dotnet build Girvs.Aspire.Hosting/Girvs.Aspire.Hosting.csproj --nologo && dotnet build Girvs.Consul/Girvs.Consul.csproj --nologo && dotnet build Girvs.OpenApi/Girvs.OpenApi.csproj --nologo && dotnet build samples/Sample.AppHost/Sample.AppHost.csproj --nologo`

Expected: 全部成功。

- [ ] **Step 2: 运行最终测试**

Run: `dotnet test tests/Girvs.Aspire.Hosting.Tests/Girvs.Aspire.Hosting.Tests.csproj --nologo && dotnet test tests/Girvs.Gateway.Tests/Girvs.Gateway.Tests.csproj --nologo`

Expected: 全部通过。

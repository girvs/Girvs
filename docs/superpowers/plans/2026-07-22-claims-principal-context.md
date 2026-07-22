# ClaimsPrincipal 身份上下文统一实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 删除 Girvs 自定义身份 DTO 与 ClaimManager，将 HTTP、JWT、EventBus 和底层数据访问统一到标准 `ClaimsPrincipal`。

**Architecture:** 核心层以 `IGirvsPrincipalAccessor` 提供无副作用的当前身份访问和 `AsyncLocal` 临时切换；AuthorizePermission 负责 HTTP 租户补充与 JWT 签发；EventBus 将白名单 Claim 序列化到版本化 CAP Header，并在消费 Scope 中重建 Principal。下游模块只通过 `ClaimsPrincipal` 扩展读取身份。

**Tech Stack:** .NET 8/9/10、ASP.NET Core Authentication、System.Security.Claims、DotNetCore.CAP、System.Text.Json、xUnit 2.9.3。

## Global Constraints

- 允许破坏性 API 变更，不保留 `IGirvsClaimManager`、`GirvsIdentityClaim` 或兼容适配器。
- 保留 Claim Type：`zf_sib`、`zf_sname`、`zf_tid`、`zf_tname`、`zf_itype`、`zf_csm`、`zf_utype`；新增 `zf_exec_source`。
- EventBus Header 为 `girvs-identity`，版本 `1`，UTF-8 上限 `8192` 字节。
- EventBus 只传播 Girvs 身份 Claim 与 `client_id`；不传播 Token、Cookie、角色、权限和未知 Claim。
- 同 Type 多值 Claim 不去重，不转换为 `Dictionary<string, string>`。
- `IdentityType.EventMessageUser` 只保留历史解析，不再表示执行来源。
- 严格执行 RED → GREEN → REFACTOR；不覆盖或提交工作区中无关的 `.csproj` 修改。

---

### Task 1：建立核心 Principal 契约

**Files:**
- Create: `tests/Girvs.Claims.Tests/Girvs.Claims.Tests.csproj`
- Create: `tests/Girvs.Claims.Tests/GlobalUsings.cs`
- Create: `tests/Girvs.Claims.Tests/GirvsPrincipalAccessorTests.cs`
- Create: `tests/Girvs.Claims.Tests/ClaimsPrincipalExtensionsTests.cs`
- Create: `Girvs/Infrastructure/IGirvsPrincipalAccessor.cs`
- Create: `Girvs/Infrastructure/GirvsPrincipalAccessor.cs`
- Create: `Girvs/Infrastructure/GirvsClaimTypes.cs`
- Create: `Girvs/Extensions/ClaimsPrincipalExtensions.cs`
- Modify: `Girvs/Infrastructure/IEngine.cs`
- Modify: `Girvs/Infrastructure/GirvsEngine.cs`
- Modify: `Girvs/GirvsModuleStartup.cs`
- Modify: `Girvs.slnx`

**Interfaces:**
- Produces: `IGirvsPrincipalAccessor.Principal`、`Change(ClaimsPrincipal)`、`GirvsClaimTypes`、`ExecutionSource` 和 Principal 读取扩展。

- [ ] **Step 1：创建 net10.0 xUnit 测试项目并加入解决方案**

测试项目引用 `Microsoft.AspNetCore.App`、现有 xUnit 版本，以及 `Girvs`、`Girvs.AuthorizePermission`、`Girvs.EventBus`、`Girvs.Driven`、`Girvs.Cache`、`Girvs.EntityFrameworkCore`。在 `Girvs.slnx` 增加：

```xml
<Project Path="tests/Girvs.Claims.Tests/Girvs.Claims.Tests.csproj" />
```

- [ ] **Step 2：写 Accessor 失败测试**

```csharp
[Fact] public void Principal_没有显式身份时_返回HttpContextUser();
[Fact] public void Principal_没有任何身份时_返回空Principal();
[Fact] public void Change_嵌套切换身份_按相反顺序恢复();
[Fact] public async Task Change_并发异步流_身份互不串扰();
```

并发测试分别使用 `user-a`、`user-b`，`Task.WhenAll` 后恢复 HTTP Principal。

- [ ] **Step 3：运行并确认 RED**

```bash
dotnet test tests/Girvs.Claims.Tests/Girvs.Claims.Tests.csproj --filter FullyQualifiedName~GirvsPrincipalAccessorTests --nologo
```

Expected: `IGirvsPrincipalAccessor` 尚不存在导致编译失败。

- [ ] **Step 4：实现最小 Accessor**

```csharp
public interface IGirvsPrincipalAccessor
{
    ClaimsPrincipal Principal { get; }
    IDisposable Change(ClaimsPrincipal principal);
}
```

实现读取顺序为 AsyncLocal 显式身份、`HttpContext.User`、空 Principal；`Change(null)` 抛 `ArgumentNullException`，Dispose 恢复旧值且可重复调用。核心模块调用 `AddHttpContextAccessor()` 并把 Accessor 注册为 Singleton。`IEngine.ClaimManager` 替换为无副作用的 `IEngine.PrincipalAccessor`。

- [ ] **Step 5：运行 Accessor 测试并确认 GREEN**

执行 Step 3 命令，预期 4 项通过。

- [ ] **Step 6：写 Claim 扩展失败测试**

覆盖所有 Girvs Claim、两个同 Type Claim、`Guid` 泛型 ID 转换和缺失 Claim 默认值。

- [ ] **Step 7：实现 Claim 常量、枚举和扩展**

```csharp
public const string UserId = "zf_sib";
public const string UserName = "zf_sname";
public const string TenantId = "zf_tid";
public const string TenantName = "zf_tname";
public const string IdentityType = "zf_itype";
public const string SystemModule = "zf_csm";
public const string UserType = "zf_utype";
public const string ExecutionSource = "zf_exec_source";
public const string ClientId = "client_id";
```

把 `IdentityType`、`SystemModule` 原值原序移入核心契约，新增 `ExecutionSource.Http/EventBus/BackgroundJob`。扩展提供用户、租户、身份类型、系统模块、执行来源、任意 Claim 的读取，以及用户/租户泛型转换。

- [ ] **Step 8：运行核心测试并提交**

```bash
dotnet test tests/Girvs.Claims.Tests/Girvs.Claims.Tests.csproj --filter "FullyQualifiedName~GirvsPrincipalAccessorTests|FullyQualifiedName~ClaimsPrincipalExtensionsTests" --nologo
git add Girvs tests/Girvs.Claims.Tests Girvs.slnx
git commit -m "feat: 新增 ClaimsPrincipal 身份上下文"
```

---

### Task 2：迁移 JWT 与 HTTP 租户身份

**Files:**
- Create: `tests/Girvs.Claims.Tests/JwtBearerAuthenticationExtensionTests.cs`
- Create: `tests/Girvs.Claims.Tests/GirvsTenantClaimsMiddlewareTests.cs`
- Create: `Girvs.AuthorizePermission/Middleware/GirvsTenantClaimsMiddleware.cs`
- Modify: `Girvs.AuthorizePermission/Extensions/JwtBearerAuthenticationExtension.cs`
- Modify: `Girvs.AuthorizePermission/GirvsAuthorizeModule.cs`
- Delete: `Girvs.AuthorizePermission/IdentityClaim.cs`
- Delete: `Girvs.AuthorizePermission/Extensions/GirvsClaimManagerExtensions.cs`

**Interfaces:**
- Consumes: Task 1 的 Claim 契约。
- Produces: `GenerateToken(ClaimsIdentity)` 和认证后租户 Claim 补充。

- [ ] **Step 1：写 JWT 失败测试**

断言参数式重载生成全部 Girvs Claim；传入包含两个同 Type Claim 的 Identity 后 Token 中两项都存在；未注册 PrincipalAccessor 仍可签发 Token。

- [ ] **Step 2：运行并确认 RED**

```bash
dotnet test tests/Girvs.Claims.Tests/Girvs.Claims.Tests.csproj --filter FullyQualifiedName~JwtBearerAuthenticationExtensionTests --nologo
```

- [ ] **Step 3：重写 JWT 入口并确认 GREEN**

```csharp
public static string GenerateToken(ClaimsIdentity claimsIdentity) =>
    GetJwtAccessToken(claimsIdentity);
```

参数式重载直接创建非空 Claim，不解析 `EngineContext.Current`，不修改或去重传入 Identity。保留 HMAC SHA-256、现有密钥和过期配置。再次执行 Step 2，预期通过。

- [ ] **Step 4：写 HTTP 中间件失败测试**

```csharp
[Fact] public async Task InvokeAsync_已认证注册用户_请求头租户替换原租户Claim();
[Fact] public async Task InvokeAsync_匿名请求携带租户头_补充租户但不标记认证();
[Fact] public async Task InvokeAsync_没有租户头_不修改Principal();
```

第一项同时验证 URL 解码，且只替换 Girvs 租户 Claim。

- [ ] **Step 5：实现中间件并确认 GREEN**

中间件在 `UseAuthentication()` 后、`UseAuthorization()` 前执行。认证的 `RegisterUser` 修改首个已认证 Identity；匿名租户请求增加 authenticationType 为空的 Identity；无租户头不修改 Principal。

```bash
dotnet test tests/Girvs.Claims.Tests/Girvs.Claims.Tests.csproj --filter "FullyQualifiedName~JwtBearerAuthenticationExtensionTests|FullyQualifiedName~GirvsTenantClaimsMiddlewareTests" --nologo
```

- [ ] **Step 6：提交**

```bash
git add Girvs.AuthorizePermission tests/Girvs.Claims.Tests
git commit -m "refactor: 统一 HTTP 与 JWT Claims 身份"
```

---

### Task 3：实现 EventBus 身份序列化与发布

**Files:**
- Create: `Girvs.EventBus/Identity/IntegrationIdentityContext.cs`
- Create: `Girvs.EventBus/Identity/IntegrationIdentityContextSerializer.cs`
- Create: `tests/Girvs.Claims.Tests/IntegrationIdentityContextSerializerTests.cs`
- Create: `tests/Girvs.Claims.Tests/CapEventBusTests.cs`
- Modify: `Girvs.EventBus/CapEventBus/CapEventBus.cs`
- Delete: `Girvs.EventBus/Extensions/ClaimManagerExtensions.cs`

**Interfaces:**
- Produces: 版本化身份 DTO、序列化器和 `girvs-identity` Header。

- [ ] **Step 1：写序列化失败测试**

覆盖白名单过滤、两个同 Type `client_id` 往返、无白名单 Claim 返回 null、超过 8192 字节失败且错误不泄露值、非法 JSON 和未知版本失败。

- [ ] **Step 2：运行并确认 RED**

```bash
dotnet test tests/Girvs.Claims.Tests/Girvs.Claims.Tests.csproj --filter FullyQualifiedName~IntegrationIdentityContextSerializerTests --nologo
```

- [ ] **Step 3：实现固定传输契约**

```csharp
public sealed record IntegrationClaim(string Type, string Value, string ValueType, string Issuer);
public sealed record IntegrationIdentityContext(int Version, IReadOnlyCollection<IntegrationClaim> Claims);
```

序列化器公开 `HeaderName="girvs-identity"`、`CurrentVersion=1`、`MaxHeaderBytes=8192`。白名单为 8 个 Girvs 身份 Type 加 `client_id`；验证 Type、Value、版本和 UTF-8 大小，错误不输出 Claim 值。

- [ ] **Step 4：运行序列化测试并确认 GREEN**

执行 Step 2，预期全部通过。

- [ ] **Step 5：写发布端失败测试并改造 CapEventBus**

测试替身记录 `ICapPublisher` headers，断言有身份时只写 `girvs-identity`，空身份时不写。`CapEventBus` 构造器注入 `IGirvsPrincipalAccessor`，调用序列化器，不访问 Engine ClaimManager。

```bash
dotnet test tests/Girvs.Claims.Tests/Girvs.Claims.Tests.csproj --filter "FullyQualifiedName~IntegrationIdentityContextSerializerTests|FullyQualifiedName~CapEventBusTests" --nologo
```

- [ ] **Step 6：提交**

```bash
git add Girvs.EventBus tests/Girvs.Claims.Tests
git commit -m "feat: EventBus 传播白名单 Claims 身份"
```

---

### Task 4：在 EventBus 消费 Scope 恢复 Principal

**Files:**
- Create: `tests/Girvs.Claims.Tests/GirvsIntegrationEventHandlerTests.cs`
- Modify: `Girvs.EventBus/GirvsIntegrationEventHandler.cs`
- Modify: `samples/Sample.ServiceB/Events/SampleMessageHandler.cs`

**Interfaces:**
- Consumes: `[FromCap] CapHeader` 和 Task 3 序列化器。
- Produces: 带 Header 的 `HandleInScopeAsync` 重载。

- [ ] **Step 1：写消费失败测试**

覆盖合法身份恢复且保留 `ManagerUser`、设置 `ExecutionSource.EventBus`、无 Header 使用空 Principal、并发消费隔离、正常完成或异常后恢复进入前身份。

- [ ] **Step 2：运行并确认 RED**

```bash
dotnet test tests/Girvs.Claims.Tests/Girvs.Claims.Tests.csproj --filter FullyQualifiedName~GirvsIntegrationEventHandlerTests --nologo
```

- [ ] **Step 3：实现消费模板**

新增：

```csharp
protected Task HandleInScopeAsync(CapHeader header,
    Func<CancellationToken, Task> body, CancellationToken cancellationToken);
protected Task HandleInScopeAsync(CapHeader header,
    Func<IServiceProvider, CancellationToken, Task> body, CancellationToken cancellationToken);
```

在独立 Scope 内解析 Accessor；合法 Header 重建 authenticationType 为 `Girvs.EventBus` 的 Identity，保留原始身份类型并替换执行来源；无 Header 显式切换为空 Principal。`using` 保证异常时恢复。

- [ ] **Step 4：更新 Sample Handler 并确认 GREEN**

Sample 将 `[FromCap] CapHeader` 传入新重载。执行 Step 2，预期全部通过。

- [ ] **Step 5：提交**

```bash
git add Girvs.EventBus samples/Sample.ServiceB tests/Girvs.Claims.Tests
git commit -m "feat: EventBus 消费端恢复 ClaimsPrincipal"
```

---

### Task 5：迁移全部下游消费者并删除旧 API

**Files:**
- Create: `tests/Girvs.Claims.Tests/IdentityConsumerMigrationTests.cs`
- Modify: `Girvs/BusinessBasis/Entities/BaseEntity.cs`
- Modify: `Girvs/BusinessBasis/Repositories/GirvsRepositoryOtherQueryCondition.cs`
- Modify: `Girvs.EntityFrameworkCore/Repositories/Repository.cs`
- Modify: `Girvs.EntityFrameworkCore/DbContextExtensions/EngineContextExtensions.cs`
- Modify: `Girvs.Cache/Caching/GirvsEntityCacheDefaults.cs`
- Modify: `Girvs.Driven/Events/Message.cs`
- Modify: `Girvs.AuthorizePermission/AuthorizeCompare/GirvsAuthorizeCompare.cs`
- Delete: `Girvs/Infrastructure/IGirvsClaimManager.cs`
- Modify/Delete: 静态扫描命中的其他旧身份引用。

**Interfaces:**
- Consumes: `EngineContext.Current.PrincipalAccessor.Principal` 和 Principal 扩展。

- [ ] **Step 1：写下游失败测试**

测试实体实现 Creator、Tenant 四个接口，在显式 Principal 下构造并断言字段；另覆盖租户查询表达式、租户缓存键、`MessageSource` 和 `GirvsAuthorizeCompare.IsLogin()` 在 EventBus Principal 下的行为。系统事件若需要公共租户，必须显式携带空 Guid 租户 Claim。

- [ ] **Step 2：运行并确认 RED**

```bash
dotnet test tests/Girvs.Claims.Tests/Girvs.Claims.Tests.csproj --filter FullyQualifiedName~IdentityConsumerMigrationTests --nologo
```

- [ ] **Step 3：逐文件迁移到 Principal**

统一通过：

```csharp
var principal = EngineContext.Current.PrincipalAccessor.Principal;
```

读取身份。`IsLogin()` 使用 `principal.Identity?.IsAuthenticated == true`；Repository 不再以 `EventMessageUser` 猜测空租户；Message 不再吞掉任意异常。

- [ ] **Step 4：删除旧文件并静态扫描**

```bash
rg -n "IGirvsClaimManager|GirvsIdentityClaim|GirvsIdentityClaimTypes|IdentityClaimManager|\.ClaimManager|\.IdentityClaim|SetFromHttpRequestToken|SetFromDictionary|BuildClaimsIdentity" --glob '*.cs'
```

Expected: 无输出。

- [ ] **Step 5：运行全部专项测试并提交**

```bash
dotnet test tests/Girvs.Claims.Tests/Girvs.Claims.Tests.csproj --nologo
git add Girvs Girvs.AuthorizePermission Girvs.EntityFrameworkCore Girvs.Cache Girvs.Driven tests/Girvs.Claims.Tests
git commit -m "refactor: 底层模块改用 ClaimsPrincipal"
```

---

### Task 6：清理样例并完成回归验证

**Files:**
- Modify: `samples/Sample.ServiceB/Startup.cs`
- Delete: `samples/Sample.ServiceB/SampleClaimManager.cs`
- Modify: `Girvs.slnx`

- [ ] **Step 1：删除 Sample 空 ClaimManager 注册与文件**

删除 `services.AddScoped<IGirvsClaimManager, SampleClaimManager>()`，验证未引用 AuthorizePermission 的服务仍由核心模块取得 PrincipalAccessor。

- [ ] **Step 2：恢复、测试专项项目和构建样例**

```bash
dotnet restore tests/Girvs.Claims.Tests/Girvs.Claims.Tests.csproj --nologo
dotnet test tests/Girvs.Claims.Tests/Girvs.Claims.Tests.csproj --no-restore --nologo
dotnet build samples/Sample.ServiceB/Sample.ServiceB.csproj --no-restore --nologo
```

- [ ] **Step 3：构建六个受影响模块**

```bash
dotnet build Girvs/Girvs.csproj --no-restore --nologo
dotnet build Girvs.AuthorizePermission/Girvs.AuthorizePermission.csproj --no-restore --nologo
dotnet build Girvs.EventBus/Girvs.EventBus.csproj --no-restore --nologo
dotnet build Girvs.EntityFrameworkCore/Girvs.EntityFrameworkCore.csproj --no-restore --nologo
dotnet build Girvs.Cache/Girvs.Cache.csproj --no-restore --nologo
dotnet build Girvs.Driven/Girvs.Driven.csproj --no-restore --nologo
```

Expected: 全部成功构建 `net8.0`、`net9.0`、`net10.0`。

- [ ] **Step 4：全解决方案与静态验证**

```bash
dotnet build Girvs.slnx --no-restore --nologo
dotnet test Girvs.slnx --no-build --nologo
git diff --check
rg -n "IGirvsClaimManager|GirvsIdentityClaim|GirvsIdentityClaimTypes|IdentityClaimManager|\.ClaimManager|\.IdentityClaim" --glob '*.cs'
git status --short
```

Expected: 构建与测试成功，diff 检查和旧 API 扫描无输出；既有无关 `.csproj` 修改保持原样且不进入提交。

- [ ] **Step 5：提交验证收尾**

```bash
git add Girvs.slnx samples/Sample.ServiceB tests/Girvs.Claims.Tests
git commit -m "test: 完成 ClaimsPrincipal 身份迁移验证"
```

## 完成标准

- 代码库不存在旧 ClaimManager、GirvsIdentityClaim 或字典化身份转换。
- HTTP、JWT、CAP 发布与消费、实体审计、租户过滤、缓存、权限判断和 Driven Message 均使用标准 Principal。
- 同 Type 多值 Claim 不丢失；CAP 消费并发隔离并保留原始身份类型。
- 无认证模块的 Sample.ServiceB 不需要伪造身份管理器。
- 专项测试、受影响模块多目标构建和全解决方案验证通过。

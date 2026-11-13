# GirvsPrincipalFactory Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 为定时任务、接口回调、启动期发送事件消息等没有 HTTP token 的执行入口，提供构造 `ClaimsPrincipal` 并设为当前身份的标准方式。

**Architecture:** 在 `Girvs` 核心新增两个文件：静态工厂 `GirvsPrincipalFactory` 负责把身份字段或 claim 字典翻译成 `ClaimsPrincipal`；扩展方法 `PrincipalAccessorExtensions` 把"构造 + 切换"合成一步，内部复用现有的 `IGirvsPrincipalAccessor.Change(principal)`。不新增任何可变状态、不引入配置节点、不改动 `Girvs.EventBus` 与 `Girvs.Quartz`。

**Tech Stack:** .NET 8/9/10（`Girvs` 核心多目标）、`System.Security.Claims`、xunit（测试项目仅 net10.0）

## Global Constraints

- `Girvs` 核心项目多目标框架为 `net8.0;net9.0;net10.0`，新增代码必须在三个框架下均可编译，不得使用仅高版本可用的 API。
- 身份内容一律由调用方决定：不新增配置节点、不预设默认业务值（用户名、租户等）、不为匿名请求做全局兜底。
- claim 类型一律使用 `GirvsClaimTypes` 常量，不做 `System.Security.Claims.ClaimTypes.Sid` 一类的别名映射。
- 构造 `ClaimsIdentity` 时必须传 `authenticationType`，取值 `GirvsPrincipalFactory.AuthenticationType`（`"Girvs"`）。否则 `Identity.IsAuthenticated` 为 `false`，`Girvs.AuthorizePermission/AuthorizeCompare/GirvsAuthorizeCompare.cs:27` 会判定未认证。
- 新写代码注释、测试方法名使用中文，跟随 `tests/Girvs.Claims.Tests` 现有惯例（如 `Principal_没有任何身份时_返回空Principal`）。
- 测试项目 `tests/Girvs.Claims.Tests` 的 `GlobalUsings.cs` 已包含 `System.Security.Claims`、`Girvs.Infrastructure`、`Girvs.Extensions`、`Girvs.Configuration`、`Microsoft.AspNetCore.Http`、`Xunit`，测试文件无需重复 using。
- `Girvs` 核心的 `GlobalUsings.cs` 已包含 `System.Security.Claims`、`System.Collections.Generic`、`System.Linq`、`Girvs.Infrastructure`，实现文件无需重复 using。

## File Structure

| 文件 | 职责 |
|---|---|
| `Girvs/Infrastructure/GirvsPrincipalFactory.cs`（新建） | 静态工厂。把身份字段或 claim 字典翻译成 `ClaimsPrincipal`。无状态、无依赖。 |
| `Girvs/Infrastructure/PrincipalAccessorExtensions.cs`（新建） | `IGirvsPrincipalAccessor` 的扩展方法，构造 + 切换合一。 |
| `tests/Girvs.Claims.Tests/GirvsPrincipalFactoryTests.cs`（新建） | 工厂的单元测试。 |
| `tests/Girvs.Claims.Tests/PrincipalAccessorExtensionsTests.cs`（新建） | 扩展方法的作用域行为测试。 |
| `框架升级说明.md`（修改） | 补充旧身份 API 的迁移对照，供业务系统升级参照。 |
| `docs/superpowers/specs/2026-07-23-principal-factory-design.md`（修改） | 同步扩展方法的最终文件路径。 |

**与 spec 的一处差异：** spec 写的扩展方法路径为 `Girvs/Infrastructure/Extensions/PrincipalAccessorExtensions.cs`（命名空间 `Girvs.Infrastructure.Extensions`）。本计划改为 `Girvs/Infrastructure/PrincipalAccessorExtensions.cs`（命名空间 `Girvs.Infrastructure`），与被扩展的 `IGirvsPrincipalAccessor` 同命名空间，业务代码只要已 `using Girvs.Infrastructure`（用 `EngineContext` 就必然已经引入）即可直接使用，少一个 using，降低迁移摩擦。Task 4 同步修正 spec。

---

### Task 1: GirvsPrincipalFactory.Create

**Files:**
- Create: `Girvs/Infrastructure/GirvsPrincipalFactory.cs`
- Test: `tests/Girvs.Claims.Tests/GirvsPrincipalFactoryTests.cs`

**Interfaces:**
- Consumes: `GirvsClaimTypes`（`Girvs/Infrastructure/GirvsClaimTypes.cs`，常量 `UserId`/`UserName`/`TenantId`/`TenantName`/`IdentityType`/`ExecutionSource`/`ClientId`）、枚举 `IdentityType`、`ExecutionSource`；读取侧 `ClaimsPrincipalExtensions`（`GetUserId()`/`GetTenantId()`/`GetUserName()`/`GetTenantName()`/`GetIdentityType()`/`GetExecutionSource()`/`GetClaimValue(string)`）
- Produces:
  - `public const string GirvsPrincipalFactory.AuthenticationType = "Girvs"`
  - `public static ClaimsPrincipal GirvsPrincipalFactory.Create(string userId = null, string tenantId = null, string userName = null, string tenantName = null, IdentityType identityType = IdentityType.ManagerUser, ExecutionSource source = ExecutionSource.Http, IDictionary<string, string> additionalClaims = null)`

- [ ] **Step 1: 写失败的测试**

创建 `tests/Girvs.Claims.Tests/GirvsPrincipalFactoryTests.cs`：

```csharp
namespace Girvs.Claims.Tests;

public class GirvsPrincipalFactoryTests
{
    [Fact]
    public void Create_产出的身份_已通过认证()
    {
        var principal = GirvsPrincipalFactory.Create(userId: "u1");

        Assert.True(principal.Identity?.IsAuthenticated);
        Assert.Equal(
            GirvsPrincipalFactory.AuthenticationType,
            principal.Identity?.AuthenticationType);
    }

    [Fact]
    public void Create_具名字段_映射到对应的GirvsClaimTypes()
    {
        var principal = GirvsPrincipalFactory.Create(
            userId: "u1",
            tenantId: "t1",
            userName: "系统管理员",
            tenantName: "系统",
            identityType: IdentityType.EventMessageUser,
            source: ExecutionSource.BackgroundJob);

        Assert.Equal("u1", principal.GetUserId());
        Assert.Equal("t1", principal.GetTenantId());
        Assert.Equal("系统管理员", principal.GetUserName());
        Assert.Equal("系统", principal.GetTenantName());
        Assert.Equal(IdentityType.EventMessageUser, principal.GetIdentityType());
        Assert.Equal(ExecutionSource.BackgroundJob, principal.GetExecutionSource());
    }

    [Fact]
    public void Create_未传的字段_不产生Claim()
    {
        var principal = GirvsPrincipalFactory.Create(tenantId: "t1");

        Assert.Null(principal.FindFirst(GirvsClaimTypes.UserId));
        Assert.Equal(string.Empty, principal.GetUserId());
    }

    [Fact]
    public void Create_未指定身份类型与执行入口时_取默认值()
    {
        var principal = GirvsPrincipalFactory.Create(tenantId: "t1");

        Assert.Equal(IdentityType.ManagerUser, principal.GetIdentityType());
        Assert.Equal(ExecutionSource.Http, principal.GetExecutionSource());
    }

    [Fact]
    public void Create_附加Claim与具名字段冲突时_具名字段优先()
    {
        var principal = GirvsPrincipalFactory.Create(
            tenantId: "t1",
            additionalClaims: new Dictionary<string, string>
            {
                [GirvsClaimTypes.TenantId] = "t-additional",
                [GirvsClaimTypes.ClientId] = "client-1",
            });

        Assert.Equal("t1", principal.GetTenantId());
        Assert.Equal("client-1", principal.GetClaimValue(GirvsClaimTypes.ClientId));
    }

    [Fact]
    public void Create_具名字段为空时_保留附加Claim中的同名项()
    {
        var principal = GirvsPrincipalFactory.Create(
            additionalClaims: new Dictionary<string, string>
            {
                [GirvsClaimTypes.TenantId] = "t-additional",
            });

        Assert.Equal("t-additional", principal.GetTenantId());
    }
}
```

- [ ] **Step 2: 运行测试，确认失败**

Run: `dotnet test tests/Girvs.Claims.Tests/Girvs.Claims.Tests.csproj --filter "FullyQualifiedName~GirvsPrincipalFactoryTests"`

Expected: 编译失败，报 `error CS0103: The name 'GirvsPrincipalFactory' does not exist in the current context`

- [ ] **Step 3: 写最小实现**

创建 `Girvs/Infrastructure/GirvsPrincipalFactory.cs`：

```csharp
namespace Girvs.Infrastructure;

/// <summary>
/// 身份构造工厂。用于定时任务、接口回调、启动期发送事件消息等没有 HTTP token 的执行入口。
/// </summary>
public static class GirvsPrincipalFactory
{
    /// <summary>
    /// 认证方式名称。必须传给 <see cref="ClaimsIdentity"/>，否则 IsAuthenticated 为 false。
    /// </summary>
    public const string AuthenticationType = "Girvs";

    /// <summary>
    /// 按具名字段构造身份。字段为空时不产生对应的 Claim；
    /// <paramref name="additionalClaims"/> 中与具名字段同类型的项会被具名字段覆盖。
    /// </summary>
    public static ClaimsPrincipal Create(
        string userId = null,
        string tenantId = null,
        string userName = null,
        string tenantName = null,
        IdentityType identityType = IdentityType.ManagerUser,
        ExecutionSource source = ExecutionSource.Http,
        IDictionary<string, string> additionalClaims = null)
    {
        var claims = new Dictionary<string, string>();

        if (additionalClaims != null)
        {
            foreach (var claim in additionalClaims)
            {
                if (!string.IsNullOrEmpty(claim.Key) && claim.Value != null)
                {
                    claims[claim.Key] = claim.Value;
                }
            }
        }

        SetIfNotEmpty(claims, GirvsClaimTypes.UserId, userId);
        SetIfNotEmpty(claims, GirvsClaimTypes.TenantId, tenantId);
        SetIfNotEmpty(claims, GirvsClaimTypes.UserName, userName);
        SetIfNotEmpty(claims, GirvsClaimTypes.TenantName, tenantName);
        claims[GirvsClaimTypes.IdentityType] = identityType.ToString();
        claims[GirvsClaimTypes.ExecutionSource] = source.ToString();

        return Build(claims);
    }

    private static void SetIfNotEmpty(
        IDictionary<string, string> claims,
        string claimType,
        string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            claims[claimType] = value;
        }
    }

    private static ClaimsPrincipal Build(IDictionary<string, string> claims)
    {
        var identity = new ClaimsIdentity(
            claims.Select(claim => new Claim(claim.Key, claim.Value)),
            AuthenticationType);

        return new ClaimsPrincipal(identity);
    }
}
```

- [ ] **Step 4: 运行测试，确认通过**

Run: `dotnet test tests/Girvs.Claims.Tests/Girvs.Claims.Tests.csproj --filter "FullyQualifiedName~GirvsPrincipalFactoryTests"`

Expected: `Passed! - Failed: 0, Passed: 6`

- [ ] **Step 5: 确认核心项目三个目标框架均可编译**

Run: `dotnet build Girvs/Girvs.csproj`

Expected: `0 Error(s)`（既有 warning 不受影响）

- [ ] **Step 6: 提交**

```bash
git add Girvs/Infrastructure/GirvsPrincipalFactory.cs tests/Girvs.Claims.Tests/GirvsPrincipalFactoryTests.cs
git commit -m "feat: 新增 GirvsPrincipalFactory.Create 构造身份"
```

---

### Task 2: GirvsPrincipalFactory.FromClaims

**Files:**
- Modify: `Girvs/Infrastructure/GirvsPrincipalFactory.cs`
- Test: `tests/Girvs.Claims.Tests/GirvsPrincipalFactoryTests.cs`

**Interfaces:**
- Consumes: Task 1 的 `GirvsPrincipalFactory` 私有方法 `Build(IDictionary<string, string>)`、常量 `AuthenticationType`
- Produces: `public static ClaimsPrincipal GirvsPrincipalFactory.FromClaims(IDictionary<string, string> claims, ExecutionSource source = ExecutionSource.Http)`

- [ ] **Step 1: 写失败的测试**

在 `tests/Girvs.Claims.Tests/GirvsPrincipalFactoryTests.cs` 的类中追加三个测试方法：

```csharp
    [Fact]
    public void FromClaims_保留字典原有键_并写入ExecutionSource()
    {
        var principal = GirvsPrincipalFactory.FromClaims(
            new Dictionary<string, string>
            {
                [GirvsClaimTypes.TenantId] = "t1",
                [GirvsClaimTypes.IdentityType] = IdentityType.ManagerUser.ToString(),
                ["custom-key"] = "custom-value",
            },
            ExecutionSource.BackgroundJob);

        Assert.True(principal.Identity?.IsAuthenticated);
        Assert.Equal("t1", principal.GetTenantId());
        Assert.Equal(IdentityType.ManagerUser, principal.GetIdentityType());
        Assert.Equal("custom-value", principal.GetClaimValue("custom-key"));
        Assert.Equal(ExecutionSource.BackgroundJob, principal.GetExecutionSource());
    }

    [Fact]
    public void FromClaims_字典中的ExecutionSource_被参数覆盖()
    {
        var principal = GirvsPrincipalFactory.FromClaims(
            new Dictionary<string, string>
            {
                [GirvsClaimTypes.ExecutionSource] = ExecutionSource.Http.ToString(),
            },
            ExecutionSource.EventBus);

        Assert.Equal(ExecutionSource.EventBus, principal.GetExecutionSource());
    }

    [Fact]
    public void FromClaims_传入null_抛出ArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => GirvsPrincipalFactory.FromClaims(null));
    }
```

- [ ] **Step 2: 运行测试，确认失败**

Run: `dotnet test tests/Girvs.Claims.Tests/Girvs.Claims.Tests.csproj --filter "FullyQualifiedName~GirvsPrincipalFactoryTests"`

Expected: 编译失败，报 `error CS0117: 'GirvsPrincipalFactory' does not contain a definition for 'FromClaims'`

- [ ] **Step 3: 写最小实现**

在 `Girvs/Infrastructure/GirvsPrincipalFactory.cs` 中，`Create` 方法之后、`SetIfNotEmpty` 之前插入：

```csharp
    /// <summary>
    /// 按 claim 字典构造身份。字典键即 claim 类型，原样写入；
    /// <paramref name="source"/> 会覆盖字典中的 ExecutionSource 项。
    /// </summary>
    public static ClaimsPrincipal FromClaims(
        IDictionary<string, string> claims,
        ExecutionSource source = ExecutionSource.Http)
    {
        ArgumentNullException.ThrowIfNull(claims);

        var merged = new Dictionary<string, string>();
        foreach (var claim in claims)
        {
            if (!string.IsNullOrEmpty(claim.Key) && claim.Value != null)
            {
                merged[claim.Key] = claim.Value;
            }
        }

        merged[GirvsClaimTypes.ExecutionSource] = source.ToString();

        return Build(merged);
    }
```

- [ ] **Step 4: 运行测试，确认通过**

Run: `dotnet test tests/Girvs.Claims.Tests/Girvs.Claims.Tests.csproj --filter "FullyQualifiedName~GirvsPrincipalFactoryTests"`

Expected: `Passed! - Failed: 0, Passed: 9`

- [ ] **Step 5: 提交**

```bash
git add Girvs/Infrastructure/GirvsPrincipalFactory.cs tests/Girvs.Claims.Tests/GirvsPrincipalFactoryTests.cs
git commit -m "feat: 新增 GirvsPrincipalFactory.FromClaims 从字典构造身份"
```

---

### Task 3: PrincipalAccessorExtensions.ChangeTo

**Files:**
- Create: `Girvs/Infrastructure/PrincipalAccessorExtensions.cs`
- Test: `tests/Girvs.Claims.Tests/PrincipalAccessorExtensionsTests.cs`

**Interfaces:**
- Consumes: Task 1 的 `GirvsPrincipalFactory.Create(...)`、Task 2 的 `GirvsPrincipalFactory.FromClaims(...)`；现有 `IGirvsPrincipalAccessor.Change(ClaimsPrincipal)` 返回 `IDisposable`；现有 `GirvsPrincipalAccessor(IHttpContextAccessor)` 构造函数
- Produces:
  - `public static IDisposable PrincipalAccessorExtensions.ChangeTo(this IGirvsPrincipalAccessor accessor, string userId = null, string tenantId = null, string userName = null, string tenantName = null, IdentityType identityType = IdentityType.ManagerUser, ExecutionSource source = ExecutionSource.Http, IDictionary<string, string> additionalClaims = null)`
  - `public static IDisposable PrincipalAccessorExtensions.ChangeTo(this IGirvsPrincipalAccessor accessor, IDictionary<string, string> claims, ExecutionSource source = ExecutionSource.Http)`

- [ ] **Step 1: 写失败的测试**

创建 `tests/Girvs.Claims.Tests/PrincipalAccessorExtensionsTests.cs`：

```csharp
namespace Girvs.Claims.Tests;

public class PrincipalAccessorExtensionsTests
{
    [Fact]
    public void ChangeTo_具名字段_作用域内生效并在结束后还原()
    {
        var accessor = new GirvsPrincipalAccessor(new HttpContextAccessor());

        using (accessor.ChangeTo(
                   tenantId: "t1",
                   userName: "系统管理员",
                   source: ExecutionSource.BackgroundJob))
        {
            Assert.True(accessor.Principal.Identity?.IsAuthenticated);
            Assert.Equal("t1", accessor.Principal.GetTenantId());
            Assert.Equal("系统管理员", accessor.Principal.GetUserName());
            Assert.Equal(
                ExecutionSource.BackgroundJob,
                accessor.Principal.GetExecutionSource());
        }

        Assert.Empty(accessor.Principal.Identities);
    }

    [Fact]
    public void ChangeTo_Claim字典_作用域内生效并在结束后还原()
    {
        var accessor = new GirvsPrincipalAccessor(new HttpContextAccessor());
        var claims = new Dictionary<string, string>
        {
            [GirvsClaimTypes.TenantId] = "t1",
            [GirvsClaimTypes.IdentityType] = IdentityType.ManagerUser.ToString(),
        };

        using (accessor.ChangeTo(claims, ExecutionSource.Http))
        {
            Assert.Equal("t1", accessor.Principal.GetTenantId());
            Assert.Equal(IdentityType.ManagerUser, accessor.Principal.GetIdentityType());
        }

        Assert.Empty(accessor.Principal.Identities);
    }

    [Fact]
    public void ChangeTo_嵌套切换_按相反顺序还原()
    {
        var accessor = new GirvsPrincipalAccessor(new HttpContextAccessor());

        using (accessor.ChangeTo(tenantId: "outer"))
        {
            using (accessor.ChangeTo(tenantId: "inner"))
            {
                Assert.Equal("inner", accessor.Principal.GetTenantId());
            }

            Assert.Equal("outer", accessor.Principal.GetTenantId());
        }

        Assert.Empty(accessor.Principal.Identities);
    }
}
```

- [ ] **Step 2: 运行测试，确认失败**

Run: `dotnet test tests/Girvs.Claims.Tests/Girvs.Claims.Tests.csproj --filter "FullyQualifiedName~PrincipalAccessorExtensionsTests"`

Expected: 编译失败，报 `error CS1929`，提示 `IGirvsPrincipalAccessor` 不包含 `ChangeTo` 的定义

- [ ] **Step 3: 写最小实现**

创建 `Girvs/Infrastructure/PrincipalAccessorExtensions.cs`：

```csharp
namespace Girvs.Infrastructure;

/// <summary>
/// 身份访问器扩展。把身份构造与切换合成一步，供没有 HTTP token 的执行入口使用。
/// </summary>
public static class PrincipalAccessorExtensions
{
    /// <summary>
    /// 按具名字段构造身份并在当前异步执行流内切换。
    /// 返回的作用域释放后还原为切换前的身份，业务逻辑须写在作用域内。
    /// </summary>
    public static IDisposable ChangeTo(
        this IGirvsPrincipalAccessor accessor,
        string userId = null,
        string tenantId = null,
        string userName = null,
        string tenantName = null,
        IdentityType identityType = IdentityType.ManagerUser,
        ExecutionSource source = ExecutionSource.Http,
        IDictionary<string, string> additionalClaims = null)
    {
        ArgumentNullException.ThrowIfNull(accessor);

        return accessor.Change(GirvsPrincipalFactory.Create(
            userId,
            tenantId,
            userName,
            tenantName,
            identityType,
            source,
            additionalClaims));
    }

    /// <summary>
    /// 按 claim 字典构造身份并在当前异步执行流内切换。
    /// 返回的作用域释放后还原为切换前的身份，业务逻辑须写在作用域内。
    /// </summary>
    public static IDisposable ChangeTo(
        this IGirvsPrincipalAccessor accessor,
        IDictionary<string, string> claims,
        ExecutionSource source = ExecutionSource.Http)
    {
        ArgumentNullException.ThrowIfNull(accessor);

        return accessor.Change(GirvsPrincipalFactory.FromClaims(claims, source));
    }
}
```

- [ ] **Step 4: 运行测试，确认通过**

Run: `dotnet test tests/Girvs.Claims.Tests/Girvs.Claims.Tests.csproj --filter "FullyQualifiedName~PrincipalAccessorExtensionsTests"`

Expected: `Passed! - Failed: 0, Passed: 3`

- [ ] **Step 5: 运行 Claims 测试项目全部用例，确认无回归**

Run: `dotnet test tests/Girvs.Claims.Tests/Girvs.Claims.Tests.csproj`

Expected: `Failed: 0`

- [ ] **Step 6: 提交**

```bash
git add Girvs/Infrastructure/PrincipalAccessorExtensions.cs tests/Girvs.Claims.Tests/PrincipalAccessorExtensionsTests.cs
git commit -m "feat: 新增 IGirvsPrincipalAccessor.ChangeTo 扩展方法"
```

---

### Task 4: 迁移文档

**Files:**
- Modify: `框架升级说明.md`
- Modify: `docs/superpowers/specs/2026-07-23-principal-factory-design.md`

**Interfaces:**
- Consumes: Task 1–3 产出的全部公开 API 签名
- Produces: 无代码产物

- [ ] **Step 1: 在 `框架升级说明.md` 末尾追加迁移章节**

追加以下内容（若文件末尾无空行，先补一个空行）：

```markdown
## 身份 API 迁移（IGirvsClaimManager 移除后）

`IGirvsClaimManager` 及其实现已移除，身份载体统一为 `ClaimsPrincipal`。读取通过 `ClaimsPrincipalExtensions`，构造与切换通过 `GirvsPrincipalFactory` 和 `IGirvsPrincipalAccessor.ChangeTo`。

| 旧 API | 新写法 |
|---|---|
| `claimManager.GetTenantId()` | `EngineContext.Current.PrincipalAccessor.Principal.GetTenantId()` |
| `claimManager.SetFromDictionary(dict)` | `using var _ = accessor.ChangeTo(dict, source)` |
| `claimManager.BuildClaimsIdentity(claim)` | `GirvsPrincipalFactory.Create(userId: …, tenantId: …)` |
| `claimManager.ResetClaim(tenantId)`（业务扩展） | `using var _ = accessor.ChangeTo(tenantId: tenantId, identityType: IdentityType.ManagerUser)` |
| `claimManager.CapEventBusReSetClaim(header)` | 发布前 `accessor.ChangeTo(…)`，消费端由 `GirvsCapFilter` 自动恢复 |

### 作用域语义变化

新 API 基于 `AsyncLocal`，`ChangeTo` 返回 `IDisposable`，释放后还原为先前身份。这与旧 `IGirvsClaimManager`「设置一次、后续一直生效」不同：**`AsyncLocal` 的写入只向下游执行流传播**，在 `async` 方法内部切换身份且不释放，方法返回到调用方后调用方读到的仍是切换前的身份。因此新 API 一律以 `using` 作用域使用，业务逻辑写在作用域内。

### 三类无 token 入口的写法

定时任务：

```csharp
var accessor = EngineContext.Current.PrincipalAccessor;
using var _ = accessor.ChangeTo(
    userId: "58205e0e-1552-4282-bedc-a92d0afb37df",
    userName: "系统管理员",
    tenantId: Guid.Empty.ToString(),
    identityType: IdentityType.ManagerUser,
    source: ExecutionSource.BackgroundJob);

await DoWorkAsync();
```

接口回调（身份来自回调报文参数）：

```csharp
var claims = new Dictionary<string, string>
{
    [GirvsClaimTypes.TenantId] = request.MerchantId,
    [GirvsClaimTypes.IdentityType] = IdentityType.ManagerUser.ToString(),
};

using var _ = EngineContext.Current.PrincipalAccessor.ChangeTo(claims, ExecutionSource.Http);
await HandleCallbackAsync(request);
```

启动期发送事件消息。`Girvs.EventBus` 的身份传播链路已闭合：发布端把当前 `Principal` 序列化进 `girvs-identity` 头，消费端 `GirvsCapFilter` 自动恢复。因此只需在发布前建立身份：

```csharp
using var _ = EngineContext.Current.PrincipalAccessor.ChangeTo(
    userId: "58205e0e-1552-4282-bedc-a92d0afb37df",
    userName: "系统管理员",
    tenantId: Guid.Empty.ToString(),
    identityType: IdentityType.ManagerUser,
    source: ExecutionSource.EventBus);

await eventBus.PublishAsync(@event);
```

### 两处行为变化

1. **`IdentityType` 不再被强制覆盖为 `EventMessageUser`。** 旧 `CapEventBusReSetClaim` 无条件覆盖该项，业务扩展 `ResetClaim` 中传入的 `IdentityType.ManagerUser` 从未生效。新写法按调用方所传取值。
2. **`ClaimTypes.Sid` / `GroupSid` / `Name` 不被识别为 `UserId` / `TenantId` / `UserName`。** 旧 `SetFromDictionary` 同样不识别，这些键只作为普通 claim 保留。业务需改用 `GirvsClaimTypes` 常量。
```

- [ ] **Step 2: 同步 spec 中的扩展方法路径**

在 `docs/superpowers/specs/2026-07-23-principal-factory-design.md` 中，把小节标题

```
### 2. `Girvs/Infrastructure/Extensions/PrincipalAccessorExtensions.cs`（新增）
```

改为

```
### 2. `Girvs/Infrastructure/PrincipalAccessorExtensions.cs`（新增）
```

并在该小节首句后补一句：`命名空间为 Girvs.Infrastructure，与被扩展的 IGirvsPrincipalAccessor 保持一致，业务无需额外 using。`

- [ ] **Step 3: 提交**

```bash
git add 框架升级说明.md docs/superpowers/specs/2026-07-23-principal-factory-design.md
git commit -m "docs: 补充身份 API 迁移指引"
```

---

## 完成标准

- [ ] `dotnet build Girvs/Girvs.csproj` 在 net8.0/net9.0/net10.0 下均 `0 Error(s)`
- [ ] `dotnet test tests/Girvs.Claims.Tests/Girvs.Claims.Tests.csproj` 全部通过
- [ ] 新增公开 API 仅两个文件：`GirvsPrincipalFactory`、`PrincipalAccessorExtensions`
- [ ] `Girvs.EventBus`、`Girvs.Quartz`、配置系统零改动

# GirvsPrincipalFactory 设计

日期：2026-07-23

## 背景

`2c51bbcc`（refactor: 底层模块改用 ClaimsPrincipal）移除了 `IGirvsClaimManager` 及其实现 `IdentityClaimManager`，身份载体统一为 `ClaimsPrincipal`，由 `IGirvsPrincipalAccessor` 提供访问与切换。

读取侧是完备的：`ClaimsPrincipalExtensions` 提供 `GetUserId()`、`GetTenantId()`、`GetTenantName()`、`GetIdentityType()` 等，覆盖了旧 `IGirvsClaimManager` 的全部读取方法。

写入侧留下了缺口。随 `IGirvsClaimManager` 一并删除的还有：

- `SetFromDictionary(Dictionary<string, string>)` —— 从字典构造身份
- `BuildClaimsIdentity(GirvsIdentityClaim)` —— 从字段构造 `ClaimsIdentity`
- `SetFromHttpRequestToken()` —— 从 HTTP 请求头构造身份
- `Girvs.EventBus` 的扩展方法 `CapEventBusReSetClaim(CapHeader)`

业务系统基于这些方法自建的扩展（`ResetClaim(tenantId)` 等）随之失效。

## 要解决的问题

部分执行入口天然没有 HTTP token，取不到身份：

- **定时任务**（Quartz Job）
- **接口回调**（第三方回调，无 token，身份信息在回调参数里）
- **启动期主动发送事件消息**（无 HTTP 上下文）

此时 `IGirvsPrincipalAccessor.Principal` 返回空 `ClaimsPrincipal`，下游取不到租户：

- `Girvs.EntityFrameworkCore/Repositories/Repository.cs:41` —— 多租户查询过滤条件为空
- `Girvs.EntityFrameworkCore/DbContextExtensions/EngineContextExtensions.cs:122,179` —— 分表后缀按 `Guid.Empty` 计算

结果是查询返回空集或落到错误的分表，业务无法正常运行。

## 目标

为无身份入口提供**构造 `ClaimsPrincipal` 并设为当前身份**的标准方式。

## 非目标

- **不引入可配置的"系统身份"。** 身份内容一律由调用方决定，框架不预设默认业务值，不新增配置节点。
- **不做无 token 请求的全局身份兜底。** HTTP 回调的身份来自回调参数，由业务显式构造；框架不为匿名请求自动赋予身份，避免认证被绕过。
- **不恢复 `IGirvsClaimManager` 或任何等价门面。** `ClaimsPrincipal` 保持为唯一身份载体。
- **不改动 `Girvs.EventBus`。** 该模块的身份传播链路已经闭合，详见下文。
- **不改动 `Girvs.Quartz`。** 定时任务的身份由业务在 Job 内自行构造。

## 设计

### 1. `Girvs/Infrastructure/GirvsPrincipalFactory.cs`（新增）

静态工厂，唯一职责是把身份信息翻译成 `ClaimsPrincipal`。

```csharp
public static class GirvsPrincipalFactory
{
    public const string AuthenticationType = "Girvs";

    public static ClaimsPrincipal Create(
        string userId = null,
        string tenantId = null,
        string userName = null,
        string tenantName = null,
        IdentityType identityType = IdentityType.ManagerUser,
        ExecutionSource source = ExecutionSource.Http,
        IDictionary<string, string> additionalClaims = null);

    public static ClaimsPrincipal FromClaims(
        IDictionary<string, string> claims,
        ExecutionSource source = ExecutionSource.Http);
}
```

行为约定：

- **`ClaimsIdentity` 必须带 `authenticationType`**，取值 `AuthenticationType`（`"Girvs"`）。否则 `ClaimsIdentity.IsAuthenticated` 为 `false`，`Girvs.AuthorizePermission/AuthorizeCompare/GirvsAuthorizeCompare.cs:27` 会判定未认证，构造出的身份不起作用。已删除的 `BuildClaimsIdentity` 没有传该参数，此处一并修正。
- **空值字段不产生 claim。** `userId` 等参数为 `null` 或空串时跳过，不写入空值 claim。读取侧 `GetClaimValue` 已对缺失 claim 返回 `string.Empty`。
- **`identityType` 默认 `ManagerUser`**，与 `ClaimsPrincipalExtensions.GetIdentityType()` 的解析默认值一致。
- **`additionalClaims` 中与具名参数同类型的项被具名参数覆盖**，保证具名参数语义优先、结果可预测。
- **`FromClaims` 把字典键原样作为 claim type 写入**，随后以 `source` 参数写入 `GirvsClaimTypes.ExecutionSource`，覆盖字典中的同名项。
- 字段到 claim type 的映射一律走 `GirvsClaimTypes`（`UserId` = `zf_sib` 等）。**不做 `System.Security.Claims.ClaimTypes.Sid` 一类的别名映射**——已删除的 `SetFromDictionary` 同样只认 `GirvsClaimTypes`，业务代码里传 `ClaimTypes.Sid` 的写法在旧版就未被识别，改用 `GirvsClaimTypes` 是修复而非回归。

### 2. `Girvs/Infrastructure/PrincipalAccessorExtensions.cs`（新增）

对 `IGirvsPrincipalAccessor` 的薄封装，把"构造 + 切换"合成一步。内部即 `GirvsPrincipalFactory` 加现有的 `Change(principal)`，不新增任何状态。命名空间为 `Girvs.Infrastructure`，与被扩展的 `IGirvsPrincipalAccessor` 保持一致，业务无需额外 using。

```csharp
public static IDisposable ChangeTo(
    this IGirvsPrincipalAccessor accessor,
    string userId = null,
    string tenantId = null,
    string userName = null,
    string tenantName = null,
    IdentityType identityType = IdentityType.ManagerUser,
    ExecutionSource source = ExecutionSource.Http,
    IDictionary<string, string> additionalClaims = null);

public static IDisposable ChangeTo(
    this IGirvsPrincipalAccessor accessor,
    IDictionary<string, string> claims,
    ExecutionSource source = ExecutionSource.Http);
```

### 3. 作用域语义

`GirvsPrincipalAccessor` 基于 `AsyncLocal<ClaimsPrincipal>`，`Change` 返回 `IDisposable`，dispose 后还原前一个身份。

这与旧 `IGirvsClaimManager` 的"设置一次、后续一直生效"不同，迁移时必须注意：**`AsyncLocal` 的写入只向下游执行流传播**。在一个 `async` 方法内部切换身份且不 dispose，方法返回到调用方后，调用方读到的仍是切换前的身份。因此新 API 一律以 `using` 作用域使用，业务逻辑写在作用域内。

## 三类场景的用法

### 定时任务

```csharp
public class SyncJob : GirvsJob
{
    public override async Task Execute(IJobExecutionContext context)
    {
        var accessor = EngineContext.Current.PrincipalAccessor;
        using var _ = accessor.ChangeTo(
            userId: "58205e0e-1552-4282-bedc-a92d0afb37df",
            userName: "系统管理员",
            tenantId: Guid.Empty.ToString(),
            identityType: IdentityType.ManagerUser,
            source: ExecutionSource.BackgroundJob);

        await DoWorkAsync();
    }
}
```

`ExecutionSource.BackgroundJob` 此前是全仓库无人写入的死枚举，至此有了写入方。

### 接口回调

身份来自回调报文参数，由业务解析后构造：

```csharp
var claims = new Dictionary<string, string>
{
    [GirvsClaimTypes.TenantId] = request.MerchantId,
    [GirvsClaimTypes.IdentityType] = IdentityType.ManagerUser.ToString(),
};

using var _ = EngineContext.Current.PrincipalAccessor.ChangeTo(claims, ExecutionSource.Http);
await HandleCallbackAsync(request);
```

### 启动期发送事件消息

`Girvs.EventBus` 的身份传播链路已经闭合，无需在消费端手工重建：

- 发布端 `Girvs.EventBus/CapEventBus/CapEventBus.cs:22-28` 把当前 `Principal` 序列化进 `girvs-identity` 头
- 消费端 `Girvs.EventBus/CapEventBus/GirvsCapFilter.cs:31-36` 反序列化并 `Change` 恢复

发布时身份为空，`IntegrationIdentityContextSerializer.Serialize` 返回 `null`（`IntegrationIdentityContextSerializer.cs:34-37`），头部不携带身份，消费端得到空 `ClaimsPrincipal`。因此只需在**发布前**建立身份，传播是自动的：

```csharp
using var _ = EngineContext.Current.PrincipalAccessor.ChangeTo(
    userId: "58205e0e-1552-4282-bedc-a92d0afb37df",
    userName: "系统管理员",
    tenantId: Guid.Empty.ToString(),
    identityType: IdentityType.ManagerUser,
    source: ExecutionSource.EventBus);

await eventBus.PublishAsync(@event);
```

旧的 `CapEventBusReSetClaim(CapHeader)` 是在没有自动传播机制时、于消费端手工重建身份的产物，新架构下不再需要等价物。

## 旧 API 迁移对照

| 旧 API | 新写法 |
|---|---|
| `claimManager.SetFromDictionary(dict)` | `using var _ = accessor.ChangeTo(dict, source)` |
| `claimManager.BuildClaimsIdentity(claim)` | `GirvsPrincipalFactory.Create(userId: …, tenantId: …)` |
| `claimManager.ResetClaim(tenantId)`（业务扩展） | `using var _ = accessor.ChangeTo(tenantId: tenantId, identityType: IdentityType.ManagerUser)` |
| `claimManager.CapEventBusReSetClaim(header)` | 发布前 `accessor.ChangeTo(…)`，消费端由 `GirvsCapFilter` 自动恢复 |
| `claimManager.GetTenantId()` | `accessor.Principal.GetTenantId()` |

两处旧行为在新写法下发生变化，均为修复既有缺陷：

1. **`IdentityType` 不再被强制覆盖为 `EventMessageUser`。** 旧 `CapEventBusReSetClaim` 无条件覆盖该项，业务扩展 `ResetClaim` 中传入的 `IdentityType.ManagerUser` 从未生效。新写法按调用方所传取值。
2. **`ClaimTypes.Sid` / `GroupSid` / `Name` 不被识别为 `UserId` / `TenantId` / `UserName`。** 旧 `SetFromDictionary` 同样不识别，这些键只作为普通 claim 保留在 `OtherClaims` 中。业务需改用 `GirvsClaimTypes` 常量。

## 测试

落在已有的 `tests/Girvs.Claims.Tests`，新增 `GirvsPrincipalFactoryTests`：

- `Create` 产出的 principal `Identity.IsAuthenticated` 为 `true`
- `Create` 各具名字段映射到对应 `GirvsClaimTypes`，可被 `ClaimsPrincipalExtensions` 正确读回
- 空值字段不产生 claim，读取时返回 `string.Empty`
- `additionalClaims` 与具名参数冲突时具名参数优先
- `FromClaims` 保留字典原有键，并以参数写入 `ExecutionSource`
- `ChangeTo` 进入作用域后 `Principal` 生效，dispose 后还原为先前身份

## 已知但不纳入本次范围

`Girvs.Quartz/GirvsJob.cs` 目前是空的抽象类，原先建立独立服务作用域并桥接到 `EngineContext` 的逻辑被整段注释。业务在 Job 内通过 `EngineContext.Current.Resolve<T>()` 解析 scoped 服务时会拿到宿主根容器的实例。这与身份无关，是独立缺口，另行处理。

# Girvs ClaimsPrincipal 身份上下文统一设计

## 背景与目标

Girvs 当前同时使用 ASP.NET Core 的 `ClaimsPrincipal`、自定义 `GirvsIdentityClaim`、`Dictionary<string, string>` 和 `IGirvsClaimManager` 表达身份。身份在 HTTP、JWT 与 EventBus 之间反复转换，造成多值 Claim 丢失、读取接口产生写入副作用、后台模块依赖认证模块，以及 CAP 消费端没有恢复身份等问题。

本次变更允许破坏现有公开 API，目标是删除 `GirvsIdentityClaim` 和 `IGirvsClaimManager`，在核心框架内统一以 `ClaimsPrincipal` 表达当前身份。HTTP、JWT 和 EventBus 只负责各自边界上的身份适配，不再建立第二套身份模型。

## 范围

- 核心框架新增当前 `ClaimsPrincipal` 的访问与临时切换能力。
- Repository、DbContext、BaseEntity、Cache、Driven Message 和权限判断统一读取 `ClaimsPrincipal`。
- JWT 签发直接使用 `ClaimsIdentity`。
- HTTP 请求直接使用认证中间件产生的 `HttpContext.User`，并兼容注册用户从请求头补充租户的现有行为。
- EventBus 发布端传播白名单 Claim，消费端在独立 DI Scope 内恢复 `ClaimsPrincipal`。
- 删除 `GirvsIdentityClaim`、`IGirvsClaimManager` 及其相关转换方法，不提供过时兼容层。
- 更新不使用认证模块的样例，使 EventBus 不再要求注册空 ClaimManager。

本次不改变 JWT 的签名算法、过期时间和认证方案配置，不把完整权限快照或访问令牌通过 EventBus 传播，也不引入跨服务身份重新鉴权协议。

## 核心身份契约

核心层提供只读访问器：

```csharp
public interface IGirvsPrincipalAccessor
{
    ClaimsPrincipal Principal { get; }

    IDisposable Change(ClaimsPrincipal principal);
}
```

`GirvsPrincipalAccessor` 的身份解析顺序为：

1. 当前异步执行流通过 `Change` 显式设置的 Principal；
2. `IHttpContextAccessor.HttpContext.User`；
3. 不含 Identity 的空 `ClaimsPrincipal`。

`Change` 使用 `AsyncLocal` 保存栈式上下文，返回的 `IDisposable` 恢复进入前的 Principal。该设计保证嵌套切换可恢复，并隔离并发 CAP 消费、Quartz 任务和普通 HTTP 请求。访问 `Principal` 不解析 Token、不修改状态，也不产生其他副作用。

`IEngine` 暴露 `PrincipalAccessor`，不再暴露 `ClaimManager`。`IGirvsPrincipalAccessor` 由核心模块显式注册，不依赖 `IManager` 的程序集扫描规则。

## Claim 契约

`GirvsClaimTypes` 位于核心层，保留现有 Claim Type 字符串以保持已签发 JWT 和服务间约定兼容：

| 常量 | Claim Type | 含义 | EventBus |
|---|---|---|---|
| `UserId` | `zf_sib` | 用户 ID | 传播 |
| `UserName` | `zf_sname` | 用户名 | 传播 |
| `TenantId` | `zf_tid` | 租户 ID | 传播 |
| `TenantName` | `zf_tname` | 租户名 | 传播 |
| `IdentityType` | `zf_itype` | 原始操作者身份类型 | 传播且不覆盖 |
| `SystemModule` | `zf_csm` | 可访问系统模块 | 传播 |
| `UserType` | `zf_utype` | 用户类型 | 传播 |
| `ExecutionSource` | `zf_exec_source` | 当前执行入口 | 消费端设置 |

`client_id` 纳入 EventBus 白名单。`ClaimTypes.Role`、`role`、`scope` 等多值 Claim 在内存和 JWT 中完整保留，但默认不通过 EventBus 传播。权限明细、访问令牌、Cookie 和未知 Claim 不通过 EventBus 传播，避免泄密与 Broker Header 膨胀。

核心层提供 `ClaimsPrincipal` 扩展方法读取用户 ID、用户名、租户 ID、租户名、身份类型、系统模块和任意 Claim。读取不到值时，字符串返回空字符串；枚举返回现有业务默认值。泛型用户 ID、租户 ID 转换继续使用 `GirvsConvert.ToSpecifiedType`，保持实体和仓储现有类型转换行为。

同一 Type 的多个 Claim 不去重、不转换为 `Dictionary<string, string>`。调用方需要单值时使用 `FindFirst`，需要多值时使用 `FindAll`。

## HTTP 与 JWT

ASP.NET Core 认证中间件仍负责验证 Token 并设置 `HttpContext.User`。框架不再从请求中二次解析 Claims。

为兼容现有注册用户行为，AuthorizePermission 模块在 `UseAuthentication` 之后、`UseAuthorization` 之前加入租户上下文中间件：

- 当身份类型为 `RegisterUser` 时，用请求头 `TenantId`、`TenantName` 替换当前 Identity 中对应的租户 Claim；`TenantName` 继续执行 URL 解码。
- 未认证请求若携带 `TenantId`，创建认证类型为空的临时 Identity，加入租户 ID、租户名、`RegisterUser` 身份类型 Claim，但不把匿名请求标记为已认证。
- 不携带租户请求头时不修改 `HttpContext.User`。

JWT API 以 `ClaimsIdentity` 为主入口：

```csharp
public static string GenerateToken(ClaimsIdentity claimsIdentity);

public static string GenerateToken(
    string userId,
    string userName,
    string tenantId = null,
    string tenantName = null,
    UserType userType = UserType.All,
    IdentityType identityType = IdentityType.ManagerUser,
    SystemModule claimSystemModule = SystemModule.All);
```

参数式重载直接创建 Claim 列表后调用主入口，不再解析 `EngineContext.Current`，也不修改传入集合。JWT Token Descriptor 仍以该 Identity 作为 `Subject`。

## EventBus 身份传播

CAP Header 的键唯一，不能正确表达多值 Claim。因此 EventBus 使用一个带版本的 JSON Header，而不是把每个 Claim 展平为字典：

```csharp
public sealed record IntegrationClaim(
    string Type,
    string Value,
    string ValueType,
    string Issuer);

public sealed record IntegrationIdentityContext(
    int Version,
    IReadOnlyCollection<IntegrationClaim> Claims);
```

Header 名称为 `girvs-identity`，当前 `Version` 为 `1`。发布端从 `IGirvsPrincipalAccessor.Principal` 读取 Claim，只允许上表标记为传播的 Girvs Claim 和 `client_id`，筛选后序列化。没有可传播 Claim 时不写该 Header。序列化后的 UTF-8 数据上限为 8 KiB；超过上限时拒绝发布并报告不包含 Claim 值的错误，避免静默截断身份。

消费端在 `GirvsIntegrationEventHandler.HandleInScopeAsync` 创建的独立 Scope 内：

1. 读取并校验 `girvs-identity`；
2. 只接受 `Version=1`、8 KiB 以内的数据，以及发布白名单中的 Claim Type；
3. 从全部传入 Claim 重建 `ClaimsIdentity`，认证类型设置为 `Girvs.EventBus`；
4. 保留原始 `zf_itype`，添加或替换 `zf_exec_source=EventBus`；
5. 通过该 Scope 内的 `IGirvsPrincipalAccessor.Change` 设置 Principal；
6. 在 `Change` 和 Scope 的生命周期内执行消费逻辑，结束后自动恢复。

无法解析、版本不支持或内容超限时，消费失败并记录结构化错误，让 CAP 按现有策略重试；日志不得输出 Claim 值。消息没有身份 Header 时，以空 Principal 执行，支持系统事件和匿名事件。

`HandleInScopeAsync` 新增包含 `CapHeader` 的重载，订阅者必须把 `[FromCap] CapHeader` 传入。现有不接收 Header 的重载保留用于明确不传播身份的后台消费，但不会隐式读取外部身份。

EventBus 身份属于可信服务间传递的执行上下文，不等价于重新验证用户 JWT。Broker 与生产者仍属于可信边界；消费者的业务授权如要求实时权限，应按用户 ID 重新查询权限，而不是传播完整权限列表。

## 下游迁移

- `BaseEntity` 从 Principal Claim 填充创建人和租户字段。
- Repository、DbContext 扩展和查询条件从 Principal Claim 读取租户及身份类型。
- Cache Key 从 Principal Claim 读取租户 ID。
- Driven `MessageSource` 从 Principal Claim 捕获用户和租户信息；IP 仍只在存在 HTTP 请求时读取。
- AuthorizePermission 的用户类型、客户端 ID 和权限比较改为 `ClaimsPrincipal` 扩展。
- `IdentityType.EventMessageUser` 不再用于覆盖原始身份；保留枚举成员只为历史数据解析，新的执行来源使用 `ExecutionSource` Claim。
- 删除 `SampleClaimManager` 及其注册，证明 EventBus 可以在未引用 AuthorizePermission 的服务中运行。

## 测试策略

核心层测试覆盖：

- HTTP Principal、显式 Principal 和空 Principal 的解析优先级。
- 嵌套 `Change` 的恢复行为以及并发异步流隔离。
- 单值、枚举、泛型 ID 和多值 Claim 的读取。

AuthorizePermission 测试覆盖：

- JWT 参数式重载生成全部 Girvs Claim。
- 传入 `ClaimsIdentity` 时保留同 Type 多值 Claim。
- JWT 生成不依赖 `IGirvsPrincipalAccessor` 或 `EngineContext` 当前身份。
- 已认证注册用户、匿名租户请求及无租户请求的 HTTP 中间件行为。

EventBus 测试覆盖：

- 发布端仅序列化白名单 Claim，并保留允许的同 Type 多值 Claim。
- 未设置身份时不写身份 Header。
- 消费端恢复原始身份并设置 `zf_exec_source=EventBus`。
- 并发消费者之间身份不串流，消费完成后身份被恢复。
- 非法 JSON、未知版本和超限数据进入明确失败路径。
- 不引用 AuthorizePermission 的 Sample 服务仍可发布和消费事件。

相关模块测试覆盖 Repository、BaseEntity、Cache、权限判断和 MessageSource 迁移后行为不变。最终按受影响项目逐个测试，再构建 `Girvs.slnx`。

## 破坏性变更与迁移说明

本次明确允许破坏性 API 变更：

- 删除 `IGirvsClaimManager`、`IdentityClaimManager`、`GirvsIdentityClaim` 和 `GirvsIdentityClaimTypes`。
- 删除 `SetFromHttpRequestToken`、`SetFromDictionary`、`BuildClaimsIdentity` 及基于 ClaimManager 的扩展。
- `IEngine.ClaimManager` 替换为 `IEngine.PrincipalAccessor`。
- 自定义实现 `IGirvsClaimManager` 的应用应删除该实现；需要自定义后台身份时使用 `IGirvsPrincipalAccessor.Change`。
- 读取 `IdentityClaim` 属性的代码改为读取 `Principal` 扩展方法。
- EventBus 订阅者若需要身份传播，应将 `[FromCap] CapHeader` 传给新的 `HandleInScopeAsync` 重载。

Claim Type 的字符串值保持不变，因此现有有效 JWT 在升级后仍可被读取；变化的是框架内部访问 API 和 EventBus Header 格式。

# YARP 网关跨越请求防护实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在 `Girvs.Gateway` 中实现基于 Girvs.Cache Redis Lua 状态机的严格线性流程防护。

**Architecture:** 注册表从独立 `FlowProtection` 配置构建下游步骤索引；中间件在转发前 Reserve，YARP 2.1 响应 Transform 在下游响应后 Commit 或 Release。所有状态转换通过 `IRedisConnectionWrapper` 执行 Lua，流程定义不写入 YARP Route Metadata。

**Tech Stack:** .NET 10、ASP.NET Core、Yarp.ReverseProxy 2.1.0、Girvs.Cache、StackExchange.Redis（经 Girvs.Cache）、xUnit、Microsoft.AspNetCore.TestHost、Testcontainers.Redis。

## 全局约束

- 仅实现严格线性流程；不实现多流程接口、路由模板、Query/JSON 提取、分支或 DAG。
- 启用时必须使用 Girvs.Cache 的 `Type=redis` 资源；内存、SQL Server、`redis-synchronized-memory` 均启动失败。
- 步骤 Path 使用下游路径，从当前 YARP 路由的唯一 `PathRemovePrefix` 还原；忽略 QueryString，Method/Path 忽略大小写精确匹配。
- Ticket 和 AttemptId 都用 `RandomNumberGenerator.GetBytes(32)` 后 Base64Url 编码；日志只准使用 Ticket SHA-256 前 12 位指纹。
- 不保存或校验 `userId`、`tenantId`；明确接受 Ticket 跨登录会话复用。下游服务仍保留认证、租户、资源授权与业务幂等。
- `businessId` 不得做 Redis Key 或唯一安全依据；必须与 Ticket 的流程及当前步骤一起校验。
- 禁止 GET/SET 分离状态推进；CreateAndReserve、Reserve、Commit、Release、Delete 都必须是 Lua。
- 仅下游 2xx Commit；失败、超时、转发异常 Release（第一步为 Delete）；完成流程只保留 60 秒。

## 文件结构

| 路径 | 职责 |
| --- | --- |
| `Girvs.Gateway/FlowProtection/Configuration/FlowProtectionOptions.cs` | 配置对象 |
| `Girvs.Gateway/FlowProtection/FlowModels.cs` | 不可变定义、匹配和状态请求/结果 |
| `Girvs.Gateway/FlowProtection/FlowDefinitionRegistry.cs` | 配置验证和索引 |
| `Girvs.Gateway/FlowProtection/IFlowStateStore.cs` | 原子状态转换契约 |
| `Girvs.Gateway/FlowProtection/RedisFlowStateStore.cs` | Lua 实现 |
| `Girvs.Gateway/FlowProtection/FlowTicketService.cs` | 票据和状态机门面 |
| `Girvs.Gateway/FlowProtection/FlowAttemptContext.cs` | 单请求上下文 |
| `Girvs.Gateway/FlowProtection/DownstreamPathResolver.cs` | 入口/下游路径映射 |
| `Girvs.Gateway/FlowProtection/FlowTicketMiddleware.cs` | 入站 Reserve 与拒绝 |
| `Girvs.Gateway/FlowProtection/FlowResponseTransform.cs` | YARP 响应 Commit/Release |
| `Girvs.Gateway/FlowProtection/FlowProtectionErrors.cs` | RFC 7807 错误 |
| `Girvs.Gateway/FlowProtection/FlowProtectionServiceCollectionExtensions.cs` | DI 和 Redis 配置校验 |
| `tests/Girvs.Gateway.Tests/*FlowProtection*.cs` | 单元、Redis、YARP 集成测试 |

---

### Task 1: 配置模型与启动期注册表

**Files:**
- Create: `Girvs.Gateway/FlowProtection/Configuration/FlowProtectionOptions.cs`
- Create: `Girvs.Gateway/FlowProtection/FlowModels.cs`
- Create: `Girvs.Gateway/FlowProtection/FlowDefinitionRegistry.cs`
- Create: `tests/Girvs.Gateway.Tests/FlowDefinitionRegistryTests.cs`
- Modify: `Girvs.Gateway/GlobalUsings.cs`

**Interfaces:**
- Produces: `bool TryGetStep(string method, PathString path, out FlowStepMatch match)`。
- Consumes: `FlowProtectionOptions`，由 Task 4 注入。

- [ ] **Step 1: 写入失败测试**

```csharp
[Theory]
[InlineData("", "POST", "/api/a")]
[InlineData("f1", "BAD METHOD", "/api/a")]
[InlineData("f1", "POST", "api/a")]
public void 非法步骤_构造注册表时抛出配置异常(string flowId, string method, string path)
{
    Assert.Throws<GirvsException>(() => new FlowDefinitionRegistry(TestFlows.Create(flowId, method, path, "POST", "/api/b")));
}

[Fact]
public void 不同流程重复MethodPath_构造注册表时抛出配置异常()
    => Assert.Throws<GirvsException>(() => new FlowDefinitionRegistry(TestFlows.CreateWithDuplicateStep()));
```

- [ ] **Step 2: 运行失败测试**

Run: `dotnet test tests/Girvs.Gateway.Tests/Girvs.Gateway.Tests.csproj --filter FullyQualifiedName~FlowDefinitionRegistryTests --no-restore --nologo`

Expected: FAIL，缺少注册表及配置类型。

- [ ] **Step 3: 实现最小配置和不可变索引**

```csharp
public sealed class FlowProtectionOptions
{
    public bool Enabled { get; set; }
    public int DefaultTtlSeconds { get; set; } = 300;
    public int LockTtlSeconds { get; set; } = 30;
    public int CompletedTtlSeconds { get; set; } = 60;
    public string TicketHeaderName { get; set; } = "X-Flow-Ticket";
    public string BusinessIdHeaderName { get; set; } = "X-Business-Id";
    public List<FlowDefinitionOptions> Flows { get; set; } = [];
}
public sealed class FlowDefinitionRegistry
{
    public FlowDefinitionRegistry(FlowProtectionOptions options) { /* 校验后创建只读字典 */ }
    public bool TryGetStep(string method, PathString path, out FlowStepMatch match) { /* 精确查询 */ }
}
```

严格校验 FlowId 唯一、每个流程至少两步、合法 HTTP Method、Path 以 `/` 开始、流程内/跨流程重复、TTL 为 `1..1800`、锁 TTL 为 `1..流程 TTL`。Path 去除末尾 `/`（根路径除外），键用 `OrdinalIgnoreCase`。

- [ ] **Step 4: 运行通过测试并提交**

Run: `dotnet test tests/Girvs.Gateway.Tests/Girvs.Gateway.Tests.csproj --filter FullyQualifiedName~FlowDefinitionRegistryTests --no-restore --nologo`

Expected: PASS，新增成功匹配、重复、空步骤和非法 TTL 测试。

```bash
git add Girvs.Gateway/FlowProtection/Configuration/FlowProtectionOptions.cs Girvs.Gateway/FlowProtection/FlowModels.cs Girvs.Gateway/FlowProtection/FlowDefinitionRegistry.cs Girvs.Gateway/GlobalUsings.cs tests/Girvs.Gateway.Tests/FlowDefinitionRegistryTests.cs
git commit -m "feat: 增加流程保护配置注册表"
```

### Task 2: 状态契约、随机票据和统一错误

**Files:**
- Create: `Girvs.Gateway/FlowProtection/IFlowStateStore.cs`
- Create: `Girvs.Gateway/FlowProtection/FlowTicketService.cs`
- Create: `Girvs.Gateway/FlowProtection/FlowAttemptContext.cs`
- Create: `Girvs.Gateway/FlowProtection/FlowProtectionErrors.cs`
- Create: `tests/Girvs.Gateway.Tests/FlowTicketServiceTests.cs`

**Interfaces:**
- Produces: `CreateAndReserveAsync`、`ReserveAsync`、`CommitAsync`、`ReleaseAsync`、`DeleteAsync`。
- Consumes: Task 1 的 `FlowStepMatch`。

- [ ] **Step 1: 写入随机性和错误映射失败测试**

```csharp
[Fact]
public void CreateTicket_生成43字符Base64Url且每次不同()
{
    var service = CreateService();
    Assert.Matches("^[A-Za-z0-9_-]{43}$", service.CreateTicket());
    Assert.NotEqual(service.CreateTicket(), service.CreateTicket());
}

[Fact]
public async Task Reserve跳步_映射为FLOW_STEP_NOT_ALLOWED()
{
    var service = CreateService(new FakeFlowStateStore(FlowStoreResult.Failed(FlowErrorCode.FlowStepNotAllowed)));
    var error = await Assert.ThrowsAsync<FlowProtectionException>(() => service.ReserveNextStepAttemptAsync("ticket", "biz", TestFlows.Match(2), default));
    Assert.Equal("FLOW_STEP_NOT_ALLOWED", error.Code);
}
```

- [ ] **Step 2: 运行失败测试**

Run: `dotnet test tests/Girvs.Gateway.Tests/Girvs.Gateway.Tests.csproj --filter FullyQualifiedName~FlowTicketServiceTests --no-restore --nologo`

Expected: FAIL，缺少状态机接口。

- [ ] **Step 3: 实现状态请求和门面**

```csharp
public interface IFlowStateStore
{
    Task<FlowStoreResult> CreateAndReserveAsync(FlowReserveRequest request, CancellationToken cancellationToken);
    Task<FlowStoreResult> ReserveAsync(FlowReserveRequest request, CancellationToken cancellationToken);
    Task<FlowStoreResult> CommitAsync(FlowCommitRequest request, CancellationToken cancellationToken);
    Task<FlowStoreResult> ReleaseAsync(FlowReleaseRequest request, CancellationToken cancellationToken);
    Task<FlowStoreResult> DeleteAsync(FlowReleaseRequest request, CancellationToken cancellationToken);
}
public sealed class FlowAttemptContext
{
    public required string Ticket { get; init; }
    public required string AttemptId { get; init; }
    public required FlowStepMatch Step { get; init; }
    public required bool IsFirstStep { get; init; }
    public bool IsFinalized { get; set; }
}
```

Ticket/AttemptId 使用 `RandomNumberGenerator.GetBytes(32)` 与 `WebEncoders.Base64UrlEncode`；Ticket 指纹为 SHA-256 十六进制前 12 位。请求模型不得有 userId/tenantId。公开错误仅包括 `FLOW_NOT_FOUND`、`FLOW_EXPIRED`、`FLOW_BUSINESS_MISMATCH`、`FLOW_STEP_NOT_ALLOWED`、`FLOW_IN_PROGRESS`、`FLOW_COMPLETED`。

- [ ] **Step 4: 运行通过测试并提交**

Run: `dotnet test tests/Girvs.Gateway.Tests/Girvs.Gateway.Tests.csproj --filter FullyQualifiedName~FlowTicketServiceTests --no-restore --nologo`

Expected: PASS，含第一步创建、下一步 Reserve 和请求模型无主体字段断言。

```bash
git add Girvs.Gateway/FlowProtection/IFlowStateStore.cs Girvs.Gateway/FlowProtection/FlowTicketService.cs Girvs.Gateway/FlowProtection/FlowAttemptContext.cs Girvs.Gateway/FlowProtection/FlowProtectionErrors.cs tests/Girvs.Gateway.Tests/FlowTicketServiceTests.cs
git commit -m "feat: 定义流程票据状态机契约"
```

### Task 3: Redis Lua 原子状态存储

**Files:**
- Create: `Girvs.Gateway/FlowProtection/RedisFlowStateStore.cs`
- Create: `tests/Girvs.Gateway.Tests/RedisFlowStateStoreTests.cs`
- Modify: `Girvs.Gateway/Girvs.Gateway.csproj`
- Modify: `tests/Girvs.Gateway.Tests/Girvs.Gateway.Tests.csproj`

**Interfaces:**
- Consumes: Task 2 的 `IFlowStateStore` 和 `IRedisConnectionWrapper.GetDatabaseAsync()`。
- Produces: 对 `flow:ticket:{ticket}` Hash 的五个原子转换。

- [ ] **Step 1: 加入依赖并写 Redis 并发失败测试**

网关项目增加 `Girvs.Cache` ProjectReference；测试项目增加 `Microsoft.AspNetCore.TestHost` 与 `Testcontainers.Redis`。测试 fixture 必须启动容器、清空逻辑数据库并在失败时报告 Docker 前置条件。

```csharp
[Fact]
public async Task 两个同步骤Reserve_只有一个成功()
{
    await _store.CreateAndReserveAsync(TestFlows.Reserve("ticket", "start", 0), default);
    await _store.ReleaseAsync(new("ticket", "start"), default);
    var results = await Task.WhenAll(
        _store.ReserveAsync(TestFlows.Reserve("ticket", "a", 0), default),
        _store.ReserveAsync(TestFlows.Reserve("ticket", "b", 0), default));
    Assert.Single(results.Where(x => x.IsSuccess));
    Assert.Single(results.Where(x => x.ErrorCode == FlowErrorCode.FlowInProgress));
}
```

- [ ] **Step 2: 运行失败测试**

Run: `dotnet test tests/Girvs.Gateway.Tests/Girvs.Gateway.Tests.csproj --filter FullyQualifiedName~RedisFlowStateStoreTests --nologo`

Expected: FAIL，缺少 Redis 存储实现；若 Docker 未运行，报告容器前置条件而非伪造成功。

- [ ] **Step 3: 实现 Lua 和 C# 映射**

每个脚本传入单个 `flow:ticket:{ticket}` 键并用 Redis `TIME` 计算锁时钟。Reserve 的核心如下，完整脚本还要校验 flowId/businessId、恢复超时锁、映射全部错误码：

```lua
if redis.call('EXISTS', KEYS[1]) == 0 then return 'FLOW_NOT_FOUND' end
if redis.call('HGET', KEYS[1], 'status') == 'completed' then return 'FLOW_COMPLETED' end
if redis.call('HGET', KEYS[1], 'nextIndex') ~= ARGV[3] then return 'FLOW_STEP_NOT_ALLOWED' end
redis.call('HSET', KEYS[1], 'status', 'processing', 'attemptId', ARGV[4], 'lockUntilUnixMs', ARGV[5])
return 'OK'
```

CreateAndReserve 原子写入 Hash/TTL；Commit 先匹配 AttemptId，非最后一步恢复 active 并递增，最后一步 completed 后 `PEXPIRE 60000`；它只可首次写 `X-Flow-Business-Id` 或严格比对已有值。Release/Delete 只接受匹配 AttemptId。Redis 网络异常不得吞掉，交给网关失败关闭。

- [ ] **Step 4: 运行通过测试并提交**

Run: `dotnet test tests/Girvs.Gateway.Tests/Girvs.Gateway.Tests.csproj --filter FullyQualifiedName~RedisFlowStateStoreTests --nologo`

Expected: PASS，覆盖并发、跳步、businessId 不符、Release 重试、完成重放、锁超时、AttemptId 不符和 60 秒完成 TTL。

```bash
git add Girvs.Gateway/Girvs.Gateway.csproj Girvs.Gateway/FlowProtection/RedisFlowStateStore.cs tests/Girvs.Gateway.Tests/Girvs.Gateway.Tests.csproj tests/Girvs.Gateway.Tests/RedisFlowStateStoreTests.cs
git commit -m "feat: 使用 Redis Lua 管理流程状态"
```

### Task 4: DI、Redis 前置校验与下游路径解析

**Files:**
- Create: `Girvs.Gateway/FlowProtection/FlowProtectionServiceCollectionExtensions.cs`
- Create: `Girvs.Gateway/FlowProtection/DownstreamPathResolver.cs`
- Create: `tests/Girvs.Gateway.Tests/FlowProtectionRegistrationTests.cs`
- Create: `tests/Girvs.Gateway.Tests/DownstreamPathResolverTests.cs`
- Modify: `Girvs.Gateway/GirvsGatewayExtensions.cs`

**Interfaces:**
- Produces: `AddFlowProtection(this IServiceCollection, IConfiguration)` 与 `PathString? Resolve(HttpContext)`。
- Consumes: Task 1-3 和 YARP Route 的 `PathRemovePrefix`。

- [ ] **Step 1: 写入失败测试**

```csharp
[Fact]
public void 启用但未配置Redis资源_构建服务提供程序失败()
{
    var services = new ServiceCollection();
    services.AddFlowProtection(TestConfiguration.Enabled());
    Assert.Throws<GirvsException>(() => services.BuildServiceProvider(validateScopes: true));
}
[Fact]
public void 入口路径含服务名前缀_还原下游路径()
{
    var context = TestHttpContext.WithRoute("/order/{**catch-all}", "order", "/order/api/pay1");
    Assert.Equal("/api/pay1", new DownstreamPathResolver().Resolve(context)?.Value);
}
```

- [ ] **Step 2: 运行失败测试**

Run: `dotnet test tests/Girvs.Gateway.Tests/Girvs.Gateway.Tests.csproj --filter "FullyQualifiedName~FlowProtectionRegistrationTests|FullyQualifiedName~DownstreamPathResolverTests" --no-restore --nologo`

Expected: FAIL，缺少注册扩展和解析器。

- [ ] **Step 3: 实现严格注册与映射**

从 `FlowProtection` 节绑定 options。启用时读取 `CacheConfig.DistributedCacheConfig.ConnectionRef` 和 Girvs `Resources`，仅 `Type=redis`（OrdinalIgnoreCase）且存在 `IRedisConnectionWrapper` 才通过；注册 `FlowDefinitionRegistry`、`RedisFlowStateStore`、`FlowTicketService`。解析器只接受一个 `PathRemovePrefix`，且入口路径必须以此段开头；缺失、多段或不支持变换返回 null，由中间件返回 500 安全配置错误，不能放行。

- [ ] **Step 4: 运行通过测试并提交**

Run: `dotnet test tests/Girvs.Gateway.Tests/Girvs.Gateway.Tests.csproj --filter "FullyQualifiedName~FlowProtectionRegistrationTests|FullyQualifiedName~DownstreamPathResolverTests|FullyQualifiedName~GirvsGatewayExtensionsTests" --no-restore --nologo`

Expected: PASS，既有网关注册测试保持通过。

```bash
git add Girvs.Gateway/GirvsGatewayExtensions.cs Girvs.Gateway/FlowProtection/FlowProtectionServiceCollectionExtensions.cs Girvs.Gateway/FlowProtection/DownstreamPathResolver.cs tests/Girvs.Gateway.Tests/FlowProtectionRegistrationTests.cs tests/Girvs.Gateway.Tests/DownstreamPathResolverTests.cs
git commit -m "feat: 注册网关流程保护服务"
```

### Task 5: 入站中间件、ProblemDetails 与响应 Transform

**Files:**
- Create: `Girvs.Gateway/FlowProtection/FlowTicketMiddleware.cs`
- Create: `Girvs.Gateway/FlowProtection/FlowResponseTransform.cs`
- Modify: `Girvs.Gateway/FlowProtection/FlowProtectionErrors.cs`
- Create: `tests/Girvs.Gateway.Tests/FlowTicketMiddlewareTests.cs`
- Create: `tests/Girvs.Gateway.Tests/FlowProtectionYarpIntegrationTests.cs`

**Interfaces:**
- Consumes: Task 1 的索引、Task 2 门面、Task 4 解析器、YARP `ITransformProvider`。
- Produces: 内部 `FlowAttemptContext`、响应 Header 和最终状态。

- [ ] **Step 1: 写入中间件和真实转发失败测试**

```csharp
[Fact]
public async Task 第二步无Ticket_返回403且不调用下游()
{
    var context = TestHttpContext.For("POST", "/order/api/role1");
    var called = false;
    await CreateMiddleware().InvokeAsync(context, _ => { called = true; return Task.CompletedTask; });
    Assert.False(called);
    Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
    Assert.Contains("FLOW_NOT_FOUND", await context.Response.ReadBodyAsync());
}

[Fact]
public async Task 两个同步骤请求_只有一个到达测试下游()
{
    await using var host = await FlowGatewayHost.StartAsync(_redis, downstream => downstream.DelaySuccess("/api/role1"));
    var ticket = await host.StartFlowAsync("biz-1");
    var results = await Task.WhenAll(host.SendStepAsync("/order/api/role1", ticket, "biz-1"), host.SendStepAsync("/order/api/role1", ticket, "biz-1"));
    Assert.Single(results.Where(x => x.StatusCode == HttpStatusCode.OK));
    Assert.Equal(1, host.Downstream.CallCount("/api/role1"));
}
```

- [ ] **Step 2: 运行失败测试**

Run: `dotnet test tests/Girvs.Gateway.Tests/Girvs.Gateway.Tests.csproj --filter "FullyQualifiedName~FlowTicketMiddlewareTests|FullyQualifiedName~FlowProtectionYarpIntegrationTests" --nologo`

Expected: FAIL，缺少中间件和 Transform。

- [ ] **Step 3: 实现入站和响应状态机**

中间件对不匹配步骤直接 `await next(context)`；命中第 0 步调用 CreateAndReserve，后续强制 `X-Flow-Ticket` 和 `X-Business-Id` 后 Reserve，并把上下文存入 `HttpContext.Items`。状态拒绝统一执行：

```csharp
await Results.Problem(
    type: $"https://errors.example.com/flow/{error.Slug}", title: error.Title,
    statusCode: StatusCodes.Status403Forbidden,
    extensions: new Dictionary<string, object?> { ["code"] = error.Code, ["traceId"] = context.TraceIdentifier })
    .ExecuteAsync(context);
```

在 `try/finally` 中，未被 Transform 终结的 Attempt：第一步 `DeleteAsync`，后续 `ReleaseAsync`。Transform 以 `ITransformProvider.Apply` 调 `context.AddResponseTransform`；`ProxyResponse` 为 2xx 才 Commit，其余/空响应 Release。Commit 后写 `X-Flow-Ticket`、`X-Flow-Next-Index` 或 `X-Flow-Completed` 和 `Cache-Control: no-store`；消费并移除下游 `X-Flow-Business-Id`。Commit Redis 异常将 HTTP 响应改为 503 并 `SuppressResponseBody=true`；仅成功 Commit/Release 才设 `IsFinalized=true`。

- [ ] **Step 4: 运行通过测试并提交**

Run: `dotnet test tests/Girvs.Gateway.Tests/Girvs.Gateway.Tests.csproj --filter "FullyQualifiedName~FlowTicketMiddlewareTests|FullyQualifiedName~FlowProtectionYarpIntegrationTests" --nologo`

Expected: PASS，覆盖完整流程、跳步、500 后重试、超时 Release、businessId 回填、另一流程 Ticket 串用、完成重放、Header 不泄漏。

```bash
git add Girvs.Gateway/FlowProtection/FlowTicketMiddleware.cs Girvs.Gateway/FlowProtection/FlowResponseTransform.cs Girvs.Gateway/FlowProtection/FlowProtectionErrors.cs tests/Girvs.Gateway.Tests/FlowTicketMiddlewareTests.cs tests/Girvs.Gateway.Tests/FlowProtectionYarpIntegrationTests.cs
git commit -m "feat: 根据下游响应推进流程步骤"
```

### Task 6: CORS、示例配置、README 与最终验证

**Files:**
- Modify: `Girvs.Gateway/README.md`
- Modify: `samples/gateway-k8s/Gateway/Program.cs`
- Create: `samples/gateway-k8s/Gateway/appsettings.FlowProtection.json`
- Create: `tests/Girvs.Gateway.Tests/FlowProtectionCorsTests.cs`

**Interfaces:**
- Consumes: Tasks 1-5 的配置与 Header 协议。
- Produces: 可复制的接入方式和最终交付验证。

- [ ] **Step 1: 写入 CORS 暴露 Header 失败测试**

```csharp
[Fact]
public async Task 成功响应_CORS暴露流程Header()
{
    await using var host = await FlowGatewayHost.StartAsync(_redis, configureCors: true);
    var response = await host.SendFirstStepAsync("biz-1");
    Assert.Contains("X-Flow-Ticket", response.Headers.GetValues("Access-Control-Expose-Headers"));
    Assert.Contains("X-Flow-Completed", response.Headers.GetValues("Access-Control-Expose-Headers"));
}
```

- [ ] **Step 2: 运行失败测试**

Run: `dotnet test tests/Girvs.Gateway.Tests/Girvs.Gateway.Tests.csproj --filter FullyQualifiedName~FlowProtectionCorsTests --nologo`

Expected: FAIL，样例尚未公开流程 Header。

- [ ] **Step 3: 编写接入示例与 README**

README 和样例必须包含 Redis Resource + `CacheConfig`、`FlowProtection` JSON、认证授权后的代理分支中间件、以及：

```csharp
builder.Services.AddCors(options => options.AddPolicy("gateway", policy => policy
    .AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()
    .WithExposedHeaders("X-Flow-Ticket", "X-Flow-Next-Index", "X-Flow-Completed")));
```

README 说明前端按 `flowId + businessId` 仅以内存保存 Ticket，后续自动附 Ticket/businessId，收到 `FLOW_*` 403 立即清除。列出错误码和安全限制：本功能不替代鉴权、资源授权与幂等，且 Ticket 可跨登录会话复用。

- [ ] **Step 4: 运行测试、构建、回归与安全检查**

Run: `dotnet test tests/Girvs.Gateway.Tests/Girvs.Gateway.Tests.csproj --nologo`

Expected: PASS，含 CORS、Redis 与既有网关测试。

Run: `dotnet build Girvs.Gateway/Girvs.Gateway.csproj --no-restore --nologo && dotnet test Girvs.slnx --no-restore --nologo && git diff --check`

Expected: 构建与全部测试通过，且无空白错误。若 Docker 不可用，Redis 集成测试必须明确 Skip 原因，交付时不可宣称完整集成测试通过。

- [ ] **Step 5: 提交**

```bash
git add Girvs.Gateway/README.md samples/gateway-k8s/Gateway/Program.cs samples/gateway-k8s/Gateway/appsettings.FlowProtection.json tests/Girvs.Gateway.Tests/FlowProtectionCorsTests.cs
git commit -m "docs: 补充网关流程防护接入说明"
```

## 计划自检

- 覆盖性：Task 1 覆盖配置；Task 2-3 覆盖随机值和全部 Lua 状态转换；Task 4 覆盖 Girvs.Cache 与下游路径；Task 5 覆盖请求/响应、异常和真实 YARP；Task 6 覆盖 CORS、文档和全量验证。
- 约束一致性：没有 Saga、Route Metadata 步骤、JWT、GET/SET 推进或主体绑定；明确保留 Ticket 跨会话风险和下游授权责任。
- 类型一致性：后续任务只依赖 `FlowStepMatch`、`IFlowStateStore`、`FlowTicketService`、`FlowAttemptContext`、`DownstreamPathResolver` 与 `FlowResponseTransform` 的前序定义。

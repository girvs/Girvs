# EventBus 自动身份作用域实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 让继承 `GirvsIntegrationEventHandler<T>` 的 CAP 处理器自动获得并清理 EventBus `ClaimsPrincipal` 与 `EngineContext`，子类不再主动调用 `HandleInScopeAsync`。

**Architecture:** 扩展现有 Scoped `GirvsCapFilter`，通过 CAP `ConsumerDescriptor.ImplTypeInfo` 判断目标处理器，在订阅执行前建立上下文、成功或异常后幂等恢复。将 Header 到 Principal 的转换从处理器基类提取为独立内部组件，旧 `HandleInScopeAsync` 只作为兼容入口保留。

**Tech Stack:** C#、.NET 8/9/10、DotNetCore.CAP 8.3.1/10.0.1、ASP.NET Core DI、xUnit。

## Global Constraints

- 自动身份作用域只覆盖继承 `GirvsIntegrationEventHandler<TIntegrationEvent>` 的处理器。
- 普通 `ICapSubscribe` 订阅者不得修改 `EngineContext` 或 `IGirvsPrincipalAccessor`。
- 保持 `[CapSubscribe]` Topic 声明和 `Handle` 方法签名不变。
- 复用 CAP 每次消费已有的 Scoped 服务提供程序，不创建第二个 DI Scope。
- 保留 `HandleInScopeAsync` 兼容 API；新代码和样例不得继续调用它。
- 不修改 `girvs-identity` Header 格式、白名单、版本与 8192 字节限制。
- 成功、异常和初始化失败路径都必须恢复已建立的上下文。
- 不记录 Claim 值及完整身份 Header。
- 所有行为变更严格遵循 RED → GREEN → REFACTOR。

---

### Task 1: 提取 EventBus Principal 构造器

**Files:**
- Create: `Girvs.EventBus/Identity/IntegrationEventPrincipalFactory.cs`
- Modify: `Girvs.EventBus/GirvsIntegrationEventHandler.cs`
- Test: `tests/Girvs.Claims.Tests/GirvsIntegrationEventHandlerTests.cs`

**Interfaces:**
- Consumes: `IntegrationIdentityContextSerializer.Deserialize(string json)`、`GirvsClaimTypes.ExecutionSource`。
- Produces: `internal static ClaimsPrincipal IntegrationEventPrincipalFactory.Create(IDictionary<string, string> headers)`。

- [ ] **Step 1: 修改兼容测试，使测试明确覆盖共享 Principal 构造规则**

在 `GirvsIntegrationEventHandlerTests` 的合法 Header 用例中增加多值 Claim，并断言两项值均被保留：

```csharp
var source = new ClaimsPrincipal(new ClaimsIdentity(
[
    new Claim(GirvsClaimTypes.UserId, "user-1"),
    new Claim(GirvsClaimTypes.IdentityType, IdentityType.ManagerUser.ToString()),
    new Claim(GirvsClaimTypes.ClientId, "client-a"),
    new Claim(GirvsClaimTypes.ClientId, "client-b")
], "Test"));

Assert.Equal(
    ["client-a", "client-b"],
    accessor.Principal.FindAll(GirvsClaimTypes.ClientId).Select(x => x.Value));
```

- [ ] **Step 2: 运行测试确认当前行为基线通过**

Run: `dotnet test tests/Girvs.Claims.Tests/Girvs.Claims.Tests.csproj --filter "FullyQualifiedName~GirvsIntegrationEventHandlerTests" --nologo -m:1 --no-restore`

Expected: PASS。该步骤记录重构前行为基线；Task 1 是无行为变化的提取重构。

- [ ] **Step 3: 创建独立 Principal 构造器**

新增 `IntegrationEventPrincipalFactory`：

```csharp
namespace Girvs.EventBus.Identity;

internal static class IntegrationEventPrincipalFactory
{
    internal static ClaimsPrincipal Create(IDictionary<string, string> headers)
    {
        ArgumentNullException.ThrowIfNull(headers);

        if (!headers.TryGetValue(IntegrationIdentityContextSerializer.HeaderName, out var json)
            || string.IsNullOrEmpty(json))
        {
            return new ClaimsPrincipal();
        }

        var context = IntegrationIdentityContextSerializer.Deserialize(json);
        var claims = context.Claims
            .Where(x => x.Type != GirvsClaimTypes.ExecutionSource)
            .Select(x => new Claim(
                x.Type,
                x.Value,
                string.IsNullOrEmpty(x.ValueType) ? ClaimValueTypes.String : x.ValueType,
                string.IsNullOrEmpty(x.Issuer) ? ClaimsIdentity.DefaultIssuer : x.Issuer))
            .ToList();
        claims.Add(new Claim(
            GirvsClaimTypes.ExecutionSource,
            ExecutionSource.EventBus.ToString()));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Girvs.EventBus"));
    }
}
```

- [ ] **Step 4: 基类兼容入口改用构造器**

删除 `GirvsIntegrationEventHandler<T>.BuildPrincipal`，将原调用替换为：

```csharp
var principal = Identity.IntegrationEventPrincipalFactory.Create(header);
```

- [ ] **Step 5: 运行兼容测试确认重构保持行为**

Run: `dotnet test tests/Girvs.Claims.Tests/Girvs.Claims.Tests.csproj --filter "FullyQualifiedName~GirvsIntegrationEventHandlerTests" --nologo -m:1 --no-restore`

Expected: PASS，合法、空 Header、异常恢复和多值 Claim 均通过。

- [ ] **Step 6: 提交 Principal 构造器重构**

```bash
git add Girvs.EventBus/Identity/IntegrationEventPrincipalFactory.cs Girvs.EventBus/GirvsIntegrationEventHandler.cs tests/Girvs.Claims.Tests/GirvsIntegrationEventHandlerTests.cs
git commit -m "refactor: 提取 EventBus 身份构造器"
```

---

### Task 2: 由 CAP 过滤器自动管理目标处理器上下文

**Files:**
- Modify: `Girvs.EventBus/CapEventBus/GirvsCapFilter.cs`
- Create: `tests/Girvs.Claims.Tests/GirvsCapFilterTests.cs`
- Modify: `tests/Girvs.Claims.Tests/GlobalUsings.cs`

**Interfaces:**
- Consumes: `IntegrationEventPrincipalFactory.Create(IDictionary<string, string>)`、`IGirvsPrincipalAccessor.Change(ClaimsPrincipal)`、`EngineContext.Current.ChangeCurrentThreadServiceProvider(IServiceProvider)`。
- Produces: `GirvsCapFilter` 在 `OnSubscribeExecutingAsync`、`OnSubscribeExecutedAsync`、`OnSubscribeExceptionAsync` 中自动建立和恢复上下文。

- [ ] **Step 1: 写目标处理器自动恢复身份的失败测试**

新增必要 using：

```csharp
global using DotNetCore.CAP.Filter;
global using DotNetCore.CAP.Internal;
global using DotNetCore.CAP.Messages;
global using DotNetCore.CAP.Persistence;
global using Girvs.EventBus.CapEventBus;
global using Microsoft.Extensions.Logging.Abstractions;
global using System.Reflection;
```

在 `GirvsCapFilterTests` 构造 `ExecutingContext`：

```csharp
private static ExecutingContext CreateExecutingContext(
    Type handlerType,
    IDictionary<string, string> headers)
{
    var descriptor = new ConsumerExecutorDescriptor
    {
        ImplTypeInfo = handlerType.GetTypeInfo()
    };
    var mediumMessage = new MediumMessage
    {
        Origin = new Message(headers, new object())
    };
    return new ExecutingContext(new ConsumerContext(descriptor, mediumMessage), []);
}
```

测试不调用 `HandleInScopeAsync`，直接调用过滤器前置回调并读取访问器：

```csharp
[Fact]
public async Task OnSubscribeExecutingAsync_Girvs处理器_自动恢复消息身份()
{
    var (filter, accessor, _) = CreateFilter();
    var principal = CreatePrincipal("event-user");
    var context = CreateExecutingContext(
        typeof(TestIntegrationEventHandler),
        CreateHeaders(principal));

    await filter.OnSubscribeExecutingAsync(context);

    Assert.Equal("event-user", accessor.Principal.GetUserId());
    Assert.Equal(ExecutionSource.EventBus, accessor.Principal.GetExecutionSource());
    Assert.True(accessor.Principal.Identity?.IsAuthenticated);
}
```

- [ ] **Step 2: 运行测试确认 RED**

Run: `dotnet test tests/Girvs.Claims.Tests/Girvs.Claims.Tests.csproj --filter "FullyQualifiedName~GirvsCapFilterTests.OnSubscribeExecutingAsync_Girvs处理器" --nologo -m:1 --no-restore`

Expected: FAIL，当前 `GirvsCapFilter` 只记录日志，Principal 未变化。

- [ ] **Step 3: 实现目标类型识别和前置上下文建立**

将过滤器构造函数调整为接收当前 Scoped `IServiceProvider`：

```csharp
public GirvsCapFilter(
    [NotNull] ILogger<GirvsCapFilter> logger,
    [NotNull] IServiceProvider serviceProvider)
{
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    _serviceProvider = serviceProvider
        ?? throw new ArgumentNullException(nameof(serviceProvider));
}
```

增加恢复句柄与类型判断：

```csharp
private IDisposable? _principalScope;
private IDisposable? _serviceProviderScope;

private static bool IsGirvsIntegrationEventHandler(Type? type)
{
    while (type != null && type != typeof(object))
    {
        if (type.IsGenericType
            && type.GetGenericTypeDefinition() == typeof(GirvsIntegrationEventHandler<>))
        {
            return true;
        }

        type = type.BaseType;
    }

    return false;
}
```

在 `OnSubscribeExecutingAsync` 的现有日志之后建立上下文：

```csharp
if (!IsGirvsIntegrationEventHandler(context.ConsumerDescriptor.ImplTypeInfo?.AsType()))
    return Task.CompletedTask;

try
{
    _serviceProviderScope = EngineContext.Current
        .ChangeCurrentThreadServiceProvider(_serviceProvider);
    var accessor = _serviceProvider.GetRequiredService<IGirvsPrincipalAccessor>();
    var principal = Identity.IntegrationEventPrincipalFactory.Create(
        context.DeliverMessage.Headers);
    _principalScope = accessor.Change(principal);
}
catch
{
    RestoreContext();
    throw;
}

return Task.CompletedTask;
```

- [ ] **Step 4: 运行自动恢复测试确认 GREEN**

Run: `dotnet test tests/Girvs.Claims.Tests/Girvs.Claims.Tests.csproj --filter "FullyQualifiedName~GirvsCapFilterTests.OnSubscribeExecutingAsync_Girvs处理器" --nologo -m:1 --no-restore`

Expected: PASS。

- [ ] **Step 5: 写成功、异常和非目标处理器清理测试**

增加以下测试：

```csharp
[Fact]
public async Task OnSubscribeExecutedAsync_Girvs处理器_恢复进入前上下文()
```

前置设置原 Principal，调用 Executing 后调用 `OnSubscribeExecutedAsync`，断言恢复原 Principal 和原 `EngineContext` 服务提供程序。

```csharp
[Fact]
public async Task OnSubscribeExceptionAsync_Girvs处理器_恢复上下文且不吞异常状态()
```

调用 Executing 后构造 `ExceptionContext`，执行异常回调，断言上下文恢复且 `ExceptionHandled` 仍为 `false`。

```csharp
[Fact]
public async Task OnSubscribeExecutingAsync_普通Cap处理器_不修改身份上下文()
```

使用不继承基类的 `PlainCapSubscriber` 作为 `ImplTypeInfo`，断言 Principal 和 EngineContext 均保持原实例。

```csharp
[Fact]
public async Task OnSubscribeExecutingAsync_没有身份头_使用空Principal()
```

使用空 Header，断言执行期 Principal 无 Claims，结束回调后恢复原 Principal。

```csharp
[Fact]
public async Task OnSubscribeExecutingAsync_身份访问器解析失败_恢复EngineContext()
```

使用未注册 `IGirvsPrincipalAccessor` 的 Scoped Provider 调用前置回调，断言抛出 `InvalidOperationException`，并验证 `EngineContext` 已恢复到进入前的 Provider。

```csharp
[Fact]
public async Task 两个Scoped过滤器并发消费_身份上下文互不共享()
```

从两个独立 DI Scope 分别解析 `GirvsCapFilter`，使用不同用户身份并发执行前置与结束回调；在各自异步执行流中断言只能读取本消息用户，结束后均恢复为空上下文。

- [ ] **Step 6: 运行新增测试确认 RED**

Run: `dotnet test tests/Girvs.Claims.Tests/Girvs.Claims.Tests.csproj --filter "FullyQualifiedName~GirvsCapFilterTests" --nologo -m:1 --no-restore`

Expected: 成功、异常、初始化失败和并发隔离测试至少一项 FAIL，因为完整恢复逻辑尚未实现。

- [ ] **Step 7: 实现幂等逆序清理**

增加统一清理方法：

```csharp
private void RestoreContext()
{
    Interlocked.Exchange(ref _principalScope, null)?.Dispose();
    Interlocked.Exchange(ref _serviceProviderScope, null)?.Dispose();
}
```

在两个结束回调的现有日志逻辑执行前调用：

```csharp
RestoreContext();
```

不要修改 `ExceptionContext.ExceptionHandled`，保持 CAP 重试语义。

- [ ] **Step 8: 运行过滤器测试确认 GREEN**

Run: `dotnet test tests/Girvs.Claims.Tests/Girvs.Claims.Tests.csproj --filter "FullyQualifiedName~GirvsCapFilterTests" --nologo -m:1 --no-restore`

Expected: PASS，目标处理器自动恢复并清理，非目标处理器不受影响。

- [ ] **Step 9: 提交过滤器自动上下文**

```bash
git add Girvs.EventBus/CapEventBus/GirvsCapFilter.cs tests/Girvs.Claims.Tests/GirvsCapFilterTests.cs tests/Girvs.Claims.Tests/GlobalUsings.cs
git commit -m "feat: CAP 自动恢复 Girvs 处理器身份"
```

---

### Task 3: 迁移推荐用法并保留兼容入口

**Files:**
- Modify: `Girvs.EventBus/GirvsIntegrationEventHandler.cs`
- Modify: `samples/Sample.ServiceB/Events/SampleMessageHandler.cs`
- Modify: `tests/Girvs.Claims.Tests/GirvsIntegrationEventHandlerTests.cs`

**Interfaces:**
- Consumes: Task 2 提供的自动过滤器行为。
- Produces: 子类 `Handle` 无需调用 `HandleInScopeAsync` 的示例与兼容性声明。

- [ ] **Step 1: 写 API 与样例迁移约束测试**

在 `GirvsIntegrationEventHandlerTests` 中增加反射测试，保证兼容方法仍存在并标记过时：

```csharp
[Fact]
public void HandleInScopeAsync_兼容方法保留并标记过时()
{
    var methods = typeof(GirvsIntegrationEventHandler<IntegrationEvent>)
        .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
        .Where(x => x.Name == "HandleInScopeAsync")
        .ToArray();

    Assert.Equal(4, methods.Length);
    Assert.All(methods, method =>
        Assert.NotNull(method.GetCustomAttribute<ObsoleteAttribute>()));
}
```

- [ ] **Step 2: 运行测试确认 RED**

Run: `dotnet test tests/Girvs.Claims.Tests/Girvs.Claims.Tests.csproj --filter "FullyQualifiedName~HandleInScopeAsync_兼容方法保留并标记过时" --nologo -m:1 --no-restore`

Expected: FAIL，方法尚未标记 `ObsoleteAttribute`。

- [ ] **Step 3: 标记兼容 API 并更新注释**

为四个 `HandleInScopeAsync` 重载增加：

```csharp
[Obsolete("GirvsIntegrationEventHandler<T> 已由 CAP 过滤器自动建立身份和服务作用域，请直接在 Handle 中编写业务逻辑。")]
```

将类注释更新为：继承该基类的 CAP 处理器由 `GirvsCapFilter` 自动恢复上下文；兼容方法只供旧代码过渡。

- [ ] **Step 4: 简化 SampleMessageHandler**

将 `Handle` 改为直接业务处理：

```csharp
[CapSubscribe(nameof(SampleMessage))]
public override Task Handle(
    SampleMessage @event,
    [FromCap] CapHeader header,
    CancellationToken cancellationToken)
{
    LastReceived = @event.Text;
    return Task.CompletedTask;
}
```

- [ ] **Step 5: 运行 Claims 测试和样例构建确认 GREEN**

Run: `dotnet test tests/Girvs.Claims.Tests/Girvs.Claims.Tests.csproj --nologo -m:1 --no-restore`

Expected: PASS。

Run: `dotnet build samples/Sample.ServiceB/Sample.ServiceB.csproj --no-restore --nologo -m:1`

Expected: 0 errors；只允许项目已有警告与兼容测试调用旧 API 产生的过时警告。

- [ ] **Step 6: 扫描业务处理器中的旧调用**

Run: `rg -n "HandleInScopeAsync" --glob '*.cs' --glob '!tests/**'`

Expected: 只命中 `Girvs.EventBus/GirvsIntegrationEventHandler.cs` 内兼容实现，不命中样例或业务处理器。

- [ ] **Step 7: 提交样例与兼容 API 调整**

```bash
git add Girvs.EventBus/GirvsIntegrationEventHandler.cs samples/Sample.ServiceB/Events/SampleMessageHandler.cs tests/Girvs.Claims.Tests/GirvsIntegrationEventHandlerTests.cs
git commit -m "refactor: EventBus 处理器自动进入身份作用域"
```

---

### Task 4: 全量验证

**Files:**
- Verify: `Girvs.EventBus/**`
- Verify: `tests/Girvs.Claims.Tests/**`
- Verify: `samples/Sample.ServiceB/**`

**Interfaces:**
- Consumes: Tasks 1-3 的最终实现。
- Produces: 可合并的构建与回归证据。

- [ ] **Step 1: 运行 Claims 专项测试**

Run: `dotnet test tests/Girvs.Claims.Tests/Girvs.Claims.Tests.csproj --nologo -m:1 --no-restore`

Expected: 全部通过，新增自动过滤器用例包含在内。

- [ ] **Step 2: 构建全解决方案**

Run: `dotnet build Girvs.slnx --no-restore --nologo -m:1`

Expected: 0 errors；记录但不扩展处理现有包漏洞和代码警告。

- [ ] **Step 3: 运行全量测试**

Run: `dotnet test Girvs.slnx --no-build --no-restore --nologo -m:1`

Expected: Claims、Aspire、Gateway、Refit 测试无新增失败；允许已确认的 Aspire Hosting 基线用例 `Cache连接引用SqlServer资源时组装连接串` 继续失败。

- [ ] **Step 4: 执行静态与 Git 检查**

Run: `git diff --check`

Expected: 无输出，退出码 0。

Run: `git status --short`

Expected: 无未提交文件。

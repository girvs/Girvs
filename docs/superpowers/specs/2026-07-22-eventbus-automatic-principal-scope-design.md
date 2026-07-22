# EventBus 自动身份作用域设计

## 背景

`GirvsIntegrationEventHandler<TIntegrationEvent>` 当前要求派生处理器在 `Handle` 中显式调用 `HandleInScopeAsync`，才能创建独立消费作用域、桥接 `EngineContext` 并恢复消息携带的 `ClaimsPrincipal`。这使身份基础设施细节泄漏到业务处理器中，也容易因遗漏调用导致租户、审计和授权上下文缺失。

本次改造将上述行为集中到已有的 CAP 订阅过滤器 `GirvsCapFilter`，只自动包装继承 `GirvsIntegrationEventHandler<TIntegrationEvent>` 的处理器。其他原生 CAP 订阅者保持现状。

## 目标

- 派生处理器只需实现带 `[CapSubscribe]` 的 `Handle` 并编写业务逻辑。
- 在处理器执行前自动恢复 EventBus `ClaimsPrincipal`，执行后自动恢复进入前的身份。
- 将 CAP 当前消费作用域桥接到 `EngineContext`，不额外创建嵌套 DI 作用域。
- 成功、异常和无身份 Header 场景均能可靠清理上下文。
- 不改变普通 `ICapSubscribe` 订阅者的上下文行为。
- 保留旧的 `HandleInScopeAsync` API，降低框架升级对现有应用的破坏。

## 非目标

- 不为所有 CAP 订阅者全局注入 Girvs 身份。
- 不改变 `[CapSubscribe]` 的 Topic 声明方式。
- 不修改消息身份 Header 的格式、白名单、版本和大小限制。
- 不在本次改造中删除旧的 `HandleInScopeAsync` 方法。

## 总体方案

继续使用 `EventBusModule` 已注册的 `GirvsCapFilter`。过滤器在订阅执行前根据 CAP 的消费者描述信息获取处理器实现类型，并判断其继承链是否包含开放泛型 `GirvsIntegrationEventHandler<>`。

命中目标处理器时，过滤器执行以下步骤：

1. 使用 CAP 当前消费作用域的 `IServiceProvider` 桥接 `EngineContext`。
2. 从消息 Header 构造 `ClaimsPrincipal`。
3. 从当前作用域解析 `IGirvsPrincipalAccessor` 并调用 `Change(principal)`。
4. 保存两个上下文恢复句柄，供成功或异常回调释放。

未命中目标处理器时，过滤器只保留现有日志行为。

CAP 已为每次消费建立依赖注入作用域，因此过滤器不再额外调用 `CreateScope()`。这样处理器构造函数注入的 Scoped 服务、`EngineContext` 和身份访问器使用同一个消费作用域。

## 组件调整

### GirvsCapFilter

过滤器增加以下职责：

- 识别 `GirvsIntegrationEventHandler<>` 派生处理器。
- 在执行前建立 `EngineContext` 和 `ClaimsPrincipal` 上下文。
- 在 `OnSubscribeExecutedAsync` 与 `OnSubscribeExceptionAsync` 中幂等清理上下文。
- 保留现有消息开始、结束和异常日志。

本地程序集核对确认，CAP 通过 `AddSubscribeFilter<T>()` 将过滤器注册为 Scoped；CAP 的每次消费在独立作用域中解析过滤器。因此恢复句柄保存在过滤器实例字段中，并通过一个统一的幂等清理方法释放，不会在并发消息之间共享。实现需同时针对项目使用的 CAP 8.3.1 与 10.0.1 编译验证。

### 身份构造组件

将 `GirvsIntegrationEventHandler<TIntegrationEvent>.BuildPrincipal` 提取为 EventBus 模块内部的身份构造组件，供过滤器和兼容方法共同调用。

转换规则保持不变：

- Header 缺失或为空时使用空 `ClaimsPrincipal`；版本不支持、内容非法或超过大小限制时抛出 `GirvsException`，交由 CAP 失败与重试机制处理。
- 保留白名单内原始 Claims。
- 移除消息中的旧 `ExecutionSource`，统一写入 `ExecutionSource.EventBus`。
- 恢复后的 `ClaimsIdentity.AuthenticationType` 为 `Girvs.EventBus`。

### GirvsIntegrationEventHandler

- 保留当前构造函数和 `Handle` 抽象方法，避免改变 CAP 扫描及现有派生类型签名。
- 保留 `HandleInScopeAsync` 重载作为兼容入口，并标记为过时。
- 兼容入口继续使用提取后的身份构造组件，不形成两套解析逻辑。
- XML 注释改为推荐直接在 `Handle` 中编写业务逻辑。

### 示例处理器

`SampleMessageHandler.Handle` 移除 `HandleInScopeAsync` 调用，直接执行原有业务逻辑，以展示新的推荐用法。

## 执行与清理顺序

执行前：

1. 判断处理器类型。
2. 建立 `EngineContext` 服务提供程序上下文。
3. 建立 `ClaimsPrincipal` 上下文。
4. 调用业务处理器。

执行后或异常时：

1. 释放 `ClaimsPrincipal` 上下文。
2. 释放 `EngineContext` 服务提供程序上下文。
3. 执行现有结束或异常日志逻辑。

清理操作必须幂等，防止异常链路与最终回调重复释放。

## 错误处理

- 无身份 Header：以空 Principal 执行业务处理器。
- 非法身份 Header：沿用现有反序列化策略抛出 `GirvsException`，不记录 Claim 值，并由 CAP 失败与重试机制处理。
- 处理器异常：不吞掉异常，过滤器只负责记录并恢复上下文。
- 上下文初始化失败：让异常进入 CAP 的失败和重试机制，同时清理已经成功建立的句柄。
- 普通 CAP 处理器：不解析身份 Header，也不修改 `EngineContext` 或 Principal。

## 兼容性

- 现有派生类无需立即修改即可继续运行。
- 仍显式调用 `HandleInScopeAsync` 的旧处理器会形成兼容性的嵌套上下文，但能够按栈正确恢复；编译器通过过时提示引导迁移。
- 新代码不再调用 `HandleInScopeAsync`。
- 后续大版本可单独评估删除兼容方法及 `IServiceProvider` 基类构造参数，本次不处理。

## 测试方案

测试采用 TDD，至少覆盖：

1. `GirvsIntegrationEventHandler<T>` 派生处理器在不调用 `HandleInScopeAsync` 时自动获得消息身份。
2. 自动写入 `ExecutionSource.EventBus` 并保持认证状态。
3. 无身份 Header 时获得空 Principal。
4. 成功执行后恢复进入前的 Principal 和 `EngineContext`。
5. 业务异常后仍恢复上下文，异常继续向 CAP 传播。
6. 普通 `ICapSubscribe` 处理器不触发身份上下文变更。
7. 兼容的 `HandleInScopeAsync` 仍可正常工作。
8. 多个消费调用之间不共享上下文恢复句柄。

验证命令：

```bash
dotnet test tests/Girvs.Claims.Tests/Girvs.Claims.Tests.csproj --nologo -m:1
dotnet build Girvs.slnx --no-restore --nologo -m:1
dotnet test Girvs.slnx --no-build --no-restore --nologo -m:1
```

全量测试仍以当前已确认的 Aspire Hosting 基线失败作为已知例外，不将其归因于本次改造。

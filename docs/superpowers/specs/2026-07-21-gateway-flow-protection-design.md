# YARP 网关跨越请求防护设计

## 目标

在 `Girvs.Gateway` 中提供配置驱动的严格线性流程防护。网关在请求转发前验证当前步骤，在下游响应成功后才推进状态；流程状态只存于 Redis，防止跳步、并发重复执行与已完成流程重放。

本设计只覆盖严格线性流程。接口参与多个流程、路由模板/通配符、从 Query 或 JSON Body 提取业务标识、分支与并行节点均不在范围内。

## 已确认的边界

- 流程保护启用时，必须使用 `Girvs.Cache` 配置且资源类型为 `redis`；内存、SQL Server 与 `redis-synchronized-memory` 均使应用启动失败。
- 网关复用 `Girvs.Cache.CacheImps.IRedisConnectionWrapper` 取得 `IDatabase` 并执行 Lua，不在网关包中另建 Redis 连接。
- `Steps.Path` 是下游服务路径，而非网关入口路径。网关依据当前 YARP Route 的 `PathRemovePrefix` 变换把入口 Path 还原为下游 Path；无法静态确定下游 Path 或出现多个候选时拒绝匹配，不进行模糊猜测。
- 流程实例不保存、不校验 `userId` 或 `tenantId`。Ticket 是持有者凭据，允许跨登录会话复用；因此本功能不能防止 Ticket 被复制后的跨主体使用。下游服务仍必须实施认证、租户隔离与业务资源授权。
- `businessId` 不是 Redis Key，也不是唯一安全凭据；它与 Ticket 的流程定义和当前步骤共同校验。
- 第一步是合法入口，不使用该机制阻止其手工调用；其滥用由鉴权、限流、风控和业务幂等处理。

## 配置与注册

新增独立的 `FlowProtection` 配置节，不把步骤号写入 YARP Route Metadata：

```json
{
  "FlowProtection": {
    "Enabled": true,
    "DefaultTtlSeconds": 300,
    "LockTtlSeconds": 30,
    "CompletedTtlSeconds": 60,
    "TicketHeaderName": "X-Flow-Ticket",
    "BusinessIdHeaderName": "X-Business-Id",
    "Flows": [
      {
        "FlowId": "create-user-role",
        "TtlSeconds": 300,
        "Steps": [
          { "Method": "POST", "Path": "/api/user1" },
          { "Method": "POST", "Path": "/api/role1" },
          { "Method": "POST", "Path": "/api/abc1" }
        ]
      }
    ]
  }
}
```

`FlowDefinitionRegistry` 在启动期完成 Path 规范化并建立不可变索引。它会拒绝以下配置：重复 `FlowId`、步骤少于两个、非法 HTTP Method、非 `/` 开头的 Path、同一流程内重复的 Method+Path、不同流程间重复的 Method+Path、TTL 不在 `1..1800` 秒、锁 TTL 不在 `1..流程 TTL` 秒。Method 和 Path 采用与现有网关一致的忽略大小写精确匹配，忽略 QueryString。

`AddGirvsGateway` 扩展注册 `FlowDefinitionRegistry`、`IFlowStateStore`、`RedisFlowStateStore`、`FlowTicketService` 与 YARP Response Transform。启用时验证 Redis wrapper 已由 Girvs.Cache 注册且 `CacheConfig.DistributedCacheConfig.ConnectionRef` 指向 `Resources` 中 Type 为 `redis` 的资源；不满足时抛出启动期配置异常。禁用时不注册流程管线行为。

## Redis 状态与原子操作

Ticket 与 AttemptId 都使用 `RandomNumberGenerator.GetBytes(32)` 后的 Base64Url 字符串。Ticket 只作为 Redis 随机索引，不含可解释的业务数据，也不使用 JWT。日志仅记录 Ticket SHA-256 的前 12 位指纹。

每个流程实例使用键 `flow:ticket:{ticket}`，值为 Hash：

| 字段 | 作用 |
| --- | --- |
| `flowId` | 流程定义 ID |
| `businessId` | 资源绑定值，可在第一步成功后首次写入 |
| `nextIndex` | 唯一允许执行的步骤索引 |
| `status` | `active`、`processing` 或 `completed` |
| `attemptId` | 当前保留锁的随机标识 |
| `lockUntilUnixMs` | 锁租约到期时间 |
| `createdAtUnixMs` | 创建时间 |

`RedisFlowStateStore` 使用内嵌 Lua，不允许 C# 的 GET/SET 分离读改写。其操作为：

- `CreateAndReserveAsync`：第 0 步原子创建 Hash 并进入 `processing`；键已存在时不得覆盖。
- `ReserveAsync`：校验键、`flowId`、`businessId`、`nextIndex` 和 `active` 状态；处理已过期锁后将其改为 `processing`。
- `CommitAsync`：仅匹配同一 AttemptId 时推进；2xx 后非最后一步恢复 `active` 并增加索引，最后一步改为 `completed` 且缩短为 60 秒 TTL。响应的 `X-Flow-Business-Id` 仅能首次绑定空 businessId，或与已有值完全相等。
- `ReleaseAsync`：仅匹配 AttemptId 时从 `processing` 恢复 `active`，不推进索引。
- `DeleteAsync`：仅用于第 0 步下游失败、且 AttemptId 匹配的流程实例。

脚本结果映射为 `FLOW_NOT_FOUND`、`FLOW_EXPIRED`、`FLOW_BUSINESS_MISMATCH`、`FLOW_STEP_NOT_ALLOWED`、`FLOW_IN_PROGRESS` 与 `FLOW_COMPLETED`。流程保护不返回 Redis Key、AttemptId、脚本细节或允许的下一步骤。

## YARP 请求与响应生命周期

使用当前包的 `Yarp.ReverseProxy 2.1.0`：`AddTransforms` / `ITransformProvider` 在构建每个 Route 的 Transform 时注册响应回调。网关宿主保持认证、授权在前，并在 `MapReverseProxy` 分支安装中间件：

```csharp
app.UseAuthentication();
app.UseAuthorization();

app.MapReverseProxy(proxyPipeline =>
{
    proxyPipeline.UseMiddleware<FlowTicketMiddleware>();
});
```

`FlowTicketMiddleware` 仅对命中的下游步骤工作。第 0 步创建 Ticket 并 `CreateAndReserve`；其余步骤要求 `X-Flow-Ticket` 并 `Reserve`。成功保留后创建内部 `FlowAttemptContext`（Ticket、指纹、流程、步骤、AttemptId 和是否终结），放入 `HttpContext.Items`。没有 Ticket 的后续步骤或任何状态校验失败时立即写入 403 ProblemDetails，绝不转发。

`FlowResponseTransform` 只在有 `FlowAttemptContext` 时处理：下游 2xx 调用 Commit，其他 HTTP 状态调用 Release。下游无响应、转发异常或 Transform 未终结时，中间件 `finally` 仅在上下文尚未终结时 Release；该标记使 Release 幂等且避免重复终结。Commit 的 Redis 调用失败或结果不确定时返回网关失败响应而非成功流程 Header，并让锁按租约过期，以免错误报告流程成功。

第 0 步下游非 2xx 时删除其新建状态；中间步骤非 2xx 时 Release 以便重试。下游成功但网关在 Commit 前崩溃时，锁到期后可再次尝试，因此每个下游写接口仍必须保留自己的 `Idempotency-Key` 或等价业务幂等能力。

未完成的成功响应写入：

```text
X-Flow-Ticket: <ticket>
X-Flow-Next-Index: <index>
Cache-Control: no-store
```

完成响应写入：

```text
X-Flow-Completed: true
Cache-Control: no-store
```

下游的 `X-Flow-Business-Id` 只由网关消费，转发前从客户响应移除。RFC 7807 错误为 403，包含稳定 `code`、中文标题和 `traceId`。README 将要求 CORS 策略暴露 `X-Flow-Ticket`、`X-Flow-Next-Index`、`X-Flow-Completed`。

## 文件与职责边界

实现放入 `Girvs.Gateway/FlowProtection/`：

- `Configuration/FlowProtectionOptions.cs`：配置模型。
- `FlowDefinitionRegistry.cs`：启动校验、规范化和步骤索引。
- `FlowTicketService.cs`：请求状态机门面与 Ticket/AttemptId 生成。
- `IFlowStateStore.cs` 与 `RedisFlowStateStore.cs`：Redis 状态接口和 Lua 原子实现。
- `FlowTicketMiddleware.cs`：入站保留与异常兜底释放。
- `FlowResponseTransform.cs`：YARP 2.1 响应提交/释放。
- `FlowAttemptContext.cs`：单请求内部上下文。
- `FlowProtectionErrors.cs`：ProblemDetails 和错误码。

现有 `GirvsGatewayExtensions` 只扩充服务注册和 Transform 注册；既有服务发现、动态 ProxyConfig 与 Route Metadata 不承载流程定义。

## 验证策略

单元测试覆盖：注册和配置拒绝、密码学随机 Ticket/AttemptId、完整流程、直接第二步、跳步、businessId 不匹配、第一步失败删除、中间失败可重试、完成后重放、另一流程接口串用 Ticket。

Redis 集成测试针对真实 Lua 验证：同一步并发只有一个 Reserve 成功、锁到期恢复、Commit/Release AttemptId 匹配、下游回填 businessId 与完成短 TTL。

YARP/TestServer 集成测试验证真实转发、2xx/500/超时路径、成功响应 Header、ProblemDetails、下游 `X-Flow-Business-Id` 不外泄，以及 CORS 暴露 Header。由于明确允许 Ticket 跨登录会话使用，测试不包含用户或租户错配拒绝。

README 将包含 Redis 资源配置、`FlowProtection` 示例、宿主管线顺序、CORS、前端按 `flowId + businessId` 的内存存储与 403 清理流程 Ticket 的用法，以及错误码表和安全限制。

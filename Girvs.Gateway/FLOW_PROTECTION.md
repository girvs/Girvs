# Girvs.Gateway 网关防跳（流程防护 / FlowProtection）设计说明

> 本文档剖析 `Girvs.Gateway` 中 **FlowProtection（流程防护 / 防跳步）** 模块的整体设计、核心状态机、关键不变量、覆盖的防护场景及明确的设计边界。
> 完整配置字段与快速接入示例请配合阅读 [`Girvs.Gateway/README.md`](./README.md#流程防护flowprotection)。

---

## 1. 设计定位：防的是什么，不防什么

在多步骤业务链路（如“下单确认 → 风险校验 → 发起支付”，或按业务类型走不同审批/提交组合）中，攻击者或异常客户端可能绕过前端引导，直接调用后续接口、打乱顺序、并发重放或串用其他订单的上下文。

`Girvs.Gateway` 的 `FlowProtection` 模块在**反向代理网关层**建立了一套**跨请求的严格顺序状态机守卫**：

- **核心机制**：首步签发一次性高熵票据（Ticket）并锁定流程与组合；后续每步必须持票、匹配业务标识、按精确顺位逐一推进；通过 Redis + Lua 原子脚本实现分布式互斥锁与状态迁移。
- **核心目标**：防止客户端**跳步、乱序、跨流程/跨组合串用、跨业务对象串用、并发重复执行同一步、已完成流程重放**。
- **明确边界（不做什么）**：
  - **不做**限流（Rate Limiting）、熔断（Circuit Breaker）、自动重试（Retry）、并发隔离（Bulkhead）或响应缓存。
  - **不做** HTTP 重定向防循环（Redirect Loop）检测。
  - **不替代**身份认证、角色授权、资源级越权校验及下游业务幂等。

---

## 2. 整体架构与组件分工

`FlowProtection` 源码位于 `Girvs.Gateway/FlowProtection/`，采用**请求前预留加锁 + 响应后结算回写 + 异常兜底清理**的两阶段协作模型，整体挂载在 YARP 代理管道内：

```text
客户端请求
  │
  ▼
ASP.NET Core Routing / CORS / AuthN / AuthZ
  │
  ▼
MapReverseProxy 代理管道
  ├─► [请求阶段] FlowTicketMiddleware
  │      1. 检查 Enabled 开关（关闭则直接放行）
  │      2. DownstreamPathResolver：按唯一 PathRemovePrefix 还原下游路径（失败 → 500）
  │      3. FlowDefinitionRegistry：反查是否属于受保护接口（未命中 → 直接放行）
  │      4. FlowTicketService → IFlowStateStore (RedisFlowStateStore Lua 原子脚本)
  │         • 线性首步 / 参数化无票首步 → CreateAndReserve（创建票据 + 加处理锁）
  │         • 持票推进 → Reserve（原子校验状态/流程/业务标识/组合/顺位 + 加处理锁）
  │         • 校验失败 → 403 ProblemDetails（不转发至下游）
  │      5. 将 FlowAttemptContext 存入 HttpContext.Items
  │
  ├─► YARP 反向代理转发至下游业务服务
  │
  ├─► [响应阶段] FlowResponseTransform (ITransformProvider)
  │      • 仅处理 Items 中存在 FlowAttemptContext 的受保护请求
  │      • 下游 2xx → CommitAsync（推进 nextIndex；末步转 completed）+ 回写响应头 + Cache-Control: no-store
  │      • 下游非 2xx 且为首步 → DeleteAsync（撤销整张票据）
  │      • 下游非 2xx 且为中间步 → ReleaseAsync（仅释放锁，保留票据与顺位供重试）
  │      • 结算异常 → 置 503 + 抑制响应体 + rethrow（防误判成功）
  │      • finally → 强制剥离内部响应头 X-Flow-Business-Id
  │
  └─► [兜底阶段] FlowTicketMiddleware finally
         • 若 attempt.IsFinalized == false（客户端断连、代理超时或下游异常未进入结算）
         • 使用独立 5 秒超时令牌清理：首步 DeleteAsync，非首步 ReleaseAsync
```

### 核心组件职责清单

| 文件 | 核心类型 | 职责 |
|---|---|---|
| `Configuration/FlowProtectionOptions.cs` | `FlowProtectionOptions` 等 | 绑定 `FlowProtection` 配置节（开关、TTL、头名、线性/参数化流程定义） |
| `FlowModels.cs` | `FlowDefinition`、`FlowEndpointMatch` 等 | 启动期编译生成的只读流程模型及按组合索引的顺位反查表 |
| `FlowDefinitionRegistry.cs` | `FlowDefinitionRegistry` | 启动期全量校验、编译索引（静态字典 + 模板桶）及运行期 `TryGetEndpoint` 反查 |
| `FlowPathTemplate.cs` | `FlowPathTemplate` | 整段 `{param}` 路径模板解析、规范化、匹配及冲突检测 |
| `DownstreamPathResolver.cs` | `DownstreamPathResolver` | 根据 YARP 路由的唯一 `PathRemovePrefix` 还原下游路径 |
| `FlowTicketMiddleware.cs` | `FlowTicketMiddleware` | 请求前准入判定、预留顺位、403/500 错误输出与 `finally` 兜底清理 |
| `FlowResponseTransform.cs` | `FlowResponseTransform` | YARP 响应转换器：按 2xx/非 2xx 执行提交/删除/释放，回写续用头并剥离内部头 |
| `FlowTicketService.cs` | `FlowTicketService` | 业务语义门面：生成加密随机票据与 `AttemptId`、调用存储抽象、生成日志指纹 |
| `FlowAttemptContext.cs` | `FlowAttemptContext` | 通过 `HttpContext.Items["__GirvsFlowAttemptContext"]` 在请求与响应阶段共享本次尝试状态 |
| `IFlowStateStore.cs` | `IFlowStateStore` | 唯一可替换的状态存储 Seam（创建、预留、提交、释放、删除） |
| `RedisFlowStateStore.cs` | `RedisFlowStateStore` | 内置 Redis 实现，全部状态迁移由 5 段单次执行的 Lua 脚本保证原子性 |
| `FlowProtectionHeaders.cs` | `FlowProtectionHeaders` | 公开请求/响应头契约常量及 CORS 需暴露头集合 |
| `FlowProtectionErrors.cs` | `FlowProtectionErrors`、`FlowProtectionException` | RFC 7807 错误目录及存储错误码到对外错误码的映射 |
| `FlowProtectionServiceCollectionExtensions.cs` | `AddFlowProtection` | DI 注册、Redis 资源配置启动期校验、禁用时的 `DisabledFlowStateStore` 占位保护 |

---

## 3. 核心设计剖析

### 3.1 两种流程形态：线性流程 vs 参数化多组合流程

网关同时支持两种互斥的流程定义模式（同一条 `Flow` 不可混配 `Steps` 与参数化字段）：

1. **线性流程（`Steps`）**：
   - 步骤顺序固定（至少 2 步），例如 `POST /api/prepare` → `POST /api/pay`。
   - 首步（`StepIndex == 0`）直接签发票据，后续步必须凭票据与业务标识按 `1, 2, ...` 顺序推进。
2. **参数化多组合流程（`ParameterHeaderName` + `Interfaces` + `Variants`）**：
   - 适用于同一组业务接口（如 `A / B / C / D / E`）按业务类型走不同路径（如 `type1: A→B→E`、`type2: A→D→E`、`type3: D→E`）的场景（详见 [`docs/adr/0001-flow-protection-parameterized-variants.md`](../docs/adr/0001-flow-protection-parameterized-variants.md)）。
   - **唯一分界判定**：因为同一接口可能既是某组合的第 1 步（如 `DE` 中的 `D`），又是另一组合的中间步（如 `ADE` 中的 `D`），中间件以**请求是否携带票据头（默认 `X-Flow-Ticket`）**作为唯一判别依据：
     - **未携带票据**：一律按「开启新流程」处理，必须携带合法、唯一、非空白且命中已配置组合的前置参数头，且当前接口必须是该组合的第 1 步（无默认兜底组合）。
     - **已携带票据**：一律按「推进既有流程」处理，直接按票据创建时锁定的组合推进；后续步骤不强制重传前置参数，但若携带了参数头（包括空值、同名多值或与锁定组合不同的值），立即按冲突拒绝。

### 3.2 下游路径还原与确定性反查

1. **严格前缀剥离（`DownstreamPathResolver`）**：
   - `Girvs.Gateway` 采用约定路由 `/{ServiceName}/...`，转发时通过 YARP `PathRemovePrefix` 去掉服务名前缀；而 `FlowProtection` 配置的是下游路径（如 `/api/pay`）。
   - 仅当路由配置中**恰好存在 1 个有效 `PathRemovePrefix`**，且请求路径**按整段匹配**该前缀（例如 `/orders` 不会误剥 `/orders-archive/x`）时才返回下游路径；否则返回 `null`，由中间件直接响应 `500 FLOW_CONFIGURATION_ERROR`，**宁可显式失败，绝不猜测或静默放行**。
2. **静态优先 + 整段路径模板（`FlowDefinitionRegistry` / `FlowPathTemplate`）**：
   - 支持整段 `{param}` 模板匹配动态资源路径（如 `/orders/{id}/confirm`，详见 [`docs/adr/0002-flow-protection-path-templates.md`](../docs/adr/0002-flow-protection-path-templates.md)）。
   - 运行期按 `"{METHOD} {normalizedPath}"` 先查静态字典（忽略大小写，最多去除一个尾斜杠），未命中再按 `(Method, 段数)` 桶遍历模板；纯静态配置不分配模板索引。
   - **启动期冲突消灭**：注册表启动期拒绝静态重复、模板重复、同形异名（如 `/orders/{id}/items` 与 `/orders/{orderId}/items`）以及交叉重叠（如 `/orders/{id}/items` 与 `/orders/export/{type}`，并给出见证路径 `/orders/export/items`），保证请求归属不依赖配置声明顺序。

### 3.3 Redis + Lua 原子状态机与锁租约

每张票据在 Redis 中对应一个 Hash 键 `flow:ticket:{ticket}`，结构如下：

| Hash 字段 | 含义 |
|---|---|
| `flowId` | 归属流程标识 |
| `businessId` | 绑定的业务标识（允许首步为空，待首步成功响应时延迟绑定） |
| `nextIndex` | 当前允许执行的步骤顺位（从 `0` 开始，每提交一步加 `1`） |
| `variant` | 锁定的组合键（参数化流程为组合键，线性流程为空串） |
| `status` | `processing`（处理中/持锁）、`active`（等待下一步）、`completed`（已完成） |
| `attemptId` | 当前持锁请求的唯一凭据（32 字节加密随机数） |
| `lockUntilUnixMs` | 处理锁到期时间戳（Unix 毫秒，统一取自 Redis `TIME`） |
| `createdAtUnixMs` | 票据创建时间戳（Unix 毫秒，取自 Redis `TIME`） |

#### 状态迁移图

```text
                    CreateAndReserve (首步)
  [不存在] ─────────────────────────────────────────► [processing (nextIndex=0, 持锁)]
                                                          │           │
                                           首步非 2xx / 异常│           │ 首步 2xx Commit
                                             DeleteAsync  │           │ (非末步)
                                                          ▼           ▼
                                                       [已删除]   [active (nextIndex=1)]
                                                                      │
                                                   Reserve (下一步)    │  ▲ 中间步非 2xx / 异常
                                                   校验顺序/业务/组合   │  │ ReleaseAsync (顺位不变)
                                                                      ▼  │
                                                                 [processing (持锁)]
                                                                      │
                                                                      │ 末步 2xx Commit
                                                                      │ PEXPIRE = CompletedTtlSeconds
                                                                      ▼
                                                                 [completed] ──TTL到期──► [已过期删除]
```

#### 并发与一致性的四条关键不变量

1. **单段 Lua 原子迁移**：`CreateAndReserve`、`Reserve`、`Commit`、`Release`、`Delete` 均在单段 Lua 脚本中完成，利用 Redis 单线程执行特性提供 CAS 语义，保证并发请求在同一顺位上**至多只有一个**能获锁放行。
2. **统一使用 Redis 服务端时钟**：所有 `lockUntilUnixMs` 与当前时间比较均来自 Redis `TIME` 命令，避免多网关实例间本地时钟漂移造成处理锁提前失效或长期死锁。
3. **`AttemptId` 防幽灵写回**：每次预留都生成新的 `AttemptId`。如果请求 A 因下游阻塞超过 `LockTtlSeconds`，其锁可被重试请求 B 合法接管并刷新 `attemptId`；当旧请求 A 迟到返回并尝试 `Commit` / `Release` / `Delete` 时，脚本检测到 `attemptId` 不匹配即拒绝修改，防止旧尝试破坏新尝试的状态。
4. **首步/中间步差异化失败回滚**：
   - 首步失败 → 删除整张票据（避免留下停在第 0 步的悬挂票据）；
   - 中间步失败 → 仅释放锁并保持 `active` 与原 `nextIndex`（允许修正参数或重试该步，无需从头重来）。

### 3.4 业务标识（BusinessId）的延迟绑定与防泄露

- 若首步调用前已确定业务主键（如已有订单号），客户端可在首步请求头（默认 `X-Business-Id`）传入，网关在 `CreateAndReserve` 时即写入票据。
- 若业务主键由首步接口创建后才生成，首步请求可不传 `X-Business-Id`，由下游服务在 2xx 响应中通过内部头 **`X-Flow-Business-Id`** 回传；`CommitScript` 会在票据 `businessId` 为空时完成首次绑定（若已绑定则校验必须一致）。
- **剥离防泄露**：无论结算成功或失败，`FlowResponseTransform` 都在 `finally` 中无条件移除响应头 `X-Flow-Business-Id`，确保该内部协作头绝不透传给客户端。

### 3.5 票据安全与响应防缓存

- **高熵不可预测**：票据与 `AttemptId` 均通过 `RandomNumberGenerator.GetBytes(32)` 生成并作 Base64Url 编码（43 字符），无法枚举或猜测。
- **防中间缓存复用**：所有成功结算的受保护响应一律写入 `Cache-Control: no-store`，避免 CDN、代理或浏览器缓存把带有一次性票据的响应复用到其他请求。
- **指纹工具**：`FlowTicketService.GetTicketFingerprint` 提供 SHA-256 十六进制前 12 位短指纹，用于在日志与监控中脱敏标识票据。

---

## 4. 防止的场景全景分析

以下全部场景均直接对应当前源码实现与 `tests/Girvs.Gateway.Tests/` 中的自动化测试断言。

### 4.1 流程顺序与组合篡改类（防跳步核心）

| # | 防范的场景 | 典型攻击/异常行为 | 触发条件与拦截机制 | 网关响应（不转发下游） |
|---|---|---|---|---|
| 1 | **跳步直调后续接口** | 客户端完成第 0 步后，跳过第 1 步直接调用第 2 步 | `ReserveScript` 校验 `stored.nextIndex != expectedIndex` | `403` `FLOW_STEP_NOT_ALLOWED` |
| 2 | **无票直调非首步接口** | 未走入口步骤，不带 `X-Flow-Ticket` 直接请求中间步或终步 | 线性流程非首步缺票；或参数化流程无票但当前接口非所选组合第 1 步（刻意不暴露组合内容） | `403` `FLOW_NOT_FOUND` |
| 3 | **乱序/回退重调前置步骤** | 已推进到第 2 步，持票再次调用第 0 步或第 1 步 | `ReserveScript` 校验顺位不符（或持票调用首步导致顺位为 0 ≠ 当前 `nextIndex`） | `403` `FLOW_STEP_NOT_ALLOWED` |
| 4 | **跨流程串用票据** | 用流程 A 签发的票据去调用流程 B 的受保护接口 | `ReserveScript` 校验 `stored.flowId != request.flowId` | `403` `FLOW_STEP_NOT_ALLOWED` |
| 5 | **跨组合越界调用接口** | 参数化流程在首步锁定了组合 `ADE`，后续持票调用仅属于组合 `ABE` 的接口 `B` | `ReserveScript` 在该接口的候选组合列表中找不到票据已锁定的 `lockedVariant` | `403` `FLOW_STEP_NOT_ALLOWED` |
| 6 | **推进途中篡改前置参数** | 首步按参数 `type=ADE` 建票锁定组合，推进第 2 步时携带 `type=ABE`、空白值或同名多值头 | 中间件拦截非法头值（空白/多值），或 `ReserveScript` 校验携带参数 `ARGV[6] != lockedVariant` | `403` `FLOW_SELECTOR_MISMATCH` |
| 7 | **无票伪造/缺省前置参数开启流程** | 参数化流程无票请求时，不带前置参数头、传空白、传同名多值头或传未配置取值 | `FlowTicketMiddleware.ReadSelector` 三态严格判定，无默认兜底组合 | `403` `FLOW_SELECTOR_INVALID` |

### 4.2 并发、重放与对象越权类

| # | 防范的场景 | 典型攻击/异常行为 | 触发条件与拦截机制 | 网关响应 |
|---|---|---|---|---|
| 8 | **并发重复执行同一步** | 前端连点或脚本并发发出多个同一票据、同一顺位的请求（如并发重复扣款） | `ReserveScript` 检测到 `status == 'processing'` 且 `lockUntilUnixMs > now`；Lua 原子保证仅 1 个请求获锁 | 1 个放行，其余 `403` `FLOW_IN_PROGRESS` |
| 9 | **已完成流程重放** | 流程走完最后一步后，客户端或抓包脚本在完成保留期内重放末步或历史请求 | 末步 `Commit` 将状态置为 `completed` 并续期 `CompletedTtlSeconds`；`ReserveScript` 优先判定 `status == 'completed'` | `403` `FLOW_COMPLETED` |
| 10 | **过期/伪造票据继续推进** | 票据超过流程 TTL 自动过期，或客户端伪造随机字符串充当票据 | Redis 键不存在，`ReserveScript` 首行 `EXISTS == 0` 拒绝 | `403` `FLOW_NOT_FOUND` |
| 11 | **跨业务对象串用票据** | 拿订单 A 的有效票据推进订单 B 的支付接口，或推进阶段不传 `X-Business-Id` | 中间件要求推进阶段必须非空；`ReserveScript` 校验 `storedBusinessId != request.businessId`；`CommitScript` 校验下游回传值不冲突 | `403` `FLOW_BUSINESS_MISMATCH` |
| 12 | **锁超时剥夺后的旧请求迟到覆盖** | 请求 A 下游卡顿超过 `LockTtlSeconds`，重试请求 B 接管锁后，请求 A 突然返回 2xx | `CommitScript` / `ReleaseScript` / `DeleteScript` 均强制校验 `stored.attemptId == request.attemptId`，拒绝旧请求写入 | 旧请求结算失败 → 网关置 `503` 抑制响应体（YARP 2.3.0 表现为空体 `502`） |

### 4.3 失败收敛、防泄露与配置安全类

| # | 防范的场景 | 风险问题 | 网关防护机制 | 表现结果 |
|---|---|---|---|---|
| 13 | **首步失败留下悬挂票据** | 首步业务校验不通过（返回 4xx/5xx/3xx），若保留票据会占用存储并产生僵尸状态 | `FlowResponseTransform` 检测到首步非 2xx，调用 `DeleteAsync` 彻底删除票据 | 透传下游错误响应，不回写票据头 |
| 14 | **中间步失败阻塞合法重试** | 第 2 步因偶发校验失败或下游抖动返回非 2xx，若直接删票会迫使用户从第 0 步重来，不释放锁则会卡死到租约过期 | `FlowResponseTransform` 调用 `ReleaseAsync` 仅释放处理锁，票据回到 `active` 且 `nextIndex` 不变 | 透传下游错误响应，调用方修正后可立即重试该步 |
| 15 | **客户端断连或代理异常导致长时死锁** | 下游未返回响应、网络超时或客户端主动断开，未进入响应转换阶段 | `FlowTicketMiddleware` 的 `finally` 检查 `!attempt.IsFinalized`，使用**独立于 `RequestAborted` 的 5 秒超时**执行删除（首步）或释放（中间步）；极端清理失败仍有 `LockTtlSeconds` 租约兜底 | 尽快解锁，不阻断后续重试，不覆盖已有响应 |
| 16 | **结算异常造成“假成功”误推进** | 下游已返回 2xx，但网关提交 Redis 状态失败（如 Redis 抖动或锁被剥夺）；若照常把下游 2xx 给客户端，客户端会以为已推进成功 | `FlowResponseTransform` 捕获结算异常后置 `503`、设置 `SuppressResponseBody = true` 并重新抛出，不写任何成功响应头 | 客户端收到 `503`（在 YARP 2.3.0 管道中表现为无响应体 `502`），不会被误导继续推进 |
| 17 | **下游内部业务标识头外泄** | 下游通过 `X-Flow-Business-Id` 向网关回传业务标识，若透传出去可能暴露内部字段 | `FlowResponseTransform` 在 `finally` 中无条件移除 `X-Flow-Business-Id` | 客户端永远看不到该内部响应头 |
| 18 | **路由配置疏漏导致的静默绕过** | 自定义路由未配置 `PathRemovePrefix` 或配置多个前缀导致无法唯一还原下游路径 | `DownstreamPathResolver.Resolve` 返回 `null`，中间件拒绝转发 | `500` `FLOW_CONFIGURATION_ERROR`（RFC 7807），阻断无保护透传 |
| 19 | **带病配置上线运行** | TTL 越界、空步骤/少于 2 步、混配 `Steps` 与参数化、模板冲突、保留头冲突、启用防护却配错 Redis 资源类型（如 `redis-synchronized-memory`） | 注册期校验 Redis 资源；`FlowDefinitionRegistry` 构造期校验并编译全部规则，不合法即抛 `GirvsException` | 启动期即暴露故障，阻止运行期不可预测行为 |

---

## 5. 详细实战示例

下面通过 5 个端到端示例，完整演示网关配置、宿主接入、正常调用链路、Redis 状态演变、各类跳步/篡改/并发/重放的真实拦截报文，以及前端调用方封装方式。

> 约定：以下示例均假设网关通过约定路由暴露 `order` 下游服务（客户端访问 `/order/...`，YARP 通过 `PathRemovePrefix: /order` 转发到下游 `/...`），票据头与业务标识头均使用默认值 `X-Flow-Ticket`、`X-Business-Id`。

---

### 示例一：线性流程 + 下游延迟绑定业务标识（创建订单 → 风控核验 → 确认支付）

**业务背景**：下单第 1 步前还没有订单号，由下游“创建订单”接口生成订单号 `ORD-1001`，并通过内部响应头 `X-Flow-Business-Id` 回传给网关完成票据绑定；后续“风控核验”和“确认支付”必须依次执行，且必须匹配 `ORD-1001`。

#### 1) 网关配置（`appsettings.json`）

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
        "FlowId": "order-checkout",
        "TtlSeconds": 600,
        "Steps": [
          { "Method": "POST", "Path": "/api/orders/create" },
          { "Method": "POST", "Path": "/api/orders/risk-check" },
          { "Method": "POST", "Path": "/api/orders/pay" }
        ]
      }
    ]
  },
  "ModuleConfigurations": {
    "CacheConfig": {
      "DistributedCacheConfig": {
        "Enabled": true,
        "ConnectionRef": ["gateway-redis"]
      }
    }
  },
  "Resources": {
    "gateway-redis": {
      "Type": "redis",
      "Settings": { "Endpoints": "127.0.0.1:6379" }
    }
  }
}
```

#### 2) 网关宿主接入（`Program.cs`）

```csharp
using Girvs.Gateway;
using Girvs.Gateway.FlowProtection;

var builder = WebApplication.CreateBuilder(args);

// 注册服务发现、Redis 包装器与 Girvs 网关（内部自动调用 AddFlowProtection）
builder.Services.AddGirvsGateway(builder.Configuration);

// 浏览器跨域场景必须暴露流程响应头，否则前端脚本读不到 X-Flow-Ticket
builder.Services.AddCors(options => options.AddPolicy("gateway", policy => policy
    .WithOrigins("https://app.example.com")
    .AllowAnyHeader()
    .AllowAnyMethod()
    .WithExposedHeaders(FlowProtectionHeaders.ResponseHeaders)));

var app = builder.Build();

// 建议在启动阶段显式触发一次注册表编译，让误配置立即暴露
_ = app.Services.GetRequiredService<FlowDefinitionRegistry>();

app.UseRouting();
app.UseCors("gateway");
app.UseAuthentication();
app.UseAuthorization();

// 必须挂载在 MapReverseProxy 的代理管道内（不可挂为全局中间件）
app.MapReverseProxy(proxyPipeline => proxyPipeline.UseMiddleware<FlowTicketMiddleware>());
app.Run();
```

#### 3) 正常执行全流程 + Redis 状态变化

**第 1 步（`index = 0`）：创建订单（无票，请求不带 `X-Business-Id`，由下游回传绑定）**

```http
# 客户端 → 网关
POST /order/api/orders/create HTTP/1.1
Host: gateway.example.com
Content-Type: application/json

{ "skuId": "SKU-88", "quantity": 1 }
```

- 网关进入 `CreateAndReserve`：在 Redis 创建 `flow:ticket:<t1>`，写入 `flowId="order-checkout"`、`businessId=""`、`nextIndex="0"`、`status="processing"`、`attemptId="<a1>"`、`TTL=600s`，随后转发下游。
- 下游创建订单 `ORD-1001`，在响应头中附带 `X-Flow-Business-Id: ORD-1001` 返回 `200 OK`。
- 网关 `FlowResponseTransform` 执行 `CommitAsync`：把 `businessId` 首次绑定为 `ORD-1001`，`nextIndex` 推进为 `1`，`status` 变为 `active`，**移除 `X-Flow-Business-Id`**，再回写续用头给客户端：

```http
# 网关 → 客户端（注意：X-Flow-Business-Id 已被剥离）
HTTP/1.1 200 OK
Content-Type: application/json
Cache-Control: no-store
X-Flow-Ticket: <t1>
X-Flow-Next-Index: 1

{ "orderId": "ORD-1001", "amount": 99.00 }
```

> 此时 Redis `flow:ticket:<t1>`：`{ flowId: "order-checkout", businessId: "ORD-1001", nextIndex: "1", variant: "", status: "active", attemptId: "", lockUntilUnixMs: "0" }`（TTL 保持约 600s）。

**第 2 步（`index = 1`）：风控核验（持票 + 业务标识）**

```http
# 客户端 → 网关
POST /order/api/orders/risk-check HTTP/1.1
Host: gateway.example.com
X-Flow-Ticket: <t1>
X-Business-Id: ORD-1001
Content-Type: application/json

{ "orderId": "ORD-1001" }

# 网关 → 客户端（下游 200 OK 提交成功后）
HTTP/1.1 200 OK
Content-Type: application/json
Cache-Control: no-store
X-Flow-Ticket: <t1>
X-Flow-Next-Index: 2

{ "passed": true }
```

> 此时 Redis `flow:ticket:<t1>`：`nextIndex: "2"`, `status: "active"`。

**第 3 步（`index = 2`，末步）：确认支付**

```http
# 客户端 → 网关
POST /order/api/orders/pay HTTP/1.1
Host: gateway.example.com
X-Flow-Ticket: <t1>
X-Business-Id: ORD-1001
Content-Type: application/json

{ "orderId": "ORD-1001", "channel": "wechat" }

# 网关 → 客户端（末步不再回传 X-Flow-Ticket 与 X-Flow-Next-Index）
HTTP/1.1 200 OK
Content-Type: application/json
Cache-Control: no-store
X-Flow-Completed: true

{ "payStatus": "SUCCESS" }
```

> 此时 Redis `flow:ticket:<t1>`：`nextIndex: "3"`, `status: "completed"`, `PEXPIRE` 重置为 `CompletedTtlSeconds = 60s`。

#### 4) 异常/攻击场景如何被拦截

- **场景 A：跳过风控核验，完成第 1 步后直接调支付（`nextIndex = 1`，但 `/pay` 期望 `2`）**

```http
POST /order/api/orders/pay HTTP/1.1
X-Flow-Ticket: <t1>
X-Business-Id: ORD-1001

HTTP/1.1 403 Forbidden
Content-Type: application/problem+json

{
  "type": "https://errors.example.com/flow/step-not-allowed",
  "title": "流程步骤不允许执行",
  "status": 403,
  "code": "FLOW_STEP_NOT_ALLOWED",
  "traceId": "0HN8K1EXAMPLE:00000001"
}
```

- **场景 B：拿 `ORD-1001` 的票据尝试推进 `ORD-1002`（跨订单越权串用）**

```http
POST /order/api/orders/risk-check HTTP/1.1
X-Flow-Ticket: <t1>
X-Business-Id: ORD-1002

HTTP/1.1 403 Forbidden
Content-Type: application/problem+json

{
  "type": "https://errors.example.com/flow/business-mismatch",
  "title": "业务标识不匹配",
  "status": 403,
  "code": "FLOW_BUSINESS_MISMATCH",
  "traceId": "0HN8K1EXAMPLE:00000002"
}
```

- **场景 C：支付完成后 60 秒保留期内重放支付接口**

```http
POST /order/api/orders/pay HTTP/1.1
X-Flow-Ticket: <t1>
X-Business-Id: ORD-1001

HTTP/1.1 403 Forbidden
Content-Type: application/problem+json

{
  "type": "https://errors.example.com/flow/completed",
  "title": "流程已完成",
  "status": 403,
  "code": "FLOW_COMPLETED",
  "traceId": "0HN8K1EXAMPLE:00000003"
}
```

---

### 示例二：参数化多组合流程 + 重叠入口 + 整段路径模板

**业务背景**：同一组订单审批接口（`A` 预审、`B` 人工复核、`D` 自动核验、`E` 终审放行），根据业务类型请求头 `X-Flow-Mode` 走三条不同路径：
- `ABE`：`A → B → E`
- `ADE`：`A → D → E`
- `DE`：`D → E`（快捷单，跳过 `A` 直接从 `D` 开始）

注意接口 `D`（`POST /api/orders/{id}/auto-verify`）**既是 `DE` 的第 1 步，又是 `ADE` 的第 2 步**，同时所有路径包含动态资源段 `{id}`。

#### 1) 网关配置（`FlowProtection:Flows` 元素）

```json
{
  "FlowId": "order-approval",
  "ParameterHeaderName": "X-Flow-Mode",
  "Interfaces": [
    { "Name": "A", "Method": "POST", "Path": "/api/orders/{id}/precheck" },
    { "Name": "B", "Method": "POST", "Path": "/api/orders/{id}/manual-review" },
    { "Name": "D", "Method": "POST", "Path": "/api/orders/{id}/auto-verify" },
    { "Name": "E", "Method": "POST", "Path": "/api/orders/{id}/approve" }
  ],
  "Variants": {
    "ABE": ["A", "B", "E"],
    "ADE": ["A", "D", "E"],
    "DE":  ["D", "E"]
  }
}
```

#### 2) 两条重叠入口组合的正常调用

**路径 1：走 `ADE` 组合（订单 `42`，由 `A` 起步，`D` 作为第 2 步）**

```http
# 1) 无票 + X-Flow-Mode: ADE 调用 A → 签发票据 <t-ade>，锁定 variant="ADE"，nextIndex 推进到 1
POST /order/api/orders/42/precheck HTTP/1.1
X-Flow-Mode: ADE
X-Business-Id: order-42

HTTP/1.1 204 No Content
Cache-Control: no-store
X-Flow-Ticket: <t-ade>
X-Flow-Next-Index: 1

# 2) 持票调用 D（可省略 X-Flow-Mode，也可带一致的 ADE）→ 匹配 ADE[1]，nextIndex 推进到 2
POST /order/api/orders/42/auto-verify HTTP/1.1
X-Flow-Ticket: <t-ade>
X-Business-Id: order-42

HTTP/1.1 204 No Content
Cache-Control: no-store
X-Flow-Ticket: <t-ade>
X-Flow-Next-Index: 2

# 3) 持票调用 E → 匹配 ADE[2]（末步），流程完成
POST /order/api/orders/42/approve HTTP/1.1
X-Flow-Ticket: <t-ade>
X-Business-Id: order-42

HTTP/1.1 204 No Content
Cache-Control: no-store
X-Flow-Completed: true
```

**路径 2：走 `DE` 组合（订单 `43`，直接无票调用 `D` 作为首步）**

```http
# 1) 无票 + X-Flow-Mode: DE 调用 D → 网关依据“无票 + DE 首步为 D”签发 <t-de>，锁定 variant="DE"
POST /order/api/orders/43/auto-verify HTTP/1.1
X-Flow-Mode: DE
X-Business-Id: order-43

HTTP/1.1 204 No Content
Cache-Control: no-store
X-Flow-Ticket: <t-de>
X-Flow-Next-Index: 1

# 2) 持票调用 E → 匹配 DE[1]（末步），流程完成
POST /order/api/orders/43/approve HTTP/1.1
X-Flow-Mode: DE
X-Flow-Ticket: <t-de>
X-Business-Id: order-43

HTTP/1.1 204 No Content
Cache-Control: no-store
X-Flow-Completed: true
```

#### 3) 五种针对参数化多组合的篡改与防跳拦截

| 企图/异常行为 | 请求示例（省略 Host） | 网关拦截结果（不转发下游） |
|---|---|---|
| **无票不带参数或传非法值**（缺失、空白、同名多值、`XYZ`） | `POST /order/api/orders/42/precheck`<br>`X-Flow-Mode: XYZ`（或省略该头） | `403` `FLOW_SELECTOR_INVALID`（无默认兜底组合） |
| **无票把 `ADE` 的中间步 `D` 当首步直调** | `POST /order/api/orders/42/auto-verify`<br>`X-Flow-Mode: ADE`<br>（无 `X-Flow-Ticket`） | `403` `FLOW_NOT_FOUND`（刻意按缺票处理，不泄露组合定义） |
| **中途篡改组合参数**（已锁定 `ADE`，推进 `D` 时传 `ABE` 或空白/多值） | `POST /order/api/orders/42/auto-verify`<br>`X-Flow-Ticket: <t-ade>`<br>`X-Business-Id: order-42`<br>`X-Flow-Mode: ABE` | `403` `FLOW_SELECTOR_MISMATCH` |
| **跨组合调用其他分支独有接口**（已锁定 `ADE`，持票调 `B`） | `POST /order/api/orders/42/manual-review`<br>`X-Flow-Ticket: <t-ade>`<br>`X-Business-Id: order-42` | `403` `FLOW_STEP_NOT_ALLOWED` |
| **组合内跳步**（`ADE` 刚过 `A`，`nextIndex=1`，持票直接调 `E`） | `POST /order/api/orders/42/approve`<br>`X-Flow-Ticket: <t-ade>`<br>`X-Business-Id: order-42` | `403` `FLOW_STEP_NOT_ALLOWED` |

> **安全提醒**：上述 `DE` 允许无票从 `D` 开始，因此**下游 `D` 接口本身必须校验订单 `43` 是否真正具备走快捷通道的业务资格**；若普通订单伪造 `X-Flow-Mode: DE` 调用 `D`，下游校验不通过并返回 `403/400` 时，网关会自动删除本次首步票据（见示例三 Case A）。

---

### 示例三：下游失败回滚与重试（首步删票 vs 中间步释锁）

**业务背景**：承接示例一（`create → risk-check → pay`），展示业务失败时网关如何既不留僵尸票，又不阻断合法重试。

#### Case A：首步下游返回非 2xx（如库存不足 `400 Bad Request`）→ 自动删票

```http
# 1) 客户端调用首步
POST /order/api/orders/create HTTP/1.1
Content-Type: application/json

{ "skuId": "SKU-OUT-OF-STOCK", "quantity": 100 }
```

- 网关先创建临时票据并加锁，转发给下游。
- 下游返回 `400 Bad Request`（`{ "message": "库存不足" }`）。
- `FlowResponseTransform` 检测到 `attempt.IsFirstStep && !success`，调用 `DeleteAsync` **彻底删除 Redis 中的票据**，透传下游 `400` 响应，且**不回写任何流程成功头**：

```http
# 网关 → 客户端（无 X-Flow-Ticket / X-Flow-Next-Index）
HTTP/1.1 400 Bad Request
Content-Type: application/json

{ "message": "库存不足" }
```

#### Case B：中间步下游返回非 2xx（如风控需要补填信息 `422`）→ 仅释锁，允许原票重试

假设第 1 步已成功，客户端拿到 `<t1>` 且 `nextIndex = 1`。

```http
# 1) 第 1 次尝试风控核验：下游返回 422
POST /order/api/orders/risk-check HTTP/1.1
X-Flow-Ticket: <t1>
X-Business-Id: ORD-1001
Content-Type: application/json

{ "orderId": "ORD-1001" }

# 网关执行 ReleaseAsync（status 回到 active，nextIndex 仍为 1），透传下游 422：
HTTP/1.1 422 Unprocessable Entity
Content-Type: application/json

{ "message": "请补充验证码后重试" }

# 2) 用户补填验证码后，持原票据 <t1> 立即重试第 2 步：
POST /order/api/orders/risk-check HTTP/1.1
X-Flow-Ticket: <t1>
X-Business-Id: ORD-1001
Content-Type: application/json

{ "orderId": "ORD-1001", "captcha": "9527" }

# 成功提交，nextIndex 推进至 2：
HTTP/1.1 200 OK
Cache-Control: no-store
X-Flow-Ticket: <t1>
X-Flow-Next-Index: 2

{ "passed": true }
```

---

### 示例四：高并发连点互斥 & 锁超时剥夺防“幽灵旧请求”

#### Case A：前端连点两次支付（并发竞争同一顺位）

假设当前 `flow:ticket:<t1>` 为 `status="active", nextIndex="2"`，用户在 10ms 内发出两笔完全相同的支付请求 R1、R2：

```text
t = 0ms   R1 到达网关 → ReserveScript 原子执行成功
          Redis: status="processing", attemptId="att-1", lockUntilUnixMs = now + 30000
          → R1 放行转发给下游支付服务（假设下游处理耗时 800ms）

t = 10ms  R2 到达网关 → ReserveScript 发现 status=="processing" && lockUntilUnixMs > now
          → R2 立即被拒（不转发下游）：
             HTTP/1.1 403 Forbidden
             Content-Type: application/problem+json
             { "type": "https://errors.example.com/flow/in-progress",
               "title": "流程步骤正在处理中",
               "status": 403,
               "code": "FLOW_IN_PROGRESS",
               "traceId": "..." }

t = 800ms R1 下游返回 200 OK → CommitScript 校验 attemptId == "att-1" 通过
          Redis: status="completed", nextIndex="3", PEXPIRE=60s
          → 客户端收到 200 OK + X-Flow-Completed: true
```

#### Case B：请求卡顿超过 `LockTtlSeconds` 后，旧请求迟到返回如何被拦截

假设 `LockTtlSeconds = 30`，下游接口偶发严重卡顿 40 秒：

```text
t = 0s    R1 获锁：status="processing", attemptId="att-1", lockUntilUnixMs = 30s
          → 转发下游，下游长时间阻塞

t = 32s   锁租约已过期（now > lockUntilUnixMs）
          调用方发起重试 R2 → ReserveScript 判定锁已过期，允许合法接管：
          Redis: status="processing", attemptId="att-2", lockUntilUnixMs = 62s
          → R2 转发下游

t = 40s   迟到的 R1 突然收到下游 200 OK → 进入 FlowResponseTransform.CommitAsync
          CommitScript 校验：stored.attemptId ("att-2") != R1.attemptId ("att-1")
          → 返回 FLOW_IN_PROGRESS，拒绝修改 Redis 状态！
          → FlowResponseTransform 捕获异常：置 503 + SuppressResponseBody = true 并上抛
          → YARP 2.3.0 向 R1 返回空响应体 502（绝不回写成功头，也不覆盖 R2 的锁）

t = 41s   R2 正常收到下游 200 OK → CommitScript 校验 attemptId == "att-2" 通过
          → 正常提交，流程安全完成
```

> 工程提示：在锁超时重试的场景下，R1 和 R2 可能都到达了下游服务，因此**下游写接口仍必须以 `orderId`/幂等键实现业务幂等**。

---

### 示例五：前端/调用方接入参考实现（TypeScript）

以下示范前端如何在**内存 Map** 中维护票据（避免写入 `localStorage` 降低泄露面）、自动透传请求头、并根据错误码统一处理重试与重置：

```ts
// 仅保存在内存中的票据缓存：key = `${flowId}:${businessId}`
const ticketStore = new Map<string, string>();

interface FlowCallOptions {
  flowId: string;
  businessId?: string;        // 首步若由下游延迟生成，可暂不传
  mode?: string;              // 参数化流程的前置参数（如 X-Flow-Mode）
  modeHeaderName?: string;    // 与网关 ParameterHeaderName 一致
  body?: unknown;
}

export async function callProtectedStep(url: string, opts: FlowCallOptions) {
  const key = opts.businessId ? `${opts.flowId}:${opts.businessId}` : "";
  const headers: Record<string, string> = {
    "Content-Type": "application/json",
  };

  if (opts.businessId) {
    headers["X-Business-Id"] = opts.businessId;
  }
  if (opts.mode && opts.modeHeaderName) {
    headers[opts.modeHeaderName] = opts.mode;
  }

  const cachedTicket = key ? ticketStore.get(key) : undefined;
  if (cachedTicket) {
    headers["X-Flow-Ticket"] = cachedTicket;
  }

  const res = await fetch(url, {
    method: "POST",
    headers,
    body: opts.body ? JSON.stringify(opts.body) : undefined,
  });

  // 1) 2xx 成功：根据响应头更新或清理票据
  if (res.ok) {
    const data = res.status === 204 ? null : await res.json();
    const resolvedBizId = opts.businessId ?? data?.orderId; // 首步延迟生成业务标识时从响应体获取
    const resolvedKey = resolvedBizId ? `${opts.flowId}:${resolvedBizId}` : "";

    if (res.headers.get("X-Flow-Completed") === "true") {
      if (resolvedKey) ticketStore.delete(resolvedKey);
    } else {
      const nextTicket = res.headers.get("X-Flow-Ticket");
      if (nextTicket && resolvedKey) {
        ticketStore.set(resolvedKey, nextTicket);
      }
    }
    return { ok: true as const, data, nextIndex: res.headers.get("X-Flow-Next-Index") };
  }

  // 2) 403 流程防护拦截：按稳定 code 分支处理
  if (res.status === 403 && res.headers.get("Content-Type")?.includes("application/problem+json")) {
    const problem = await res.json();
    switch (problem.code) {
      case "FLOW_IN_PROGRESS":
        // 上一次同步骤请求仍在执行，提示用户稍候，不要清除票据
        throw new Error("请求正在处理中，请勿重复提交");
      case "FLOW_NOT_FOUND":
      case "FLOW_COMPLETED":
      case "FLOW_BUSINESS_MISMATCH":
      case "FLOW_STEP_NOT_ALLOWED":
      case "FLOW_SELECTOR_MISMATCH":
        // 票据失效、已完成或上下文不一致：清除本地失效票据，引导重新核对流程
        if (key) ticketStore.delete(key);
        throw new Error(`流程校验未通过 (${problem.code})：${problem.title}`);
      default:
        throw new Error(problem.title ?? "流程校验失败");
    }
  }

  // 3) 非 2xx 下游业务错误（如 400/422）：中间步票据已由网关自动释锁，保留 cachedTicket 供修正后重试
  throw new Error(`业务请求失败，HTTP ${res.status}`);
}
```

---

## 6. 校验优先级与对外错误契约

### 6.1 `ReserveScript` 原子校验优先级

当客户端持票推进请求进入 Redis `ReserveScript` 时，按以下严格顺序检查，**先命中先返回**：

```text
1. 票据是否存在 (EXISTS == 0)           → FLOW_NOT_FOUND
2. 是否已完成 (status == 'completed')   → FLOW_COMPLETED
3. 归属流程是否一致 (flowId)             → FLOW_STEP_NOT_ALLOWED
4. 业务标识是否一致 (businessId)         → FLOW_BUSINESS_MISMATCH
5. [仅参数化] 票据是否已锁定组合          → FLOW_STEP_NOT_ALLOWED
6. [仅参数化] 携带的前置参数是否冲突      → FLOW_SELECTOR_MISMATCH
7. [仅参数化] 当前接口是否属于锁定组合    → FLOW_STEP_NOT_ALLOWED
8. 当前接口顺位是否等于 nextIndex        → FLOW_STEP_NOT_ALLOWED
9. 处理锁是否未过期 / 状态是否合法        → FLOW_IN_PROGRESS
```

> 注：参数化推进时，若前置参数头存在但为空白或同名多值，会在进入 Redis 前由中间件直接拦截并返回 `FLOW_SELECTOR_MISMATCH`；若携带的是未配置字符串值，则会传给脚本，在完成态（第 2 步）之后判定 `FLOW_SELECTOR_MISMATCH`。

### 6.2 错误响应格式与错误码速查

所有 403 拒绝响应均遵循 RFC 7807 `application/problem+json` 格式，不泄露票据本体与内部状态：

```json
{
  "type": "https://errors.example.com/flow/step-not-allowed",
  "title": "流程步骤不允许执行",
  "status": 403,
  "code": "FLOW_STEP_NOT_ALLOWED",
  "traceId": "..."
}
```

| HTTP 状态 | `code` | `type` 后缀 | 含义 |
|---|---|---|---|
| `403` | `FLOW_NOT_FOUND` | `not-found` | 票据缺失、不存在、已过期，或无票调用参数化组合的非首步 |
| `403` | `FLOW_EXPIRED` | `expired` | 预留的过期错误码（当前内置 Redis 过期直接按键不存在归入 `FLOW_NOT_FOUND`） |
| `403` | `FLOW_BUSINESS_MISMATCH` | `business-mismatch` | 推进时缺少 `X-Business-Id`，或与票据绑定业务标识不一致 |
| `403` | `FLOW_STEP_NOT_ALLOWED` | `step-not-allowed` | 跳步、乱序、跨流程、跨组合或持票重调非当前顺位接口 |
| `403` | `FLOW_IN_PROGRESS` | `in-progress` | 当前顺位已有并发请求正在处理中（锁未过期） |
| `403` | `FLOW_COMPLETED` | `completed` | 流程已完成（在 `CompletedTtlSeconds` 保留期内重放） |
| `403` | `FLOW_SELECTOR_INVALID` | `selector-invalid` | 参数化流程无票开启时，前置参数缺失、空白、同名多值或未命中已配置组合 |
| `403` | `FLOW_SELECTOR_MISMATCH` | `selector-mismatch` | 持票推进时携带的前置参数非法或与票据已锁定组合不一致 |
| `500` | `FLOW_CONFIGURATION_ERROR` | `configuration-error` | 代理路由缺少唯一有效的 `PathRemovePrefix` 或路径前缀不匹配 |
| `503` / `502` | —（抑制响应体） | — | 响应结算失败（Redis 异常、锁已被超时剥夺或下游回传业务标识冲突） |

---

## 7. 设计边界、局限与使用注意

团队在落地网关防跳时，务必清楚以下**不在防护范围内**的场景，并在宿主或下游补齐防线：

1. **不防首步滥用**：首步本身是流程的合法入口，任何能通过认证/授权的请求都可开启新流程。首步的防刷、防重与风控必须由认证授权、独立限流器及下游业务幂等承担。
2. **票据不绑定用户或租户身份**：票据属于持有者凭据（Bearer Ticket），`FlowReserveRequest` 模型不包含 `UserId`/`TenantId`。若票据被泄露给其他用户，只要 `X-Business-Id` 一致即可被另一登录态使用；因此下游接口仍必须做用户/租户级资源归属校验。
3. **路径模板参数不参与业务授权**：例如 `/orders/{id}/confirm` 中的 `{id}` 仅用于匹配步骤归属，网关不会把 `{id}` 提取为 `BusinessId` 或前置参数，业务标识仍以 `X-Business-Id` 请求头为准。
4. **前置参数仅支持请求头**：为保持网关层零请求体缓冲与 O(1) 判定，前置参数仅从配置的请求头读取，不支持从 Query String 或 JSON Body 提取。
5. **配置不支持运行期热更新**：`FlowDefinitionRegistry` 在启动期一次性编译只读索引，修改流程定义或开关需重启网关实例；建议宿主在 `app = builder.Build()` 后显式解析一次 `FlowDefinitionRegistry`，将配置错误提前暴露在启动阶段。
6. **必须挂在 `MapReverseProxy` 管道内**：`FlowTicketMiddleware` 强依赖 `IReverseProxyFeature.Route`，不能作为全局中间件挂载；若忘记在 `MapReverseProxy(p => p.UseMiddleware<FlowTicketMiddleware>())` 中挂载，整套防护不会生效。
7. **空的 `Flows` 列表不会报错**：启用防护但 `Flows` 为空时，所有接口都会被判定为“非受保护接口”直接放行，上线前需核对配置绑定路径。
8. **兜底清理失败日志属于敏感日志**：`FlowTicketMiddleware.FinalizeUncommittedAttemptAsync` 在清理失败记录警告日志时包含原始 `attempt.Ticket`，运维侧应将该类日志按敏感凭据管控（或在后续版本改为输出 `FlowTicketService.GetTicketFingerprint` 指纹）。
9. **无流量治理能力**：`GirvsGatewayConfig.ActivityTimeout`（默认 5 分钟）仅限制转发阶段相邻 I/O 空闲超时，不是端到端请求总时长上限，也不提供限流、熔断或自动重试。

---

## 8. 相关文档与验证入口

- **配置手册与接入示例**：[`Girvs.Gateway/README.md`](./README.md#流程防护flowprotection)
- **架构决策记录（ADR）**：
  - [`docs/adr/0001-flow-protection-parameterized-variants.md`](../docs/adr/0001-flow-protection-parameterized-variants.md)（前置参数 + 单流程多组合模型）
  - [`docs/adr/0002-flow-protection-path-templates.md`](../docs/adr/0002-flow-protection-path-templates.md)（整段路径模板与冲突检测规则）
- **自动化测试套件**：[`tests/Girvs.Gateway.Tests/`](../tests/Girvs.Gateway.Tests/)
  - `FlowProtectionPipelineTests.cs`：端到端中间件 + 响应转换全链路测试
  - `RedisFlowStateStoreTests.cs`：基于真实 Redis（Testcontainers）的 Lua 原子并发、锁接管、状态机测试
  - `FlowDefinitionRegistry*Tests.cs` / `FlowPathTemplateTests.cs`：启动期校验、模板冲突与反查测试
  - `FlowTicketServiceTests.cs` / `FlowResponseTransformTests.cs` / `DownstreamPathResolverTests.cs` / `FlowProtectionRegistrationTests.cs`

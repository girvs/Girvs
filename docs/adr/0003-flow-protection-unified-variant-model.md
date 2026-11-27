---
status: accepted
---

# 流程防护移除独立线性模型，统一采用组合模型并支持单步组合

ADR-0001 为兼容存量配置在 `FlowProtection` 中并存了纯线性 `Steps` 与参数化 `Interfaces + Variants` 两套模型，导致配置结构、公开 API、中间件分支与 Redis Lua 脚本均需维护双路径，且“持票调用首步”在两种模式下语义不一致。我们决定彻底移除线性模型及兼容层，所有 `Flow` 统一采用 `Interfaces + Variants` 配置；当 `Variants` 仅含 1 条组合时允许省略 `ParameterHeaderName`（显式配置时仍严格校验），同时将每条组合的步骤下限从 2 放宽至 1，以满足部分参数组合仅需调用单个接口的业务场景。

## Considered Options

- **保留线性 `Steps` 或加 `[Obsolete]` 过渡层**：会继续维持双配置形态、双反查结构与双 Lua 路径，无法消除维护成本与“持票调首步”的语义分裂；当前库内可接受破坏性变更，直接一步清理最干净。
- **所有流程（含单组合）一律强制配置 `ParameterHeaderName`**：虽然运行期零特判，但固定顺序单路径业务会被迫在首步多传一个无意义的固定请求头；允许单组合省略 `ParameterHeaderName` 既保持统一配置/存储模型，又不增加单路径调用方负担。
- **单步组合仍走完整 Redis 建票与提交**：单步组合在首步成功即完成，网关只回写 `X-Flow-Completed: true` 而不回传票据，写入 Redis 只会产生两次无意义的 Lua 往返与客户端永远不可见的已完成僵尸键；改为无票命中单步时跳过 Redis，仅保留前置参数校验、成功响应头与内部头剥离。
- **仅允许在多组合中出现单步组合，单组合仍强制 ≥ 2 步**：会引入额外的组合数与步数耦合校验，不如统一“每条组合 ≥ 1 步”直观，且统一规则便于业务从单步平滑演进为多步或多组合。

## Consequences

- **破坏性清理**：移除 `FlowDefinitionOptions.Steps`、`FlowStepOptions`、`FlowDefinitionKind`、`FlowDefinition.Steps`、`FlowEndpointMatch.LinearMatch`、`FlowDefinitionRegistry.TryGetStep`、`FlowTicketService` 线性重载，以及 `IFlowStateStore` / `RedisFlowStateStore` 中 `candidateCount == 0` 和空 `variant` 的线性分支。
- **开启/推进唯一分界**：全量流程统一以请求是否携带票据头（默认 `X-Flow-Ticket`）作为开启新流程与推进既有流程的唯一判定；持票调用任何受保护接口（含首步）一律走既有票据推进校验，不再允许持票重开新票。
- **单组合免前置参数规则**：配置结构保持单一的 `Interfaces` + `Variants`（组合键非空唯一，写入 Redis `variant` 字段）；仅当 `Variants.Count == 1` 且 `ParameterHeaderName` 留空时免校验前置参数，显式填写了 `ParameterHeaderName` 或 `Variants.Count >= 2` 时按严格前置参数规则校验。
- **单步组合行为契约**：每条组合至少 1 步；无票命中单步组合时，校验通过后直接转发下游而不写 Redis，下游返回 `2xx` 时仍写入 `X-Flow-Completed: true` 与 `Cache-Control: no-store`，`finally` 照常剥离下游 `X-Flow-Business-Id`；非 `2xx` 时透传响应且不写完成头。持票调用该接口时仍走既有票据校验路径。
- **升级注意**：网关升级窗口内若 Redis 存在旧版线性流程生成的空 `variant` 进行中票据，推进时会因缺少锁定组合被拒，需在低峰期或等待旧票 TTL 自然过期后切换。

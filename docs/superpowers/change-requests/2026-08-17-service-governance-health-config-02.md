---
change_request_status: accepted
plan_id: 2026-08-17-service-governance-health-config
created_at: 2026-08-17T12:15:00+08:00
accepted_at: 2026-08-17T12:16:00+08:00
---

# 服务治理健康配置变更申请 02

## 触发原因

独立审查确认 approved Plan 的全仓 `HealthAddress` 零残留验收与 Spec 中“不修改 `Girvs.Consul`”的边界冲突；同时需明确没有可用 HTTP/HTTPS endpoint 时的 Aspire 行为及健康路径格式约束。

## 原条款

- 全仓代码与配置不再使用 `HealthAddress`。
- Aspire 项目声明 Readiness 与 Liveness probe。

## 证据

- `Girvs.Consul` 仍使用独立的 `HealthAddress` 配置模型，且 Spec 明确排除该模块。
- 自动补充 `http` endpoint 会改变已有服务端口与发布拓扑。
- 未限制 `/` 前缀的健康路径会生成无效 Consul URL 或 probe 路径。

## 影响与建议选项

已接受的范围变更：

1. `HealthAddress` 零残留验收仅适用于 `Girvs.ServiceGovernance`。
2. 无可用 HTTP/HTTPS endpoint 时，Aspire Hosting 应抛出明确配置错误，不自动新增端口。
3. `HealthCheckPath` 与 `LivenessCheckPath` 必须非空且以 `/` 开头。

必须重新规划：是。

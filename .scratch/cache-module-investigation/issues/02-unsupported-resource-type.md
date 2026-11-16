Status: needs-triage
Blocked by: 无

# P2：拒绝缓存不支持的资源类型

详见 [诊断报告](../report.md) 第 2 项。

验收：引用有效但 Type 为空、拼错或不支持时，注册阶段抛出明确的 GirvsException，不留下缺失依赖的缓存服务注册。

## Comments

2026-11-16：sqlserver、redsi、空类型均已复现；未恢复 SQL Server 功能。

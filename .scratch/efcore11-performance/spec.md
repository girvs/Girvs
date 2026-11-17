# EF Core 11 性能评估

日期：2026-11-17。

## 目标与范围

检查 `Girvs.EntityFrameworkCore/Girvs.EntityFrameworkCore.csproj` 对应模块及直接影响其查询的 Girvs 公共扩展，结合 Microsoft 官方 EF Core 11 与通用性能指南，找出具体可实施的性能改进。

首轮交付评估报告和可复现探针。用户随后授权“按建议进行优化”，本轮进一步实施报告中的明确优化；不修改数据库或包版本，不拆分现有组件。保留 net8/net9/net10/net11 多目标与旧调用入口，新增行为及事件差异见 `docs/efcore-performance.md`。

## 产出

- `official-research.md`：官方资料、版本边界及适用限制。
- `report.md`：源码位置、优先级、复现证据、建议与验证边界。
- `probe/`：EF 11 + 临时内存 SQLite 的行为探针；不访问 SQL Server/MySQL。
- `issues/01-performance-findings.md`：后续优化候选。

## 完成情况

已完成源码检查、官方资料核对、既有测试及行为探针。实施内容为动态值参数化、先排序/分页再投影、字段级更新、可选总数/无跟踪分页、游标和流式入口、Clear 清理及动态 setter 元数据缓存。验收结果见 validation.md。收益量化仍需实际 SQL Server/MySQL、业务查询与生产相近数据，不将编译次数差异直接换算为吞吐提升。

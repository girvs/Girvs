# .NET 11 升级准备记录

更新日期：2026-11-13

## 已确认的决定

- 任务跟踪使用本地 Markdown，遵循 `docs/agents/issue-tracker.md`。
- 新增 `net11.0` 支持，保留各模块现有目标框架，不统一替换为单一 `net11.0`。
- 默认多目标库由 `net8.0;net9.0;net10.0` 扩展为 `net8.0;net9.0;net10.0;net11.0`。
- MySQL 提供程序采用 `Microting.EntityFrameworkCore.MySql` 作为新目标的适配方向；旧目标的依赖保留。
- 新增的 `net11.0` 分支同步采用 EF Core 11，接受 Microting 11 预发布版本作为适配候选；具体版本组合须通过还原、构建和数据库行为验证。
- 升级范围包括库、测试、主示例应用、网关和 AppHost，保留现有目标支持；用户在任务拆分阶段明确排除容器示例。
- 用户随后强调全部库必须添加 .NET 11 支持；实际数据库验收独立跟踪，不再阻挡库适配和回归。

## 已核对的仓库现状与实施进度

- `Directory.Build.props` 已追加 `net11.0`，默认支持 .NET 8、9、10、11。
- 全部 17 个库完成 .NET 11 适配并保留原目标；Gateway、ServiceGovernance 为 net10/net11，OpenApi 为 net9/net10/net11，其余 14 个库为 net8/net9/net10/net11。
- EF Core 模块按目标框架分别声明依赖；`net8.0/net9.0` 使用 Pomelo，`net10.0` 使用 Microting 10.0.9；新增目标已接入 EF Core 11.0.0、Microting 11.0.0-preview.1 和 MySqlConnector 2.6.2。
- 仓库 SDK 已切换至本机安装的 11.0.100，原 SDK 配置的无效特征带已修正。
- 全部库测试覆盖 `net10.0;net11.0`，新增 EF Core 和 OpenAPI 兼容性项目；主示例网关测试仍待示例任务追加新目标。
- `samples/Directory.Build.props` 以及各示例项目显式使用 `net10.0`；AppHost 使用 Aspire.AppHost.Sdk 13.4.6。
- `samples/gateway-k8s/Gateway/Gateway.csproj` 不在当前解决方案中，容器示例使用 `aspnet:10.0`，本次不升级；共享示例配置变更需避免隐式改变其目标。

## 验收边界

- 用户已确认三层验收边界；根据后续范围调整，最终为各目标构建和测试回归、实际数据库行为、示例编排。
- 正式规格见 `spec.md`，五项正式任务见 `issues/`；容器任务已移除，最终验收由 06 重编号为 05。

## 依赖核验记录

- Microting 11 候选版本 `11.0.0-preview.1` 已与 EF Core 11.0.0 完成构建及离线提供程序兼容性检查；尚未完成实际数据库验收。
- 包详情：https://www.nuget.org/packages/Microting.EntityFrameworkCore.MySql/11.0.0-preview.1
- .NET 目标框架升级与 EF Core 大版本升级分别决策；不能将 Microting 10 的引用直接视为 EF Core 11 适配完成。

## 当前阶段

升级范围、依赖策略和验收边界均已确认，已形成正式 `spec.md`。
任务 01 已完成并提交为 `4b4db86`，基础引用链、双版本核心测试和包消费结果见 `validation-01.md`。
任务 02 已完成依赖及快照接口适配，实际数据库验收仍未完成，详见 `validation-02.md`。
任务 03 已完成全部库适配、包消费检查与双框架回归，结果见 `validation-03.md`。任务 04 主示例/AppHost、任务 05 最终整体运行验收尚未完成。
全仓回归共 425 项，409 通过、16 项为既有 8 个 Redis 集成用例在双框架下的 Docker 环境失败；未将这些失败或待执行的数据库验收标为通过。

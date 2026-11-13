# .NET 11 升级准备记录

更新日期：2026-11-13

## 已确认的决定

- 任务跟踪使用本地 Markdown，遵循 `docs/agents/issue-tracker.md`。
- 新增 `net11.0` 支持，保留各模块现有目标框架，不统一替换为单一 `net11.0`。
- 默认多目标库拟由 `net8.0;net9.0;net10.0` 扩展为 `net8.0;net9.0;net10.0;net11.0`。
- MySQL 提供程序采用 `Microting.EntityFrameworkCore.MySql` 作为新目标的适配方向；旧目标的依赖保留。
- 新增的 `net11.0` 分支同步采用 EF Core 11，接受 Microting 11 预发布版本作为适配候选；具体版本组合须通过还原、构建和数据库行为验证。
- 升级范围包括库、测试、主示例应用、网关和 AppHost，保留现有目标支持；用户在任务拆分阶段明确排除容器示例。

## 已核对的仓库现状与实施进度

- `Directory.Build.props` 已追加 `net11.0`，默认支持 .NET 8、9、10、11。
- Girvs 与 DynamicWebApi 基础引用链已完成四目标适配；其他模块由后续任务继续适配。
- EF Core 模块按目标框架分别声明依赖；`net8.0/net9.0` 使用 Pomelo，`net10.0` 使用 Microting 10.0.9。
- 仓库 SDK 已切换至本机安装的 11.0.100，原 SDK 配置的无效特征带已修正。
- 解决方案包含 7 个测试项目；核心测试已覆盖 `net10.0;net11.0`，其余项目后续逐项适配。
- `samples/Directory.Build.props` 以及各示例项目显式使用 `net10.0`；AppHost 使用 Aspire.AppHost.Sdk 13.4.6。
- `samples/gateway-k8s/Gateway/Gateway.csproj` 不在当前解决方案中，容器示例使用 `aspnet:10.0`，本次不升级；共享示例配置变更需避免隐式改变其目标。

## 验收边界

- 用户已确认三层验收边界；根据后续范围调整，最终为各目标构建和测试回归、实际数据库行为、示例编排。
- 正式规格见 `spec.md`，五项正式任务见 `issues/`；容器任务已移除，最终验收由 06 重编号为 05。

## 依赖核验记录

- 此前核验的 Microting 11 候选版本为 `11.0.0-preview.1`，尚未完成与最终选定 EF Core 版本的构建及数据库验证。
- 包详情：https://www.nuget.org/packages/Microting.EntityFrameworkCore.MySql/11.0.0-preview.1
- .NET 目标框架升级与 EF Core 大版本升级分别决策；不能将 Microting 10 的引用直接视为 EF Core 11 适配完成。

## 当前阶段

升级范围、依赖策略和验收边界均已确认，已形成正式 `spec.md`。
任务 01 已完成基础引用链、双版本核心测试和包消费验证，结果见 `validation-01.md`。下一项为 02：EF Core 11 与数据库提供程序适配。
全仓升级尚未完成；基线中原有的 8 项 Redis 集成测试因 Docker 不可用而失败，不能将本阶段验证等同于最终验收。

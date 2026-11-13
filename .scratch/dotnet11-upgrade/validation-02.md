# 任务 02 阶段验证记录

日期：2026-11-13。基线提交：`4b4db86`。任务仍为 in-progress，未完成实际数据库验收。

## 候选依赖组合

仅新增 net11.0 分支：Microsoft.EntityFrameworkCore、Proxies、Relational、SqlServer、Tools 和 Design 统一为 11.0.0；MySQL 提供程序使用 Microting.EntityFrameworkCore.MySql 11.0.0-preview.1；显式选择 MySqlConnector 2.6.2，避免新目标默认引入已报告审计告警的 2.5.0。原 net8.0、net9.0、net10.0 分支不改。

已检查实际下载包的元数据及解析资产：Microting 11 对 EF Core Relational 的范围为 `[11.0.0-preview.7.26381.103, 11.0.999]`；net11.0 实际统一解析到 EF Core 11.0.0。不能仅凭这一范围证明实际数据库行为兼容，故保留实际数据库验收要求。

## 适配与回归

- 首次编译发现原四参数 GenerateSnapshot 无法覆盖 EF Core 11 接口，报 CS0115。
- 本地 EF Core 11 Design XML 明确第五参数为 latestMigrationId；新增目标继承基类快照生成实现，保留 LastMigrationId 元数据，旧目标保留原四参数实现。
- 新增 xUnit 提供程序测试，从 GirvsDbContext 公共入口使用真实 MySQL 和 SQL Server 提供程序。SQL 与创建脚本生成不连接数据库；不把它称为数据库读写测试。
- 查询用例检查过滤 SQL 和标识符引用；建表脚本用例覆盖含 `~` 的索引名；EF Core 11 快照用例检查最新迁移 ID 被写入快照。

## 已执行检查

| 检查 | 结果 |
| --- | --- |
| EF Core 基础引用链还原 | 成功；NuGet TLS 沙箱错误通过获准的沙箱外还原解决 |
| EF Core 模块四目标干净构建 | 成功，62 个警告、0 个错误 |
| 提供程序测试 net10.0 | 4 项通过、0 失败 |
| 提供程序测试 net11.0 | 6 项通过、0 失败 |

现存警告包含旧目标依赖审计、既有 EF 内部 API、泛型参数重名及过时 API。本阶段未改旧目标依赖，不宣称已消除全仓警告。日志与 TRX 留存于本地已忽略的 artifacts 目录。

```powershell
dotnet restore tests/Girvs.EntityFrameworkCore.Tests/Girvs.EntityFrameworkCore.Tests.csproj
dotnet build Girvs.EntityFrameworkCore/Girvs.EntityFrameworkCore.csproj --no-restore --no-incremental
dotnet test tests/Girvs.EntityFrameworkCore.Tests/Girvs.EntityFrameworkCore.Tests.csproj --no-restore
```

## 未完成验收

尚无可销毁的临时 MySQL 与 SQL Server 环境。当前主机找不到 docker、mysqld、sqlservr、SqlLocalDB 命令，也未发现对应服务或已配置的 Girvs 数据库测试环境变量。

实际数据库的读写、异步保存、延迟加载、仓储、分表、迁移历史表、无迁移及空查询结果等边界尚未执行。已向用户询问测试环境，仅请求环境变量名或本地配置路径，不请求在对话中粘贴密码。

任务 02 的实际数据库验收保持未完成。用户随后明确全部库必须继续增加 .NET 11 支持，任务 03 仅依赖本任务已完成的编译与依赖适配部分，进度见 `validation-03.md`；数据库环境不再阻挡其余库。容器示例适配仍排除在范围之外。

## Standards

独立审查未发现需修复的问题。新依赖仅用于新增目标，条件编译范围明确，测试使用公共入口、中文行为命名和文件作用域命名空间，未将离线测试冒充数据库读写验收。硬性违规及需处理的启发式问题均为 0。

## Spec

独立审查未发现已实现部分的新偏差或越界。实际配置、仓储、数据库读写、迁移、延迟加载和分表仍为明确待验收事项，任务保持 in-progress；不能宣称全量规格已完成。

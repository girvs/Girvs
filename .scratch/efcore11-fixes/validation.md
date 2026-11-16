# EF Core 修复验证记录

日期：2026-11-16。最终范围为第 3、4、5 项；第 6 项拆分已按用户要求撤回，框架保持单一组件。

## 实施结果

- 第 3 项：批量更新依据 EF 模型为每个字段构造 `EF.Property<T>` 选择器及匹配类型的值；EF8/9 使用表达式链，EF10/11 使用普通 setter 委托。完整校验后才切库和执行。拒绝重复/未映射字段、主键、租户字段、保存后不可写字段和错误类型/空值。
- 第 4 项：批量更新、删除入口选择主库，保留原有查询过滤和立即执行语义；`Commit` 再次确保主库，仅保存跟踪变更。新增 `BeginWriteTransactionAsync`，事务由调用方提交/回滚。已打开连接或已有事务时拒绝跨库切换，同主库事务可继续使用。
- 第 5 项：按 DbContext 类型、Provider、主库连接 SHA-256 指纹和后缀隔离缓存，每键独立锁定；仅成功后标记完成，失败保留原始异常且允许重试。检查当前上下文的分表实体，避免全局类型发现误判。
- 第 6 项已撤回：三个迁移文件恢复到原 Migrations 目录；原 csproj、GlobalUsings、EngineContextExtensions、解决方案及既有 ProviderCompatibilityTests 恢复原内容。独立设计时项目、注册入口、说明及项目引用已删除。消费端保持单组件接入。
- 仓储及工作单元公共方法签名保持不变，接口仅补充注释。分表运行期迁移程序集及模型缓存工厂保留。

## 构建与测试

| 检查 | 结果 |
|---|---|
| 原单组件 net8/net9/net10/net11 构建 | 撤回后四目标测试构建通过 |
| EF8/net8 目标测试 | 39 通过、0 失败 |
| EF9/net9 目标测试 | 39 通过、0 失败 |
| EF10/net10 目标测试 | 39 通过、0 失败 |
| EF11/net11 目标测试 | 41 通过、0 失败 |
| 生成器组件归属及代码生成 | 撤回后通过；生成器与 GirvsMigration 位于同一原程序集，迁移和快照生成后可编译，EF11 保留 LastMigrationId |
| Sample.ServiceA 下游构建 | 通过，27 个既有警告、0 错误 |
| git diff --check | 通过 |

本机只有 .NET 10、11 运行时。net8/net9 目标使用 `DOTNET_ROLL_FORWARD=Major` 在 .NET 10 运行时执行，验证了 EF8/9 依赖与旧 setter 分支；不将其宣称为原生 .NET 8/9 运行时验收。net10/net11 使用对应运行时。总计 158 项通过、0 跳过。

测试平台报告非阻断提示：`VSTEST_ARTIFACTS_DIRECTORY environment variable was not found`。撤回后 TRX 已写入 `artifacts/single-component-results/`，原拆分阶段的产物不作为最终交付依据。

```powershell
dotnet restore tests/Girvs.EntityFrameworkCore.Tests/Girvs.EntityFrameworkCore.Tests.csproj
dotnet build Girvs.EntityFrameworkCore/Girvs.EntityFrameworkCore.csproj --no-restore
$env:DOTNET_ROLL_FORWARD = 'Major'
dotnet test tests/Girvs.EntityFrameworkCore.Tests/Girvs.EntityFrameworkCore.Tests.csproj --no-restore --logger 'trx;LogFilePrefix=single-component' --results-directory .scratch/efcore11-fixes/artifacts/single-component-results
dotnet build samples/Sample.ServiceA/Sample.ServiceA.csproj --no-restore
git diff --check
```

依赖还原首次遇到沙箱 TLS 认证错误，已通过获准的沙箱外还原解决。

此前拆分阶段的临时包及消费项目仅留在已忽略的 artifacts 中，现已失效，不再代表最终组件结构；未发布远程包。本轮恢复原包边界及依赖，不要求消费端引用新包。

## 验证层次与边界

- 临时 SQLite 是实际关系数据库执行：双库数据隔离、动态字段、空值、租户过滤、显式事务提交/回滚、无跟踪变更的 Commit 返回值。
- MySQL/SQL Server 动态更新使用真实 Provider 翻译 SQL，拦截并禁止连接打开，在命令执行前捕获 SQL；不算真实数据库写入验收。
- 分表协调测试替换 `IMigrator`，验证多上下文、多主库、同键并发、不同键并发和失败重试；不执行真实服务器迁移。
- 迁移生成器测试直接使用原组件中的生成器，生成迁移、元数据及快照并用 Roslyn 编译；不新增生产注册入口。
- 未获得实际 MySQL/SQL Server 环境，真实服务器迁移、跨实例锁及延迟加载验收仍不在本轮已通过范围内。

## 消费端调整

单组件架构及原有依赖恢复，设计时功能仍在 Girvs.EntityFrameworkCore 中，现有消费项目无需新增包或采用新的注册方式。

批量更新仍不自动刷新已跟踪实体、不自动触发 SaveChanges 拦截器或审计填充。迁移缓存只覆盖当前进程；多实例需部署锁或 Provider 迁移锁。数据库被外部重建后须清理进程完成状态（重启）。

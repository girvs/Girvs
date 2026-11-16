# EF Core 11 官方资料核验

核验日期：2026-11-16。范围为官方行为变化、Provider 元数据及其与当前 Girvs 实现的关系；未修改源码，未执行实际数据库验收。

## 资料时效与结论边界

Microsoft Learn 的 EF11 新特性页面目前仍将 EF11 描述为开发中；仓库已使用 `11.0.0`，两者存在状态描述差异。因此以下以当前可读取的行为文档与实际包元数据为依据，不根据环境日期推断所有第三方 Provider 已稳定，也不建议把仓库已完成的升级回退为预览 SDK。[官方 EF11 新特性](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-11.0/whatsnew)

## Provider 版本：当前组合在声明范围内，仍需数据库验证

NuGet 官方 feed 的 `Microting.EntityFrameworkCore.MySql/11.0.0-preview.1` 元数据声明：目标为 `net11.0`，依赖 `Microsoft.EntityFrameworkCore.Relational` 范围为 `[11.0.0-preview.7.26381.103, 11.0.999]`，`MySqlConnector` 最低版本为 `2.5.0`。故仓库的 EF Core `11.0.0` 与 Connector `2.6.2` 满足所声明范围，不能仅凭 `preview.1` 判断存在包版本冲突。[NuGet nuspec](https://api.nuget.org/v3-flatcontainer/microting.entityframeworkcore.mysql/11.0.0-preview.1/microting.entityframeworkcore.mysql.nuspec)

当前 feed 的 11 系列仅列出 `11.0.0-preview.1`，未发现 11 正式版。Provider 仓库为 Microting 维护的 Pomelo 派生项目。声明范围兼容不能证明运行期 SQL、迁移及代理行为兼容；应保留旧框架 Provider，补齐实际 MySQL、SQL Server 验收。[NuGet 版本索引](https://api.nuget.org/v3-flatcontainer/microting.entityframeworkcore.mysql/index.json)、[Provider 仓库](https://github.com/microting/Pomelo.EntityFrameworkCore.MySql)

EF 官方提醒 Provider 通常不能跨 EF 主版本工作，且第三方 Provider 的质量、维护与详细兼容性需单独评估。[官方 Provider 指南](https://learn.microsoft.com/en-us/ef/core/providers/)

## 对当前模块直接相关的 EF11 行为变化

| 官方变化 | 对 Girvs 的影响与建议 |
|---|---|
| `Migrate/MigrateAsync` 在迁移程序集没有迁移时默认抛异常 | `DbContextExtension.ShardingAutoMigration` 会调用同步 `Migrate`，需要区分“没有迁移类型”与“全部迁移已经应用”。按业务选择跳过、明确失败或仅对此情形配置日志，避免一律吞异常。 |
| SQL Server 默认兼容级别由 150 改为 160 | `UseSqlServerWithLazyLoading` 未显式指定兼容级别，旧 SQL Server 或低兼容级别数据库可能收到无法执行的 SQL；应提供可配置兼容级别，并验证分页、`Math.Min/Max` 等代表查询。 |
| SqlClient 升至 7，Azure/Entra 认证依赖拆包 | 使用相关认证模式的消费端需显式添加 `Microsoft.Data.SqlClient.Extensions.Azure`；普通 SQL 用户密码场景无需为此无条件增加包。 |
| `Tools/Tasks` 不再传递依赖 `Design` | 项目目前显式引用了 `Design`，该项已有基础；整理依赖时不能认为保留 `Tools` 即足以使用设计时 API。 |

以上四项及解决方式来源：[EF11 官方破坏性变更](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-11.0/breaking-changes)。

## 迁移生成器：已适配部分应保留

EF11 快照记录最新迁移 ID，以暴露分支迁移分歧。仓库 `GirvsCSharpMigrationsGenerator` 已在 `NET11_0_OR_GREATER` 下沿用基类快照生成器；`ProviderCompatibilityTests` 已断言 `LastMigrationId`。这属于已有适配，应保留，后续补实际 scaffolding、生成代码编译与应用迁移链测试，不能列为尚未修复的问题。[官方最新迁移 ID 说明](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-11.0/whatsnew#latest-migration-id-recorded-in-model-snapshot)

## 不是 EF11 新增、但值得利用的能力

- `ExecuteUpdate` 接受普通语句 lambda、可在其中条件添加 setter，是 EF10 已引入的 API 变化。仓库的 `NET10_0_OR_GREATER` 分支方向正确；setter 的属性表达式必须表示实体属性，`b => keyValuePair.Key` 只是外部字符串值，不是实体成员。应改为强类型属性表达式，或经模型元数据校验后构造成员/`EF.Property<T>` 表达式。[EF10 官方 ExecuteUpdate 说明](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/breaking-changes#executeupdateasync-now-accepts-a-regular-non-expression-lambda)
- 命名查询过滤器及选择性禁用从 EF10 起提供。可评估将租户条件放到 DbContext 全局过滤器，覆盖直接 DbSet 与导航查询；不能将其称为 EF11 新特性。旧目标需保留组合条件的实现，跨租户管理入口与写操作保护仍需明确。[官方全局查询过滤器](https://learn.microsoft.com/en-us/ef/core/querying/filters)
- 动态模型缓存必须包含决定模型形状的变量及设计时标志，这是既有契约。当前 `GirvsTenantModelCacheKeyFactory` 已包含上下文类型、分表后缀及 `designTime`。后续应核验所有映射变化都被键覆盖，不应无条件将请求级租户 ID 加入模型键，造成每租户一份相同模型。[官方动态模型缓存](https://learn.microsoft.com/en-us/ef/core/modeling/dynamic-model)

## 设计时依赖的整理建议

后续决定：用户明确要求组件保持一体，第 6 项拆分建议不采纳；实现已恢复为单一 Girvs.EntityFrameworkCore 组件。以下段落仅保留最初评估背景。

EF 官方将设计时服务与运行期服务分离，`Design` 是开发依赖。项目自定义迁移代码生成器直接使用其类型，因此不能机械删除引用或去掉编译资产。合理方向是抽出单独设计时模块，再给 `Design/Tools` 明确私有资产策略，并验证发布后的包消费。[官方设计时服务](https://learn.microsoft.com/en-us/ef/core/cli/services)

## 可选 EF11 特性，按业务需求引入

复杂类型映射与 JSON、嵌套属性索引、查询翻译改进可按具体业务评估。SQL Server JSON/向量能力受数据库版本、兼容级别及 Provider 支持约束，适合独立扩展，不宜为通用仓储强制增加。无需为当前未引用的 Cosmos Provider 批量适配 Cosmos 专属变化。[官方 EF11 新特性](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-11.0/whatsnew)

## 本地对应改进

以下由主代理对当前源码检查汇总，属于 Girvs 实现改进；其中既有缺陷不能归因为 EF11 新增行为。

| 优先级 | 位置 | 改进事项 |
|---|---|---|
| 高 | `DbContextOptionsBuilderExtensions.cs:19` | 提供 SQL Server 兼容级别配置，适应 EF11 默认 160 的变化。 |
| 高 | `GirvsEntityFrameworkCoreModule.cs:53`、`:63`；`Repository.cs` 构造函数 | 默认启用自动迁移，启动迁移失败只记录日志，仓储构造又触发分表迁移。宜明确迁移策略及失败处理，将迁移从请求路径移出，并覆盖 EF11 空迁移集合。 |
| 高 | `DbContextExtension.cs:36` | 分表迁移完成缓存仅以 suffix 去重，多 DbContext/数据库使用同后缀时可能误跳过。键应纳入上下文、目标数据库及后缀；全局锁仅保护当前进程，不能代表多实例协调。 |
| 高 | `Repository.cs:99`、`:103` | `SetProperty` 的选择器是外部字符串值，未引用实体属性；应修复动态 setter 并验证真实 Provider SQL 翻译与执行。 |
| 高 | `Repository.cs:146`；`AddGirvsDbContext`；`UnitOfWork` 构造函数 | `ExecuteDelete` 与批量更新立即访问数据库；初始上下文连接为读库，切换写库发生在 UoW 构造，可能先在读库执行批量写。必须在批量写入口明确路由到主库，并约定事务边界。 |
| 高 | `MigrationService.InitMigration` | 匿名 GET 端点执行迁移，依赖硬编码校验值；应改为明确授权的管理操作、使用合适 HTTP 方法，或改为部署期入口。文档不记录校验值原文。 |
| 中 | `Repository.cs:202` | `UpdateEntity` 忽略 `fields` 参数并调用 `DbSet.Update`，有全量覆盖风险；按指定字段设置修改状态，并验证并发保护。 |
| 中 | `.csproj`、迁移生成器、`MigrationService` | 主库混合运行期 ORM、设计时代码生成和 DynamicWebApi 端点。宜拆分边界后隔离 Tools/Design，减少不必要消费依赖。 |
| 中 | `MySqlSyntaxInterceptor` 注册处 | 拦截器对所有 Provider 注册；宜仅用于适用的 MySQL 路径，并优先修正结构化迁移操作，避免直接改写 SQL 字符串。 |

主代理本轮运行 `net11.0` 对应测试，结果为 **13/13 通过**。既有 Provider 测试主要验证 SQL/建表脚本生成与快照，仍不能代替真实数据库读写、分表及迁移验收。`LastMigrationId` 的现有适配和测试应保留。

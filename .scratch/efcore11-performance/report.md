# Girvs.EntityFrameworkCore 性能评估

日期：2026-11-17。结论：有明确改进空间，优先处理查询形状、查询量和写入列数；仅调整 csproj 或全面开启性能开关不足以解决这些问题。

后续：用户已授权按建议实施，以下保留为优化前的评估记录。当前实现与验收结果见 [validation.md](validation.md)，使用方式见 [EF 仓储说明](../../docs/efcore-performance.md)。

本次检查了目标项目、仓储、上下文注册、分表、批量写、取消跟踪及直接参与查询的 Girvs 公共扩展。保留现有单组件边界。本轮只新增本目录文档和探针，未修改生产代码及依赖。

## 一、优先改进项

### 1. 高优先级：动态筛选和租户条件参数化

位置：`Girvs.EntityFrameworkCore/DbContextExtensions/IQueryableExtensions.cs:19`；`Girvs/BusinessBasis/Repositories/GirvsRepositoryOtherQueryCondition.cs:29`。

两处都把变化的请求值写成 `Expression.Constant`。在相同模型、相同查询结构下，更换值仍会产生不同的 EF 查询缓存项。租户条件在 `Repository.Queryable` 中广泛应用，因此不只影响显式调用 WhereEqual 的查询。

本地 EF 11 + SQLite 实际执行的结果：

| 方式 | 执行次数 | 不同值数量 | QueryCompilationStarting 次数 |
|---|---:|---:|---:|
| 现有 WhereEqual | 3 | 3 | 3 |
| 普通捕获变量的 Where | 3 | 3 | 1 |
| 现有租户条件构造器 | 3 | 3 | 3 |

WhereEqual 生成 `WHERE "r"."Name" = 'a'`，值被内联。建议保留字段/运算结构，将业务值改成 EF 能提取为参数的捕获表达式。不要把属性名、表名等结构常量统一替换为参数。缓存表达式时禁止捕获上一次请求值。

上述结果证明重复编译，尚不证明生产吞吐提升比例；数据库计划复用还依赖 Provider。分表后不同物理表对应不同模型，参数化不能使它们共用同一个模型或计划。[官方动态查询与参数化指南](https://learn.microsoft.com/en-us/ef/core/performance/advanced-performance-topics#dynamically-constructed-queries)

### 2. 高优先级：分页先排序，再投影，保证排序唯一

位置：`Girvs.EntityFrameworkCore/Repositories/Repository.cs:169`、`:170`；`Girvs/Extensions/OrderByExtension.cs:25`。

现有顺序为 `Where → SelectProperties → OrderByDescending → Skip → Take`。当 QueryFields 仅有 Name、OrderBy 为未选择的 Count 时，实际构造 `new TEntity { Name = ... }.Count`。本地 EF 11 探针抛出 `InvalidOperationException`，不是客户端自动排序。这是影响投影优化使用的功能缺陷。

建议顺序调整为 `Where → OrderBy/ThenBy → Skip/Take → Select`。排序为空时明确默认唯一键；按非唯一字段排序时增加主键作为第二排序键。当前字符串排序扩展只有单字段排序，需支持组合排序，不能只在现有方法后重复 OrderBy 而覆盖前一排序。探针对应的正确业务顺序是 Count 降序得到 `a,c,b`。

唯一排序与排序索引属于官方分页建议；本项翻译失败属于本地复现结果。[官方分页指南](https://learn.microsoft.com/en-us/ef/core/querying/pagination)

### 3. 高优先级：UpdateEntity 尊重 fields，减少全列更新

位置：`Girvs.EntityFrameworkCore/Repositories/Repository.cs:183`、`:195`。

`UpdateAsync` 和列表版 `UpdateRangeAsync` 都接收 fields，但 UpdateEntity 没有使用它，仍执行 `DbSet.Update(t)`。探针中只修改 Name 后调用 Update，Count、Name、TenantId 都被标记为修改。

建议已跟踪实体利用正常变更检测；显式 fields 场景验证字段后按属性设置 IsModified。需要维护 UpdateTime、原始并发令牌、租户字段保护及已有跟踪实例语义。不要简单 Attach 后整体清除状态，抹掉同一工作单元的其他待保存修改。减少写入列数的效果应结合执行计划、日志量及索引确认；未修改值的索引维护行为由数据库决定，不能一律声称全量 UPDATE 必然重写所有索引。

与全量覆盖风险一并修复。EF 支持按属性跟踪和更新；条件版批量更新已使用 ExecuteUpdate，不应再改回加载实体。[官方变更跟踪指南](https://learn.microsoft.com/en-us/ef/core/change-tracking/)、[官方批量写指南](https://learn.microsoft.com/en-us/ef/core/saving/execute-insert-update-delete)

### 4. 中优先级：分页可选总数，增加游标入口及结果上限

位置：`Girvs.EntityFrameworkCore/Repositories/Repository.cs:135`、`:142`、`:154`、`:171`；`Girvs/BusinessBasis/Queries/QueryBase.cs:11`。

有效非空页固定执行 Count + 页数据两条命令；深页还需要处理大量被 Skip 的记录。GetAll/GetWhere 返回所有匹配行，PageSize 也未在该层设上限。

建议保留现有计数分页契约，增加不计总数的查询入口：取 PageSize + 1 条判断 HasMore。连续翻页增加 keyset/cursor 入口；按具体排序字段与主键构造游标条件，配置匹配的业务复合索引。任意跳页需求继续使用 offset，不承诺游标完全替代页码。大批量导出提供 AsAsyncEnumerable 入口，正确维持上下文/连接生命周期与取消令牌。

索引候选如共享表的 `(TenantId, 排序列, Id)`，须结合筛选条件、实际 SQL 和执行计划决定，不能给所有实体统一加索引。不要为了并发 Count 与数据查询，在同一个 DbContext 上使用 Task.WhenAll。[官方分页指南](https://learn.microsoft.com/en-us/ef/core/querying/pagination)、[官方高效查询指南](https://learn.microsoft.com/en-us/ef/core/performance/efficient-querying)

### 5. 中优先级：全量取消跟踪使用 Clear，确认事件差异

位置：`Girvs.EntityFrameworkCore/DbContextExtensions/DbContextDetachExtension.cs:8`。

目前先 Entries().ToList() 再逐个 Detached，已避免旧的重复枚举，但仍产生快照列表、一次全局 DetectChanges 和逐条状态事件。全部取消跟踪时可使用 ChangeTracker.Clear。

3 个实体的探针结果：

| 方式 | DetectChangesStarting | StateChanged |
|---|---:|---:|
| 当前 DetachAll | 1 | 3 |
| ChangeTracker.Clear | 0 | 0 |

Clear 不触发逐条 StateChanged，因此不是事件行为完全相同的替换。先检查消费端事件订阅；按实体类型/ID 取消跟踪的方法不能直接使用 Clear，因为会清掉其他工作单元变更。IsWasTrack 也会枚举 Entries 并触发检测，只有实际分析显示其为热点后，再考虑 EF 公共本地查找 API 或批次化查询。[Clear 官方 API](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.changetracking.changetracker.clear)、[自动变更检测说明](https://learn.microsoft.com/en-us/ef/core/change-tracking/change-detection#methods-that-automatically-detect-changes)

### 6. 中优先级：明确只读查询，避免重复添加现有能力

位置：`Girvs.EntityFrameworkCore/Configuration/DataBaseConfig.cs:92`；`Girvs.EntityFrameworkCore/DbContextExtensions/DbContextOptionsBuilderExtensions.cs:159`；`Girvs.EntityFrameworkCore/Repositories/Repository.cs:135`。

当前 UseDataTracking 默认为 true，已有全局 NoTracking 配置，不能说框架尚不支持无跟踪。建议为只读列表增加每次查询的明确选项或 DTO 投影，保留读取后直接修改并 Commit 的现有跟踪用法。存在大量重复导航实体时再考虑 AsNoTrackingWithIdentityResolution，其本身也有身份解析开销。

探针：完整实体查询跟踪 3 条；现有 SelectProperties(Name) 的成员初始化投影跟踪 0 条。因此不能认为所有返回 TEntity 的投影都会跟踪，也不要把“给 SelectProperties 添加 AsNoTracking”列为本次已经证明的收益。DTO 的价值主要是清楚表达只读形状，避免部分实体被误用为完整实体去 Update。

UseLazyLoading 默认 false，方向已合理；启用后需通过业务请求 SQL 条数检查 N+1。不建议无差别给所有导航加 Include 或全局使用拆分查询。[官方跟踪指南](https://learn.microsoft.com/en-us/ef/core/querying/tracking)、[官方高效查询指南](https://learn.microsoft.com/en-us/ef/core/performance/efficient-querying)

## 二、依据热点再决定的实现优化

| 候选 | 源码位置 | 建议与边界 |
|---|---|---|
| 动态批量 setter 每次编译 | `DbContextExtensions/BatchUpdateBuilderSetExtensions.cs:25`、`:67` | net10/net11 每次反射筛选 SetProperty、构造表达式并 Compile。可缓存泛型方法或构建接收当前值的 setter 工厂；按字段/类型形状缓存时避免无限键数，禁止捕获实体值、上下文、模型或租户。这里只确认存在应用层编译成本，未断言 EF SQL 值参数化失效，先测高频批量更新的 CPU/分配。 |
| MySQL 首次版本探测并发 | `DbContextExtensions/DbContextOptionsBuilderExtensions.cs:47` | GetOrAdd 的 valueFactory 可能被并发执行多次，注释的“每库只探测一次”在首次并发时不成立。可显式配置 ServerVersion，或 Lazy 单次执行并设计失败后的重试/移除策略。当前已缓存，不能说每次请求都会 AutoDetect；本轮未连接真实 MySQL 测量。 |
| 分表初始化留在仓储构造 | `Repositories/Repository.cs:23`；`DbContextExtensions/DbContextExtension.cs:89` | 缓存命中仍计算连接指纹、取分表后缀并加锁；首次分表执行同步 Migrate，可能阻塞请求。先测首请求与命中路径，再评估部署/后台预建表、请求期就绪检查。迁移缓存隔离与按键锁已修复，不能把旧问题重复列为未修复。多实例协调仍由部署/Provider 锁承担。 |
| 分表模型基数 | `Migrations/GirvsTenantModelCacheKeyFactory.cs:11` | 物理表名随租户/年份变化，每种映射需要独立模型；监控活跃模型数量、冷启动和内存。参数化筛选不能消除这种结构差异，也不能为了省内存删除后缀缓存键导致串表。 |
| 重复租户校验与元数据查找 | `Repositories/Repository.cs:68`、`:78`、`:185` | 列表校验后 UpdateEntity 再次校验，并逐实体读取租户。大批量时可先取当前租户并做一次完整校验，内部复用已验证路径；优先级低于数据库查询/写入优化，必须保持整批验证和覆盖方法兼容语义。 |

GetOrAdd 工厂语义来源：[.NET 官方 ConcurrentDictionary.GetOrAdd](https://learn.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentdictionary-2.getoradd?view=net-11.0)。其余为源码分析推论，尚未用真实业务负载证明属于瓶颈。

## 三、EF11 已提供的改进与本项目边界

- net11.0 已引用 EF 11.0.0；官方已描述的多余 JOIN/ORDER BY 与无效 CAST 裁剪，主要由 Provider 自动应用，应检查生成 SQL，而非在仓储重复造优化器。
- JSON_CONTAINS 是 SQL Server 2025/兼容级别 170 的 JSON 集合能力，不是全部 Contains 查询的通用加速开关。普通参数集合翻译改进和 ExecuteUpdate 普通 lambda 属于 EF10 已有能力。
- 当前 Learn EF11 页面仍标注开发中，与本地包状态描述有差异。这里只引用已读取的官方行为，不按日期推断所有新特性和第三方 Provider 都已稳定。MySQL 当前采用 Microting 11.0.0-preview.1，应单独做真实数据库 SQL/执行计划验证。

来源：[EF11 官方新特性](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-11.0/whatsnew)、[EF10 官方新特性](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/whatsnew)。更多逐项来源与限制见 [official-research.md](official-research.md)。

不建议直接全局启用以下能力：

| 能力 | 当前限制 |
|---|---|
| AddDbContextPool | 上下文当前承载随机读库、切写库、分表模型和运行期迁移配置；需设计池边界与租借/归还状态管理，不能只替换 AddDbContext。连接池与 DbContext 池是两层能力。 |
| EF.CompileQuery/CompileAsyncQuery | 单个委托只能针对一个模型；动态分表不能按实体类型全局缓存一个委托。适合固定模型、固定形状且经测量的热点。 |
| 编译模型 | 官方文档仍列自定义 IModelCacheKeyFactory、代理等限制；当前可选分表/懒加载配置不能直接统一套用。大型固定模型冷启动才值得单独评估。 |
| 全局禁用 AutoDetectChanges | 容易丢失正常属性修改；只有确认大量跟踪实体的变更检测是热点后，在受控批次使用 try/finally，保证正确检测。 |

来源：[高级性能主题](https://learn.microsoft.com/en-us/ef/core/performance/advanced-performance-topics)、[官方变更检测指南](https://learn.microsoft.com/en-us/ef/core/change-tracking/change-detection)。

## 四、验证记录与后续验收

环境：Windows x64；SDK 11.0.100；.NET 11.0.0；EF/SQLite 11.0.0。

```powershell
dotnet test tests/Girvs.EntityFrameworkCore.Tests/Girvs.EntityFrameworkCore.Tests.csproj -f net11.0 --no-restore --verbosity quiet
dotnet run --project .scratch/efcore11-performance/probe/Probe.csproj -f net11.0 --verbosity quiet
```

- 现有测试：41 通过、0 失败、0 跳过。未把既有测试通过等同于无性能问题。
- 行为探针：实际执行内存 SQLite，不访问业务数据库；输出编译次数、投影排序异常、跟踪数量、修改字段及状态事件。
- 本次没有新增正式测试或修改生产实现；探针捕获预期翻译异常后继续运行，因此退出码 0 本身不代表缺陷消失。
- 本次没有测量响应时间、CPU/分配或 SQL Server/MySQL 的服务器计划；不提供未经测量的百分比收益。

实施优先序：参数化 → 排序/投影与字段更新 → 按业务增加分页/只读入口 → 确认事件语义后使用 Clear → 再依据 CPU/冷启动证据处理缓存、编译和初始化。

实施后先覆盖成功与关键边界：同形状不同值只编译一次；空值/可空值/多租户及公共数据隔离；排序字段未投影与排序重复值；字段更新保留其他属性和并发令牌；Clear 事件契约；分页取消和结果上限。之后以相同数据集在实际 SQL Server/MySQL 上比较命令次数、执行计划、逻辑读取、分配、吞吐与 P95。[官方性能诊断](https://learn.microsoft.com/en-us/ef/core/performance/performance-diagnosis)

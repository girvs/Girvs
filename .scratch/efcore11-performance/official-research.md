# EF Core 11 性能官方资料核验

核验日期：2026-11-17。仅评估，不修改生产代码，不声称已有性能实测收益。阅读了既有 `.scratch/efcore11-review/official-research.md`；本文聚焦性能，不重复上一轮兼容性整改。

## 版本边界

当前仓库 `net11.0` 引用 EF `11.0.0`，而本次读取的 Microsoft Learn EF11 页面仍称其开发中。因此以下是“当前官方已描述的行为”，不以环境日期推断全部 Provider 或全部计划均已发布。EF11 要求 .NET 11；其他 TFM 不能直接调用 EF11 API。[EF11 官方新特性](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-11.0/whatsnew)

## EF11 已描述的性能变化

- 引用导航查询会裁剪多余 JOIN、ORDER BY；SQL 后处理会移除无效 CAST。主要随升级自动受益，应以实际 SQL 确认，不必在 Girvs 重写同一优化。
- SQL Server 2025、兼容级别 170 下，JSON 列的标量集合 Contains 可译为 JSON_CONTAINS，并可结合 JSON 索引。它与普通 `ids.Contains(entity.Id)` 不同；现有库不宜全局强制开启 170。
- 复杂类型嵌套属性索引可用于具体业务模型，不是通用仓储的必备开关。

上述来源：[EF11 新特性](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-11.0/whatsnew)。官方示例基准不能代表 Girvs 的实际收益。

普通参数集合使用多标量参数及 padding 是 EF10 已有变化，不应称为 EF11 新能力。选择常量、JSON 参数或多标量参数需按数据分布和执行计划判断，不能认为某一种全局最快。ExecuteUpdate 的普通语句 lambda 也从 EF10 起提供。[EF10 查询翻译](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/whatsnew#improved-translation-for-parameterized-collection)、[EF10 ExecuteUpdate 变化](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/breaking-changes#executeupdateasync-now-accepts-a-regular-non-expression-lambda)

## 对本仓库最有价值的落点

| 优先级 | 代码证据 | 建议与验证 |
|---|---|---|
| 高 | `DbContextExtensions/IQueryableExtensions.cs` 的 WhereEqual；`Girvs/BusinessBasis/Repositories/GirvsRepositoryOtherQueryCondition.cs` 的动态租户条件均使用 Expression.Constant 保存请求值 | 改为可参数提取的值捕获表达式，使相同字段/运算的不同值共享查询形状。验证 SQL 参数、重复调用编译次数及查询缓存命中；不要把属性名等结构常量一并替换。 |
| 高 | `Repositories/Repository.cs` 的 GetByQueryAsync 固定 Count 后 Skip/Take；GetAll/GetWhere 返回全部匹配行 | 增加“不计总数”与游标分页入口；保留已有页码协议。排序需唯一，按排序列加主键、匹配复合索引。大结果导出考虑流式读取。 |
| 中 | `Repository.UpdateEntity` 忽略 fields 并 DbSet.Update | 根据指定字段标记修改，减少全列 UPDATE；同时验证 UpdateTime、并发令牌、已跟踪实体及导航图行为。该项兼有效率与正确性价值。 |
| 中 | `DbContextDetachExtension.cs` 的 DetachAll 逐条 Detached | 全部清理可评估 ChangeTracker.Clear；限定类型/指定 ID 的清理仍需定向处理。Clear 不触发逐实体 StateChanged，不能未经验证宣称行为完全相同。 |
| 中 | `DataConnectionConfig.UseDataTracking` 默认 true，已有全局 NoTracking 配置 | 面向只读列表提供显式 AsNoTracking 或 DTO 投影；保留查询后修改的跟踪行为。无需重复添加已有全局开关。 |
| 待测量 | `BatchUpdateBuilderSetExtensions` 每次反射查找 SetProperty 并 Compile setter | 可缓存稳定泛型 MethodInfo 或按字段形状构建带参数的委托；禁止缓存捕获上次值/上下文的委托。先测批量更新高频调用的 CPU/分配，不直接断言 EF SQL 参数化失效。 |

动态常量影响查询形状与缓存、编译查询的单模型限制，见[高级性能主题](https://learn.microsoft.com/en-us/ef/core/performance/advanced-performance-topics#dynamically-constructed-queries)。分页的唯一排序与游标策略见[分页指南](https://learn.microsoft.com/en-us/ef/core/querying/pagination)。投影、结果集、流式和懒加载 N+1 见[高效查询](https://learn.microsoft.com/en-us/ef/core/performance/efficient-querying)。按属性更新的行为见[变更跟踪](https://learn.microsoft.com/en-us/ef/core/change-tracking/)。Clear 的事件差异见[ChangeTracker.Clear API](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.changetracking.changetracker.clear?view=efcore-10.0)。只读与身份解析的取舍见[跟踪与不跟踪](https://learn.microsoft.com/en-us/ef/core/querying/tracking)。

## 不宜直接全局启用的开关

- DbContext Pool：当前有随机读库、切写库、分表上下文状态。池化需先设计租借时注入及归还时复位；不能只替换 AddDbContext。数据库连接池与上下文池不同。
- CompiledQuery：仅适合稳定单模型查询；当前按分表后缀切模型时，不能跨模型复用同一编译委托。
- CompiledModel：当前官方限制包含自定义 IModelCacheKeyFactory、代理；适用于大型固定模型冷启动，需重新生成维护。当前分表/可选懒加载路径不宜直接套用。

来源：[高级性能主题](https://learn.microsoft.com/en-us/ef/core/performance/advanced-performance-topics)。

自动 DetectChanges 通常不是瓶颈；Entries/HasChanges/SaveChanges 都可能触发检测。仅对已证明大量跟踪实体导致热点的受控批次考虑暂时禁用，并 finally 恢复、确保保存前检测正确，不能默认全局关闭。[变更检测](https://learn.microsoft.com/en-us/ef/core/change-tracking/change-detection)

已有条件批量更新/删除应保留；它们立即执行，绕过跟踪器，不能依赖之后的 SaveChanges 累积提交。扩展此路径时须保留租户过滤、写库路由及显式事务语义，避免与旧跟踪实体交错覆盖。[ExecuteUpdate/ExecuteDelete](https://learn.microsoft.com/en-us/ef/core/saving/execute-insert-update-delete)

## 建议验收

先用生产相近数据记录 SQL、命令次数、执行计划、查询编译、分配与 P95。对比参数化前后、带/不带 Count、深分页与游标、跟踪与 DTO、逐个 Detach 与 Clear。分别验 SQL Server 和实际 MySQL Provider；仅 SQL 生成测试不等同于数据库性能验证。[官方性能诊断](https://learn.microsoft.com/en-us/ef/core/performance/performance-diagnosis)

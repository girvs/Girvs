# EF Core 性能优化实施与验证

日期：2026-11-17。用户已授权按评估建议实施；保留单一组件和 net8/net9/net10/net11 多目标，没有修改 csproj 包引用，也没有执行数据库迁移或访问业务数据库。

## 已实施

1. 动态筛选与核心租户过滤使用强类型捕获表达式，固定查询形状。泛型工厂按类型缓存，只保存调用元数据，不保存请求值。可空类型按实际属性类型构造，保留公共数据和租户条件组合。
2. 页码查询先排序、定位再投影；默认 Id 降序，非主键排序增加 Id 降序作为唯一排序兜底。
3. UpdateAsync/列表 UpdateRangeAsync 尊重 fields。验证映射、写入权限、租户、空值和实体状态；保护已有跟踪变更与原始并发令牌，维护 UpdateTime。未跟踪且未指定字段的旧全量更新行为保留。
4. QueryBase 新增 IncludeRecordCount、HasMore、AsNoTracking、可选 MaxPageSize；默认仍统计总数并沿用上下文跟踪。无总数模式只查一条命令并额外取一行判断后续页，RecordCount=-1，PageCount=0。校验非法页码/条数和偏移量溢出，支持取消。
5. 增加只读游标 GetSliceAsync 和 StreamAsync。游标每页最多 1000 条，after 为业务显式提供的复合条件；始终与租户条件相交。接口采用默认实现维持第三方旧实现的编译兼容，Girvs EF 实现提供完整能力。
6. DetachAll 使用 ChangeTracker.Clear，不再执行逐条 Detached；事件差异已写入 API 和接入文档。按类型/ID 的清理仍定向执行。
7. EF10/11 的动态批量更新改用缓存泛型 setter 调用，不再逐次 Compile 表达式；EF8/9 保留表达式 API，缓存反射方法并参数化更新值。所有字段完整验证后才路由主库，缓存不保存值或上下文。

消费端示例与行为契约见 [EF 仓储说明](../../docs/efcore-performance.md)。

## 回归证据

先对旧实现新增并运行 6 项测试，全部失败，分别复现：WhereEqual 3 次编译、租户过滤 3 次编译、排序字段未投影时翻译失败、残缺实体更新 Count 被覆盖、跟踪更新标记无关字段、DetachAll 逐条触发 StateChanged。对应修复后全部通过。

后续覆盖：

- 不同筛选值/租户仍只编译一次，结果不串值；Guid 公共数据隔离；null/空字符串区分；可空数值查询。
- SQL Server/MySQL 实际命令包含参数且值正确，排序字段未投影也能翻译。拦截连接打开并在命令执行前捕获 SQL，不访问网络。
- 无总数模式仅一条读取命令；计数模式保留两条；越界只统计总数；HasMore、唯一排序、无跟踪、页大小及偏移量校验。
- 复合游标升序/降序、重复排序值、租户隔离、一条读取命令、不计总数；流式无跟踪、取消。
- 局部更新保护未指定字段；保留已跟踪修改、维护时间、保留并发原值、过期令牌触发并发异常；非法字段、非法后续实体、跨租户和已删除实体在设置更新前拒绝。
- 连续批量写入不同字段顺序/值，不复用旧值；既有多类型、空值、影子属性、事务和主库路由测试仍通过。

## 最终结果

| 项目 | net8.0 | net9.0 | net10.0 | net11.0 | 合计 |
|---|---:|---:|---:|---:|---:|
| Girvs.EntityFrameworkCore.Tests | 73 | 73 | 73 | 75 | 294 |
| Girvs.Core.Tests | 86 | 86 | 86 | 86 | 344 |

共 638 项通过、0 失败、0 跳过。测试命令同时构建对应框架的实际项目分支。本机没有 .NET 8/9 运行时，这两项目标设置 DOTNET_ROLL_FORWARD=Major，运行在已安装的 .NET 10 上；不视为原生 .NET 8/9 运行时验收。net10/net11 使用对应运行时。

下游 Sample.ServiceA（net10.0）构建通过：27 个既有警告、0 个错误。`git diff --check` 通过。

```powershell
$env:DOTNET_ROLL_FORWARD = 'Major'
dotnet test tests/Girvs.EntityFrameworkCore.Tests/Girvs.EntityFrameworkCore.Tests.csproj --no-restore --verbosity quiet
dotnet test tests/Girvs.Core.Tests/Girvs.Core.Tests.csproj --no-restore --verbosity quiet
dotnet build samples/Sample.ServiceA/Sample.ServiceA.csproj --no-restore --verbosity quiet
git diff --check
```

测试平台的 VSTEST_ARTIFACTS_DIRECTORY 提示及核心测试已有的 CS8632 警告不阻断验收。

## 验证中处理的问题

- 最初两组测试并行构建共享 Girvs 输出，触发文件写入争用；改为串行构建测试后通过。
- MySQL ToQueryString 预览会内联筛选值，初始断言据此失败。改为拦截实际读取命令，同时核对 SQL 参数占位符与参数值后，四目标 SQL Server/MySQL 测试均通过。没有为满足显示字符串断言而改变生产 Provider 配置。

## 边界与后续候选

SQLite 测试实际执行临时关系数据库读写；SQL Server/MySQL 只验命令翻译和参数，不等同于真实服务器运行/性能验收。已证明编译次数、命令数和更新范围改善，尚未声称吞吐或 P95 提升比例。

待有真实负载后，再决定 MySQL 版本探测的并发控制、分表预初始化/迁移请求路径、业务复合索引及模型基数监控。本次不启用上下文池化、编译模型/跨模型编译查询，不全局关闭自动变更检测。现有分表模型键和迁移协调规则保持。

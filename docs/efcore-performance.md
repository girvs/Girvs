# EF 仓储查询与更新

适用于当前 Girvs 的 net8/net9/net10/net11 目标。新增入口可通过 `IRepository<TEntity, TKey>` 使用，现有方法签名和单组件接入方式保持不变。

## 页码查询

`QueryBase<TEntity>` 默认仍统计总数、沿用 DbContext 的跟踪配置。排序基于完整实体，分页完成后才进行 `QueryFields` 投影，排序字段不必出现在投影中。未指定排序时按 Id 降序；指定其他字段时以 Id 降序作为第二排序键，避免重复排序值导致分页不稳定。

```csharp
var query = new OrderQuery
{
    PageIndex = 1,
    PageSize = 50,
    MaxPageSize = 1000,
    IncludeRecordCount = false,
    AsNoTracking = true,
    OrderBy = "CreateTime",
    QueryFields = ["Id", "CreateTime", "Amount"]
};
var rows = await repository.GetByQueryAsync(query, cancellationToken);
var hasMore = query.HasMore;
```

关闭 `IncludeRecordCount` 后，每次只执行页数据查询，多取一条判断 `HasMore`，返回结果仍不超过 PageSize。此时 `RecordCount = -1`，`PageCount = 0`，表示未统计；不能将它们当作真实总数。该选项会参与查询缓存键。

`MaxPageSize` 默认 null，保留旧调用的条数范围。对外部列表入口建议显式设置上限。页码小于 1、条数小于 1、超过上限或偏移量溢出时，在执行数据库查询前抛出 `ArgumentOutOfRangeException`。

只读查询可设置 `AsNoTracking = true`；默认 false 表示沿用 DbContext 配置，并不强制开启跟踪。原 `GetAllAsync/GetWhereAsync` 仍返回全部匹配行，大结果集应使用分页或流式入口。

## 游标分批查询

`GetSliceAsync` 不查询总数，不使用 Skip，默认只读；每页最多 1000 条。返回 `QuerySlice<TEntity>`，包含 Items 和 HasMore。调用方负责根据上一页最后一条记录构造复合 after 条件；after 仍与仓储租户过滤相交，不会替代租户条件。

以下示例假设 Order 的 Id 为整数，按 CreateTime、Id 降序：

```csharp
var first = await repository.GetSliceAsync(
    row => row.IsEnabled,
    row => row.CreateTime,
    pageSize: 50,
    cancellationToken: cancellationToken);

if (first.HasMore)
{
    var last = first.Items[^1];
    var next = await repository.GetSliceAsync(
        row => row.IsEnabled,
        row => row.CreateTime,
        after: row => row.CreateTime < last.CreateTime
            || (row.CreateTime == last.CreateTime && row.Id < last.Id),
        pageSize: 50,
        cancellationToken: cancellationToken);
}
```

`descending = false` 时主排序及 Id 都升序，after 的比较方向也必须改为大于。非唯一排序字段必须与 Id 一起构造条件。Guid、字符串、可空排序列的比较与数据库排序规则须由业务针对 Provider 明确处理；不能直接套用整数比较示例。必要时使用专用仓储提供适合该领域的游标编码和条件构造。

按实际查询验证索引，例如共享租户表可能需要 `(TenantId, CreateTime, Id)`；不要为所有实体统一增加索引。游标查询适合连续翻页，随机跳页仍使用页码查询。两类查询都不自动提供跨页快照一致性。

## 流式导出

```csharp
await foreach (var row in repository.StreamAsync(row => row.IsEnabled, cancellationToken))
{
    await exporter.WriteAsync(row, cancellationToken);
}
```

该入口不在仓储中把整个结果集 ToList，查询为无跟踪且保留租户过滤。枚举完成前保持 DI 作用域，不并行使用同一 DbContext；调用方释放异步枚举器会释放对应读取资源。查询提供程序、重试策略或业务导出器仍可能有自己的缓冲行为。

## 局部更新与并发

```csharp
await repository.UpdateAsync(entity, "Name", "Amount");
await unitOfWork.Commit(cancellationToken);
```

- 指定字段仅限允许写入的已映射 CLR 标量属性；拒绝重复、空白、未知字段、主键、租户字段、影子属性和保存后不可写字段。
- 对已跟踪实体，保留原始值、原始并发令牌与工作单元中已有的其他变更；fields 会追加修改标记，不会清除其他待保存修改。
- 对未跟踪实体，只挂接当前根实体并标记指定字段；传入实体需包含正确的主键、租户和原始并发令牌。若同键实体已经被跟踪，应修改已有实例。
- 未指定 fields 时，已跟踪实体使用实际属性变更检测；未跟踪实体保留原全量更新语义。
- 实现 `IIncludeUpdateTime` 的实体继续维护 UpdateTime。新增或删除状态的实体不能执行更新。
- 列表更新先校验整批租户、字段、指定值及实体状态，再设置修改标记。数据库保存的事务仍由工作单元/显式事务承担。

条件版 `UpdateRangeAsync(predicate, ...)` 和 `DeleteRangeAsync(predicate)` 仍在主库立即执行，不等待 Commit，不自动同步已经跟踪的实体。动态 setter 在 EF10/11 不再每次 Compile；缓存只保存泛型调用元数据，不缓存本次值、上下文或租户。

## 全部清理跟踪状态

`DetachAll()` 现在调用 `ChangeTracker.Clear()`，不会逐条触发 `StateChanged`，也不执行逐实体脱离前的全局 DetectChanges。需要逐实体事件的业务应显式枚举并设置 Detached；按类型/ID 清理的方法仍保留定向行为。清理前应先处理需要保存的变更。

## 兼容性

新增接口方法采用默认实现，使既有第三方仓储实现仍能编译：取消重载默认转发旧分页方法；游标和流式入口默认抛出 `NotSupportedException`，由支持这些能力的实现覆盖。Girvs EF 仓储已完整实现取消、游标和流式入口。

本次不全局开启 DbContext 池化、编译查询/模型或关闭自动变更检测。动态分表与读写切换的状态约束仍然保留；未调整包版本或执行数据库迁移。

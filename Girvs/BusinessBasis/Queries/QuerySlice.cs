namespace Girvs.BusinessBasis.Queries;

/// <summary>不统计总数的只读分批结果；最后一条记录可用于构建下一页的复合游标条件。</summary>
public sealed record QuerySlice<TEntity>(IReadOnlyList<TEntity> Items, bool HasMore);

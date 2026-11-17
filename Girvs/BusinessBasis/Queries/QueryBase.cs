using System.Collections.Concurrent;

namespace Girvs.BusinessBasis.Queries;

public abstract class QueryBase<TEntity> : IQuery<TEntity>
    where TEntity : Entity
{
    // 参与缓存键的属性由特性静态决定，按查询类型缓存一次。
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> CacheKeyProperties = new();

    public int PageStart => (PageIndex - 1) * PageSize;

    /// <summary>
    /// 当前页，从1开始
    /// </summary>
    [QueryCacheKey]
    public int PageIndex { get; set; } = 1;

    [QueryCacheKey]
    public int PageSize { get; set; } = 20;
    /// <summary>可选的分页上限。为兼容既有查询默认不限制；外部列表入口建议设置为 1000 或更小。</summary>
    public int? MaxPageSize { get; set; }
    /// <summary>默认统计总数；关闭时 RecordCount 为 -1，使用 HasMore 判断后续页。</summary>
    [QueryCacheKey]
    public bool IncludeRecordCount { get; set; } = true;
    /// <summary>只读列表可显式关闭实体跟踪，默认沿用上下文配置。</summary>
    public bool AsNoTracking { get; set; }
    public bool HasMore { get; set; }
    public int RecordCount { get; set; }
    public List<TEntity> Result { get; set; }

    [QueryCacheKey]
    public string OrderBy { get; set; } = string.Empty;

    // 整数上取整，避免 decimal 除法与装箱。
    public int PageCount => RecordCount < 0 || PageSize < 1 ? 0 : (int)(((long)RecordCount + PageSize - 1) / PageSize);

    public string[] QueryFields { get; set; }

    public virtual string GetCacheKey()
    {
        //此处字符串为约定，请不要随意修改
        var sb = new StringBuilder(GetType().FullName);

        var ps = CacheKeyProperties.GetOrAdd(
            GetType(),
            static type =>
                type.GetProperties()
                    .Where(propertyInfo =>
                        propertyInfo.IsDefined(typeof(QueryCacheKeyAttribute), true)
                    )
                    .ToArray()
        );

        foreach (var propertyInfo in ps)
        {
            // 用链式 Append 代替插值字符串，减少临时字符串分配。
            sb.Append(':')
                .Append(propertyInfo.Name)
                .Append('=')
                .Append(propertyInfo.GetValue(this));
        }

        if (QueryFields is { Length: > 0 })
        {
            sb.Append("QueryFields:").AppendJoin(',', QueryFields);
        }

        if (!string.IsNullOrEmpty(OrderBy))
        {
            sb.Append("OrderBy:").Append(OrderBy);
        }

        return sb.ToString().ToMd5();
    }

    public abstract Expression<Func<TEntity, bool>> GetQueryWhere();
}

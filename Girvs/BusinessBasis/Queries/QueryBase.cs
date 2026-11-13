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
    public int RecordCount { get; set; }
    public List<TEntity> Result { get; set; }

    [QueryCacheKey]
    public string OrderBy { get; set; } = string.Empty;

    // 整数上取整，避免 decimal 除法与装箱。
    public int PageCount => (RecordCount + PageSize - 1) / PageSize;

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

using System.Collections.Concurrent;

namespace Girvs.BusinessBasis.Queries;

public abstract class QueryBase<TEntity> : IQuery<TEntity>
    where TEntity : Entity
{
    // 参与缓存键的属性及其 getter 按查询类型缓存。
    private static readonly ConcurrentDictionary<Type, (string Name, Func<object, object?> Getter)[]>
        _cacheKeyProperties = new();

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
        var queryType = GetType();
        var sb = new StringBuilder(queryType.FullName);

        var properties = _cacheKeyProperties.GetOrAdd(
            queryType,
            static type =>
                type.GetProperties()
                    .Where(propertyInfo =>
                        propertyInfo.IsDefined(typeof(QueryCacheKeyAttribute), true)
                    )
                    .Select(propertyInfo => (propertyInfo.Name, CreateCacheKeyGetter(propertyInfo)))
                    .ToArray()
        );

        foreach (var (name, getter) in properties)
        {
            // 用链式 Append 代替插值字符串，减少临时字符串分配。
            sb.Append(':')
                .Append(name)
                .Append('=')
                .Append(getter(this));
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

    private static Func<object, object?> CreateCacheKeyGetter(PropertyInfo property)
    {
        var getter = property.GetGetMethod(nonPublic: true);
        if (!RuntimeFeature.IsDynamicCodeSupported || getter == null || getter.GetParameters().Length != 0 ||
            getter.ReturnType.IsByRef || getter.ReturnType.IsByRefLike || getter.ReturnType.IsPointer ||
            getter.ReturnType.IsFunctionPointer)
        {
            // 保留反射对索引器、无 getter 等特殊属性的行为。
            return property.GetValue;
        }

        var parameter = Expression.Parameter(typeof(object));
        var instance = getter.IsStatic ? null : Expression.Convert(parameter, getter.DeclaringType!);
        var call = Expression.Call(instance, getter);
        var read = Expression.Lambda<Func<object, object?>>(
            Expression.Convert(call, typeof(object)),
            parameter
        ).Compile();

        return target =>
        {
            try
            {
                return read(target);
            }
            catch (Exception exception)
            {
                // 与 PropertyInfo.GetValue 一样包装 getter 抛出的异常。
                throw new TargetInvocationException(exception);
            }
        };
    }

    public abstract Expression<Func<TEntity, bool>> GetQueryWhere();
}

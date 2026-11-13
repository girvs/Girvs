using System.Collections.Concurrent;

namespace Girvs.Extensions.Collections;

public static class IQueryableExtensions
{
    // 属性元数据静态不变，按 (类型, 属性名) 缓存，避免每次投影都反射查找。
    private static readonly ConcurrentDictionary<(Type, string), PropertyInfo> PropertyCache = new();

    public static IEnumerable<dynamic> SelectDynamic<T>(this IEnumerable<T> source, params string[] properties)
    {
        return SelectProperties<T>(source.AsQueryable(), properties).Cast<dynamic>();
    }

    public static IQueryable<T> SelectProperties<T>(
        this IQueryable<T> queryable,
        IEnumerable<string> selectedProperties)
    {
        // 先物化一次，避免 Any() 与后续 Select 重复枚举惰性序列。
        var propertyNames = selectedProperties?.ToArray();
        if (propertyNames is not { Length: > 0 }) return queryable;

        var type = typeof(T);
        // p =>
        var parameter = Expression.Parameter(type, "p");
            
        // create bindings for initialization
        var bindings = propertyNames
            .Select(s =>
                {
                    // property
                    var property = GetProperty(type, s);
                    // original value 
                    var propertyExpression = Expression.Property(parameter, property);
                    // set value "Property = p.Property"
                    return Expression.Bind(property, propertyExpression);
                }
            );
        // new PersonViewModel()
        var newViewModel = Expression.New(type);
        // new PersonViewModel { Property1 = p.Property1, ... }
        var body = Expression.MemberInit(newViewModel, bindings);
        // p => new PersonViewModel { Property1 = p.Property1, ... }
        var selector = Expression.Lambda<Func<T, T>>(body, parameter);

        return queryable.Select(selector);

    }

    private static PropertyInfo GetProperty(Type type, string propertyName)
    {
        if (PropertyCache.TryGetValue((type, propertyName), out var cached))
            return cached;

        var property = type.GetProperty(propertyName);
        if (property != null)
            PropertyCache[(type, propertyName)] = property;

        return property;
    }
}
using System.Collections.Concurrent;

namespace Girvs.Extensions;

public static class OrderByExtension
{
    // 反射查找方法只在启动期做一次，后续直接复用。
    private static readonly MethodInfo OrderByInternalMethod =
        typeof(OrderByExtension).GetMethod(nameof(OrderByInternal), BindingFlags.Public | BindingFlags.Static)!;

    private static readonly MethodInfo OrderByDescendingInternalMethod =
        typeof(OrderByExtension).GetMethod(nameof(OrderByDescendingInternal), BindingFlags.Public | BindingFlags.Static)!;

    // 属性与闭合泛型方法均为静态元数据，按 (实体类型, 属性名/属性类型, 是否倒序) 缓存。
    private static readonly ConcurrentDictionary<(Type, string), PropertyInfo> PropertyCache = new();

    private static readonly ConcurrentDictionary<(Type, Type, bool), MethodInfo> ClosedMethodCache = new();

    public static IQueryable<T> OrderBy<T>(this IQueryable<T> query, string propertyName)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        return string.IsNullOrEmpty(propertyName) ? query : _OrderBy<T>(query, propertyName, false);
    }

    public static IQueryable<T> OrderByDescending<T>(this IQueryable<T> query, string propertyName)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        return string.IsNullOrEmpty(propertyName) ? query : _OrderBy<T>(query, propertyName, true);
    }

    static IQueryable<T> _OrderBy<T>(IQueryable<T> query, string propertyName, bool isDesc)
    {
        var memberProp = GetProperty(typeof(T), propertyName);

        var method = ClosedMethodCache.GetOrAdd(
            (typeof(T), memberProp.PropertyType, isDesc),
            static key => (key.Item3 ? OrderByDescendingInternalMethod : OrderByInternalMethod)
                .MakeGenericMethod(key.Item1, key.Item2));

        return (IOrderedQueryable<T>) method.Invoke(null, new object[] {query, memberProp});
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

    public static IQueryable<T> OrderByInternal<T, TProp>(IQueryable<T> query, PropertyInfo memberProperty)
    {
        return query.OrderBy(_GetLamba<T, TProp>(memberProperty));
    }

    public static IQueryable<T> OrderByDescendingInternal<T, TProp>(IQueryable<T> query, PropertyInfo memberProperty)
    {
        return query.OrderByDescending(_GetLamba<T, TProp>(memberProperty));
    }

    static Expression<Func<T, TProp>> _GetLamba<T, TProp>(PropertyInfo memberProperty)
    {
        if (memberProperty.PropertyType != typeof(TProp)) throw new Exception();

        var thisArg = Expression.Parameter(typeof(T));
        var lambda = Expression.Lambda<Func<T, TProp>>(Expression.Property(thisArg, memberProperty), thisArg);
        return lambda;
    }
}

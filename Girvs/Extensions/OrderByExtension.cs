namespace Girvs.Extensions;

public static class OrderByExtension
{
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
        var methodname = (isDesc) ? "OrderByDescendingInternal" : "OrderByInternal";

        var memberProp = typeof(T).GetProperty(propertyName);

        var method = typeof(OrderByExtension).GetMethod(methodname)
            ?.MakeGenericMethod(typeof(T), memberProp.PropertyType);

        return (IOrderedQueryable<T>) method.Invoke(null, new object[] {query, memberProp});
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
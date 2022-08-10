namespace Girvs.EntityFrameworkCore.DbContextExtensions;

public static class IQueryableExtensions
{
    public static IQueryable<T> Where<T>(
        this IQueryable<T> query,
        bool condition,
        Expression<Func<T, bool>> predicate)
    {
        return condition
            ? query.Where(predicate)
            : query;
    }

    public static IQueryable<T> WhereEqual<T>(this IQueryable<T> q, string Field, string Value)
    {
        var param = Expression.Parameter(typeof(T), "p");
        var prop = Expression.Property(param, Field);
        var val = Expression.Constant(Value);
        var body = Expression.Equal(prop, val);
        var exp = Expression.Lambda<Func<T, bool>>(body, param);
        return Queryable.Where(q, exp);
    }
}
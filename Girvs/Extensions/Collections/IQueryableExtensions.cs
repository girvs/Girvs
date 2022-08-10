namespace Girvs.Extensions.Collections;

public static class IQueryableExtensions
{
    public static IEnumerable<dynamic> SelectDynamic<T>(this IEnumerable<T> source, params string[] properties)
    {
        return SelectProperties<T>(source.AsQueryable(), properties).Cast<dynamic>();
    }

    public static IQueryable<T> SelectProperties<T>(
        this IQueryable<T> queryable,
        IEnumerable<string> selectedProperties)
    {
        if (selectedProperties == null || !selectedProperties.Any()) return queryable;
            
        var type = typeof(T);
        // p =>
        var parameter = Expression.Parameter(type, "p");
            
        // create bindings for initialization
        var bindings = selectedProperties
            .Select(s =>
                {
                    // property
                    var property = type.GetProperty(s);
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
}
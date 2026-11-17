using System.Collections.Concurrent;

namespace Girvs.Extensions;

/// <summary>为动态表达式提供可由查询提供程序参数化的值捕获节点。</summary>
public static class CapturedExpression
{
    private static readonly MethodInfo CreateMethod = typeof(CapturedExpression)
        .GetMethod(nameof(CreateCore), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly ConcurrentDictionary<Type, Func<object, Expression>> Factories = new();

    public static Expression Create(object value, Type valueType)
    {
        ArgumentNullException.ThrowIfNull(valueType);
        return Factories.GetOrAdd(valueType, static type => CreateMethod.MakeGenericMethod(type)
            .CreateDelegate<Func<object, Expression>>())(value);
    }

    private static Expression CreateCore<T>(object value)
    {
        var captured = (T)value;
        Expression<Func<T>> expression = () => captured;
        return expression.Body;
    }
}

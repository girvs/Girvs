using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore.Query;

namespace Girvs.EntityFrameworkCore.DbContextExtensions;

public static class BatchUpdateBuilderSetExtensions
{
    internal static Task<int> ExecuteUpdateFieldsAsync<TEntity>(
        this IQueryable<TEntity> query,
        DbContext context,
        IReadOnlyList<KeyValuePair<string, object>> fields)
        where TEntity : class
    {
        var entityType = context.Model.FindEntityType(typeof(TEntity))
            ?? throw new GirvsException($"实体 {typeof(TEntity).Name} 未映射到 EF 模型");
        var names = new HashSet<string>(StringComparer.Ordinal);
        var tenantEntity = typeof(TEntity).GetInterfaces().Any(type =>
            type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IIncludeMultiTenant<>));
        var validated = new List<(IProperty Property, object Value)>();

        foreach (var (name, value) in fields)
        {
            if (string.IsNullOrWhiteSpace(name) || !names.Add(name))
                throw new GirvsException("批量更新字段名不能为空或重复");

            var property = entityType.FindProperty(name)
                ?? throw new GirvsException($"字段 {name} 不是已映射的标量属性");
            if (property.IsKey() || property.GetAfterSaveBehavior() != PropertySaveBehavior.Save
                || (tenantEntity && name == nameof(IIncludeMultiTenant<object>.TenantId)))
                throw new GirvsException($"字段 {name} 不允许批量更新");

            var valueType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
            if (value is null ? !property.IsNullable : !valueType.IsInstanceOfType(value))
                throw new GirvsException($"字段 {name} 的值必须符合类型 {property.ClrType.Name} 及其可空约束");

            validated.Add((property, value));
        }

        // 全部字段校验通过后才切库与执行，避免错误输入造成部分更新。
        context.SwitchReadWriteDataBase(DataBaseWriteAndRead.Write);
#if NET10_0_OR_GREATER
        // EF10 起接受普通委托：按属性类型缓存泛型调用，不再逐次编译捕获当前值的表达式。
        return query.ExecuteUpdateAsync(setters =>
        {
            foreach (var (property, value) in validated)
                SetterCache<TEntity>.Add(setters, CreateSelector<TEntity>(property), property.ClrType, value);
        });
#else
        var setters = Expression.Parameter(typeof(SetPropertyCalls<TEntity>), "setters");
        Expression body = setters;
        foreach (var (property, value) in validated)
            body = Expression.Call(body, SetterCache<TEntity>.GetMethod(property.ClrType),
                CreateSelector<TEntity>(property), CapturedExpression.Create(value, property.ClrType));
        var configure = Expression.Lambda<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>>(body, setters);
        return query.ExecuteUpdateAsync(configure);
#endif
    }

    private static LambdaExpression CreateSelector<TEntity>(IProperty property)
    {
        var entity = Expression.Parameter(typeof(TEntity), "entity");
        return Expression.Lambda(Expression.Call(typeof(EF), nameof(EF.Property), [property.ClrType],
            entity, Expression.Constant(property.Name)), entity);
    }

    private static class SetterCache<TEntity> where TEntity : class
    {
#if NET10_0_OR_GREATER
        private static readonly MethodInfo AddMethod = typeof(SetterCache<TEntity>)
            .GetMethod(nameof(AddCore), BindingFlags.NonPublic | BindingFlags.Static)!;
        private static readonly ConcurrentDictionary<Type, Action<UpdateSettersBuilder<TEntity>, LambdaExpression, object>> Adders = new();

        public static void Add(UpdateSettersBuilder<TEntity> setters, LambdaExpression selector, Type type, object value) =>
            Adders.GetOrAdd(type, static propertyType => AddMethod.MakeGenericMethod(propertyType)
                .CreateDelegate<Action<UpdateSettersBuilder<TEntity>, LambdaExpression, object>>())(setters, selector, value);

        private static void AddCore<TProperty>(UpdateSettersBuilder<TEntity> setters, LambdaExpression selector, object value) =>
            setters.SetProperty((Expression<Func<TEntity, TProperty>>)selector, (TProperty)value);
#else
        private static readonly MethodInfo SetProperty = typeof(SetPropertyCalls<TEntity>).GetMethods().Single(method =>
            method.Name == "SetProperty" && method.IsGenericMethodDefinition
            && method.GetParameters()[1].ParameterType.IsGenericParameter);
        private static readonly ConcurrentDictionary<Type, MethodInfo> Methods = new();

        public static MethodInfo GetMethod(Type type) => Methods.GetOrAdd(type,
            static propertyType => SetProperty.MakeGenericMethod(propertyType));
#endif
    }

#if !NET10_0_OR_GREATER
    public static Expression<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>> Append<TEntity>(
        this Expression<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>> left,
        Expression<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>> right)
    {
        var replace = new ReplacingExpressionVisitor(right.Parameters, new[] {left.Body});
        var combined = replace.Visit(right.Body);
        return Expression.Lambda<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>>(combined, left.Parameters);
    }
#endif
}

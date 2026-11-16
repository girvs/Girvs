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
        var entity = Expression.Parameter(typeof(TEntity), "entity");
        var names = new HashSet<string>(StringComparer.Ordinal);
        var tenantEntity = typeof(TEntity).GetInterfaces().Any(type =>
            type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IIncludeMultiTenant<>));
#if NET10_0_OR_GREATER
        var settersType = typeof(UpdateSettersBuilder<TEntity>);
#else
        var settersType = typeof(SetPropertyCalls<TEntity>);
#endif
        // 选取值重载，避免将字符串/对象当作属性选择器或依赖 EF 内部 API。
        var setProperty = settersType.GetMethods().Single(method =>
            method.Name == "SetProperty" && method.IsGenericMethodDefinition
            && method.GetParameters()[1].ParameterType.IsGenericParameter);
        var setters = Expression.Parameter(settersType, "setters");
#if NET10_0_OR_GREATER
        var calls = new List<Expression>();
#else
        Expression body = setters;
#endif

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

            var member = Expression.Call(typeof(EF), nameof(EF.Property), [property.ClrType],
                entity, Expression.Constant(name));
            var selector = Expression.Lambda(member, entity);
            Expression constant = value is null
                ? Expression.Constant(null, property.ClrType)
                : Expression.Convert(Expression.Constant(value, valueType), property.ClrType);
#if NET10_0_OR_GREATER
            calls.Add(Expression.Call(setters, setProperty.MakeGenericMethod(property.ClrType), selector, constant));
#else
            body = Expression.Call(body, setProperty.MakeGenericMethod(property.ClrType), selector, constant);
#endif
        }

        // 全部字段校验通过后才切库与执行，避免错误输入造成部分更新。
        context.SwitchReadWriteDataBase(DataBaseWriteAndRead.Write);
#if NET10_0_OR_GREATER
        calls.Add(Expression.Empty());
        var configure = Expression.Lambda<Action<UpdateSettersBuilder<TEntity>>>(Expression.Block(calls), setters).Compile();
        return query.ExecuteUpdateAsync(configure);
#else
        var configure = Expression.Lambda<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>>(body, setters);
        return query.ExecuteUpdateAsync(configure);
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

using System.Collections.Concurrent;

namespace Girvs.BusinessBasis.QueryTypeFields;

public static class TypeExtensions
{
    // 查询字段来自类型元数据，与实例无关；按类型缓存，避免每次请求都反射遍历属性。
    private static readonly ConcurrentDictionary<Type, QueryFieldMetadata> FieldCache = new();

    // 属性名到查询字段名的映射同样缓存，供 OrderBy 等高频入口直接查询。
    private static readonly ConcurrentDictionary<(Type, string), string> PropertyFieldCache = new();

    public static (string[], string) GetTypeQueryFieldsAndCacheKey(this Type type)
    {
        var metadata = GetMetadata(type);
        return ((string[])metadata.Fields.Clone(), metadata.CacheKey);
    }

    public static string GetTypeQueryFieldByPropertyName(this Type type, string propertyName)
    {
        return PropertyFieldCache.GetOrAdd(
            (type, propertyName),
            static key => GetTypeQueryFieldByProperty(key.Item1, key.Item1.GetProperty(key.Item2)));
    }

    public static string GetTypeQueryFieldByProperty(this Type type, PropertyInfo propertyInfo)
    {
        if (propertyInfo != null)
        {
            var ignore =
                Attribute.GetCustomAttribute(propertyInfo, typeof(QueryIgnoreAttribute)) as QueryIgnoreAttribute;
            if (ignore is not null || !CheckPropertyInfoValidity(propertyInfo)) return string.Empty;
            return Attribute.GetCustomAttribute(propertyInfo, typeof(QuerySourceMemberAttribute)) is not QuerySourceMemberAttribute sourceMember ? propertyInfo.Name : sourceMember.Name;
        }

        return string.Empty;
    }

    public static string[] GetTypeQueryFields(this Type type)
    {
        // 对外仍返回独立副本，避免调用方修改污染缓存。
        return (string[])GetMetadata(type).Fields.Clone();
    }

    private static QueryFieldMetadata GetMetadata(Type type)
    {
        return FieldCache.GetOrAdd(type, static t =>
        {
            var fields = t.GetProperties()
                .Select(propertyInfo => GetTypeQueryFieldByProperty(t, propertyInfo))
                .Where(fieldName => !string.IsNullOrEmpty(fieldName))
                .ToArray();

            var cacheKey = fields.Length > 0
                ? HashHelper.CreateHash(Encoding.UTF8.GetBytes(string.Join(',', fields)))
                : string.Empty;

            return new QueryFieldMetadata(fields, cacheKey);
        });
    }

    private sealed record QueryFieldMetadata(string[] Fields, string CacheKey);

    private static bool CheckPropertyInfoValidity(PropertyInfo propertyInfo)
    {
        var result = true;
        var getMethod = propertyInfo.GetMethod;
        if (getMethod != null)
        {
            result = !getMethod.IsStatic;
        }

        var setMethod = propertyInfo.SetMethod;
        result = setMethod != null && !setMethod.IsStatic;
        return result;
    }
}

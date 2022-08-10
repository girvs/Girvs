namespace Girvs.BusinessBasis.QueryTypeFields;

public static class TypeExtensions
{
    public static (string[], string) GetTypeQueryFieldsAndCacheKey(this Type type)
    {
        IList<string> fields = GetTypeQueryFields(type);

        if (fields.Any())
        {
            var bs = System.Text.Encoding.UTF8.GetBytes(string.Join(',', fields.ToArray()));
            var cacheKey = HashHelper.CreateHash(bs);
            return (fields.ToArray(), cacheKey);
        }

        return (fields.ToArray(), string.Empty);
    }


    public static string GetTypeQueryFieldByPropertyName(this Type type, string propertyName)
    {
        var propertyInfo = type.GetProperty(propertyName);
        return GetTypeQueryFieldByProperty(type, propertyInfo);
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
        var propertyInfos = type.GetProperties();
        var fields = propertyInfos.Select(propertyInfo => GetTypeQueryFieldByProperty(type, propertyInfo))
            .Where(fieldName => !string.IsNullOrEmpty(fieldName)).ToList();

        return fields.ToArray();
    }


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
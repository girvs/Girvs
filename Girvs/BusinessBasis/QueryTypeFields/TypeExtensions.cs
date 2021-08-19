using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Girvs.BusinessBasis.QueryTypeFields
{
    public static class TypeExtensions
    {
        public static (string[], string) GetTypeFieldsAndCacheKey(this Type type)
        {
            IList<string> fields = new List<string>();
            var propertyInfos = type.GetProperties();
            foreach (var propertyInfo in propertyInfos)
            {
                var ignore =
                    Attribute.GetCustomAttribute(propertyInfo, typeof(QueryIgnoreAttribute)) as QueryIgnoreAttribute;
                if (!(ignore is null) || !CheckPropertyInfoValidity(propertyInfo)) continue;
                var sourceMember =
                    Attribute.GetCustomAttribute(propertyInfo, typeof(QuerySourceMemberAttribute)) as
                        QuerySourceMemberAttribute;
                fields.Add(sourceMember is null ? propertyInfo.Name : sourceMember.Name);
            }

            if (fields.Any())
            {
                byte[] bs = System.Text.Encoding.UTF8.GetBytes(string.Join(',', fields.ToArray()));
                var cacheKey = HashHelper.CreateHash(bs);
                return (fields.ToArray(), cacheKey);
            }

            return (fields.ToArray(), string.Empty);
        }

        public static string[] GetTypeFields(this Type type)
        {
            IList<string> fields = new List<string>();
            var propertyInfos = type.GetProperties();
            foreach (var propertyInfo in propertyInfos)
            {
                var ignore =
                    Attribute.GetCustomAttribute(propertyInfo, typeof(QueryIgnoreAttribute)) as QueryIgnoreAttribute;
                if (!(ignore is null) || !CheckPropertyInfoValidity(propertyInfo)) continue;
                var sourceMember =
                    Attribute.GetCustomAttribute(propertyInfo, typeof(QuerySourceMemberAttribute)) as
                        QuerySourceMemberAttribute;
                fields.Add(sourceMember is null ? propertyInfo.Name : sourceMember.Name);
            }

            return fields.ToArray();
        }


        private static bool CheckPropertyInfoValidity(PropertyInfo propertyInfo)
        {
            bool result = true;
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
}
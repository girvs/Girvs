namespace Girvs.Swagger;

/// <summary>
///  swagger enum 支持
/// </summary>
public class SwaggerAddEnumDescriptions : IDocumentFilter
{
    public void Apply(Microsoft.OpenApi.Models.OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var dict = GetAllEnum();

        foreach (var (typeName, property) in swaggerDoc.Components.Schemas)
        {
            try
            {
                Type itemType = null;
                if (property.Enum == null || property.Enum.Count <= 0) continue;
                itemType = dict.ContainsKey(typeName) ? dict[typeName] : null;
                var list = property.Enum.Cast<OpenApiInteger>().ToList();
                property.Description += DescribeEnum(itemType, list);
            }
            catch
            {
                continue;
            }
        }
    }

    private static Dictionary<string, Type> GetAllEnum()
    {
        var typeFinder = new WebAppTypeFinder();
        var assemblies = typeFinder.GetAssemblies()
            .Where(x => x.FullName.Contains("Domain") || x.FullName.Contains("Application")).ToList();

        return assemblies.Select(ass => ass.GetTypes().Where(x => x.IsEnum).ToList())
            .Where(enumTypes => enumTypes.Any()).SelectMany(enumTypes => enumTypes).ToDictionary(item => item.Name);
    }

    private static string DescribeEnum(Type type, IEnumerable<OpenApiInteger> enums)
    {
        var enumDescriptions = (from item in enums
            where type != null
            let value = Enum.Parse(type, item.Value.ToString())
            let desc = GetDescription(type, value)
            select string.IsNullOrEmpty(desc)
                ? $"{item.Value.ToString()}:{Enum.GetName(type, value)}; "
                : $"{item.Value.ToString()}:{Enum.GetName(type, value)},{desc}; ").ToList();

        return $"<br/>{Environment.NewLine}{string.Join("<br/>" + Environment.NewLine, enumDescriptions)}";
    }

    private static string GetDescription(Type t, object value)
    {
        foreach (var mInfo in t.GetMembers())
        {
            if (mInfo.Name != t.GetEnumName(value)) continue;
            foreach (var attr in Attribute.GetCustomAttributes(mInfo))
            {
                if (attr.GetType() == typeof(DescriptionAttribute))
                {
                    return ((DescriptionAttribute)attr).Description;
                }
            }
        }

        return string.Empty;
    }
}
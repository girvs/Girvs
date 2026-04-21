using System.Reflection;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Girvs.OpenApi;

/// <summary>
/// 共享的 XML 注释加载器。
/// </summary>
internal static class XmlCommentStore
{
    private static readonly Lazy<Dictionary<string, string>> _summaries = new(Load);

    public static Dictionary<string, string> Summaries => _summaries.Value;

    private static Dictionary<string, string> Load()
    {
        var dict = new Dictionary<string, string>();
        var basePath = AppContext.BaseDirectory;

        foreach (var xmlFile in Directory.GetFiles(basePath, "Zhuofan.Ailynx.*.xml"))
        {
            try
            {
                var doc = XDocument.Load(xmlFile);
                foreach (var member in doc.Descendants("member"))
                {
                    var name = member.Attribute("name")?.Value;
                    if (name is null) continue;

                    var summaryText = member.Element("summary")?.Value?.Trim();
                    if (!string.IsNullOrEmpty(summaryText))
                        dict[name] = summaryText;

                    foreach (var paramEl in member.Elements("param"))
                    {
                        var paramName = paramEl.Attribute("name")?.Value;
                        var paramDesc = paramEl.Value?.Trim();
                        if (paramName is not null && !string.IsNullOrEmpty(paramDesc))
                        {
                            var dotMethodName = name.StartsWith("M:")
                                ? name[2..].Split('(')[0]
                                : name[2..];
                            dict[$"PARAM:{dotMethodName}.{paramName}"] = paramDesc;
                        }
                    }
                }
            }
            catch { /* 忽略无效 XML */ }
        }

        return dict;
    }

    /// <summary>
    /// 在类型及其基类中查找属性（忽略大小写）。
    /// </summary>
    public static PropertyInfo? FindProperty(Type type, string name)
    {
        var t = type;
        while (t is not null && t != typeof(object))
        {
            var prop = t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
            if (prop is not null) return prop;
            t = t.BaseType;
        }
        return null;
    }
}

/// <summary>
/// 操作级别：注入方法摘要、参数描述、[FromQuery] 复杂对象属性描述。
/// </summary>
public class XmlCommentsDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken ct)
    {
        var apiDescriptions = context.DescriptionGroups.SelectMany(g => g.Items);

        foreach (var apiDesc in apiDescriptions)
        {
            if (apiDesc.ActionDescriptor is not ControllerActionDescriptor descriptor)
                continue;

            var method = descriptor.MethodInfo;
            var memberKey = GetMethodXmlKey(method);

            var relativePath = apiDesc.RelativePath ?? "";
            var pathKey = "/" + relativePath;

            if (!document.Paths.TryGetValue(pathKey, out var pathItem))
                continue;

            var httpMethod = apiDesc.HttpMethod?.ToUpper();
            var opType = httpMethod?.ToLower();

            if (opType is null || !pathItem.Operations.TryGetValue(opType, out var operation))
                continue;

            if (XmlCommentStore.Summaries.TryGetValue(memberKey, out var summary))
                operation.Summary = summary;

            // 参数描述
            if (operation.Parameters is not null)
            {
                foreach (var param in operation.Parameters)
                {
                    // 1. 方法参数的 <param> 注释
                    var paramKey = $"PARAM:{method.DeclaringType?.FullName}.{method.Name}.{param.Name}";
                    if (XmlCommentStore.Summaries.TryGetValue(paramKey, out var paramDesc))
                    {
                        param.Description = paramDesc;
                        continue;
                    }

                    // 2. [FromQuery] 复杂对象展开的属性参数
                    foreach (var methodParam in method.GetParameters())
                    {
                        var paramType = methodParam.ParameterType;
                        if (paramType.IsPrimitive || paramType == typeof(string) ||
                            paramType == typeof(Guid) || paramType.IsEnum)
                            continue;

                        var clrProp = XmlCommentStore.FindProperty(paramType, param.Name);
                        if (clrProp is not null)
                        {
                            var declaringType = clrProp.DeclaringType ?? paramType;
                            var propXmlKey = $"P:{declaringType.FullName}.{clrProp.Name}";
                            if (XmlCommentStore.Summaries.TryGetValue(propXmlKey, out var propDesc))
                            {
                                param.Description = propDesc;
                                break;
                            }
                        }
                    }
                }
            }
        }

        return Task.CompletedTask;
    }

    private static string GetMethodXmlKey(MethodInfo method)
    {
        var typeName = method.DeclaringType?.FullName;
        var parameters = method.GetParameters();
        if (parameters.Length == 0)
            return $"M:{typeName}.{method.Name}";

        var paramStr = string.Join(",", parameters.Select(p => GetXmlTypeName(p.ParameterType)));
        return $"M:{typeName}.{method.Name}({paramStr})";
    }

    private static string GetXmlTypeName(Type type)
    {
        if (type.IsGenericType)
        {
            var genericDef = type.GetGenericTypeDefinition().FullName;
            genericDef = genericDef?[..genericDef.IndexOf('`')];
            var args = string.Join(",", type.GetGenericArguments().Select(GetXmlTypeName));
            return $"{genericDef}{{{args}}}";
        }
        return type.FullName ?? type.Name;
    }
}

/// <summary>
/// Schema 级别：注入类型描述和属性描述（FromBody / 返回实体字段注释）。
/// 使用 IOpenApiSchemaTransformer，框架直接提供精确的 Type 信息。
/// </summary>
public class XmlCommentsSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken ct)
    {
        var type = context.JsonTypeInfo.Type;

        // 类型描述
        var typeKey = $"T:{type.FullName}";
        if (XmlCommentStore.Summaries.TryGetValue(typeKey, out var typeDesc))
            schema.Description ??= typeDesc;

        // 属性描述
        if (schema.Properties is not null)
        {
            foreach (var (jsonPropName, propSchema) in schema.Properties)
            {
                var clrProp = XmlCommentStore.FindProperty(type, jsonPropName);
                if (clrProp is null) continue;

                var declaringType = clrProp.DeclaringType ?? type;
                var propKey = $"P:{declaringType.FullName}.{clrProp.Name}";
                if (XmlCommentStore.Summaries.TryGetValue(propKey, out var propDesc))
                    propSchema.Description ??= propDesc;
            }
        }

        return Task.CompletedTask;
    }
}

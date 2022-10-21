using System.Collections.Generic;
using System.Linq;

namespace Girvs.CodeGenerator.Generator;

public abstract class GirvsAbstractGenerator : IGenerator
{
    protected const string FixedTag = ".Domain";
    protected const string ResourceFixed = "Girvs.CodeGenerator.CodeTemplates.";

    public abstract string OutputFileName { get; }
    public abstract string GeneratorName { get; }

    protected abstract string TemplateResourceName { get; }
    
    protected virtual string GetCurrentNamespacePrefixName(string entityNamespaceName)
    {
        var fixedTagLocation = entityNamespaceName.IndexOf(FixedTag, StringComparison.Ordinal);
        return entityNamespaceName[..fixedTagLocation];
    }

    protected virtual string GetCurrentNamespaceSuffixName()
    {
        var resourcePath = TemplateResourceName.Replace(ResourceFixed, "");
        var namespaceSuffixName = resourcePath.Replace($".{GeneratorName}.tt", "");
        return namespaceSuffixName;
    }

    protected virtual string GetCurrentNamespaceName(string entityNamespaceName)
    {
        var namespaceSuffixName = GetCurrentNamespaceSuffixName();
        var namespacePrefixName = GetCurrentNamespacePrefixName(entityNamespaceName);
        return $"{namespacePrefixName}.{namespaceSuffixName}";
    }

    protected virtual TemplateNamespaceParameter CreateTemplateNamespaceParameter(Type entityType)
    {
        var p = new TemplateNamespaceParameter
        {
            EntityNamespaceName = entityType.Namespace,
            CurrentNamespacePrefixName = GetCurrentNamespacePrefixName(entityType.Namespace),
            CurrentNamespaceName = GetCurrentNamespaceName(entityType.Namespace)
        };

        return p;
    }

    protected virtual IEnumerable<TemplateFieldParameter> CreateTemplateFieldParameter(Type entityType)
    {
        var fields = entityType.GetProperties().Where(x => x.CanWrite);
        return fields.Select(x => new TemplateFieldParameter()
        {
            FieldName = x.Name,
            FieldTypeName = x.PropertyType.Name
        });
    }

    protected virtual dynamic CreateTemplateParameter(Type entityType)
    {
        return new
        {
            EntityName = entityType.Name,
            Fields = CreateTemplateFieldParameter(entityType),
            Namespace = CreateTemplateNamespaceParameter(entityType)
        };
    }

    protected virtual string GetOutputFilePath(Type entityType)
    {
        var namespacePrefixName = GetCurrentNamespacePrefixName(entityType.Namespace);
        var namespaceSuffixName = GetCurrentNamespaceSuffixName();
        var result = namespacePrefixName;
        var pathArray = namespaceSuffixName.Split('.');
        for (var i = 0; i < pathArray.Length; i++)
        {
            if (i == 0)
            {
                result += $".{pathArray[i]}";
            }
            else
            {
                result += $"/{pathArray[i]}";
            }
        }

        var fileName = OutputFileName.Replace("{EntityName}", entityType.Name);
        result += $"/{fileName}";

        return result;
    }

    protected virtual string GetResourceContent()
    {
        var assembly = typeof(GirvsAbstractGenerator).GetTypeInfo().Assembly;
        var stream = assembly.GetManifestResourceStream(TemplateResourceName);

        if (stream == null) throw new GirvsException("获取模板内容出错");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }


    public GenerateResult Generate(Type entityType)
    {
        var templateContent = GetResourceContent();
        var templateEngine = Template.Parse(templateContent);
        var templateParameter = CreateTemplateParameter(entityType);
        var parseContent = templateEngine.Render(Hash.FromAnonymousObject(templateParameter));
        return new GenerateResult(GetOutputFilePath(entityType), parseContent);
    }
}
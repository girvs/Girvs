using System.Collections.Generic;
using System.Linq;

namespace Girvs.CodeGenerator.Generator;

public class ParameterManager
{
    public IGenerator Generator { get; }
    public Type EntityType { get; }
    private const string FixedTag = ".Domain";
    private const string ResourceFixed = "Girvs.CodeGenerator.CodeTemplates.";

    public ParameterManager(IGenerator generator,Type entityType)
    {
        Generator = generator;
        EntityType = entityType;
    }

    public string GetFileSavePath()
    {
        var namespacePrefixName = GetCurrentNamespacePrefixName(EntityType.Namespace);
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

        var fileName = Generator.OutputFileName.Replace("{EntityName}", EntityType.Name);
        result += $"/{fileName}";

        return result;
    }

    public TemplateParameter GetBuildeTemplateParameter()
    {
        return  new TemplateParameter
        {
            PrimarykeyName = "Id",
            PrimarykeyTypeName = EntityType.GetProperty("Id")?.PropertyType.Name,
            EntityName = EntityType.Name,
            Fields = CreateTemplateFieldParameter().ToArray(),
            Namespace = CreateTemplateNamespaceParameter()
        };
    }
    
    protected virtual string GetCurrentNamespacePrefixName(string entityNamespaceName)
    {
        var fixedTagLocation = entityNamespaceName.IndexOf(FixedTag, StringComparison.Ordinal);
        return entityNamespaceName[..fixedTagLocation];
    }

    protected virtual string GetCurrentNamespaceSuffixName()
    {
        var resourcePath = Generator.TemplateResourceName.Replace(ResourceFixed, "");
        var namespaceSuffixName = resourcePath.Replace($".{Generator.GeneratorName}.tt", "");
        return namespaceSuffixName;
    }

    protected virtual string GetCurrentNamespaceName(string entityNamespaceName)
    {
        var namespaceSuffixName = GetCurrentNamespaceSuffixName();
        var namespacePrefixName = GetCurrentNamespacePrefixName(entityNamespaceName);
        return $"{namespacePrefixName}.{namespaceSuffixName}";
    }
    
    protected virtual TemplateNamespaceParameter CreateTemplateNamespaceParameter()
    {
        var p = new TemplateNamespaceParameter
        {
            EntityNamespaceName = EntityType.Namespace,
            CurrentNamespacePrefixName = GetCurrentNamespacePrefixName(EntityType.Namespace),
            CurrentNamespaceName = GetCurrentNamespaceName(EntityType.Namespace)
        };

        return p;
    }

    protected virtual (string,int) GetDbTypeName(Type fieldType)
    {
        if (fieldType == typeof(Guid))
        {
            return (string.Empty,-1);
        }

        if (fieldType == typeof(string))
        {
            return ("varchar",50);
        }
        
        if (fieldType == typeof(DateTime))
        {
            return ("datetime",-1);
        }

        return (string.Empty,-1);
    }

    protected virtual IEnumerable<TemplateFieldParameter> CreateTemplateFieldParameter()
    {
        var fields = EntityType.GetProperties().Where(x => x.CanWrite);
        return fields.Select(x =>
        {
            var (dbType, dbFieldMaxLength) = GetDbTypeName(x.PropertyType);
            return new TemplateFieldParameter()
            {
                FieldName = x.Name,
                FieldTypeName = x.PropertyType.Name,
                DbType = dbType,
                MaxLength = dbFieldMaxLength,
                IsPrimarykey = x.Name == "Id",
                Comment = string.Empty
            };
        });
    }
}
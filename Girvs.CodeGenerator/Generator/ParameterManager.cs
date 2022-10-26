using System.Collections.Generic;
using System.Linq;
using Girvs.BusinessBasis.Entities;

namespace Girvs.CodeGenerator.Generator;

public class ParameterManager
{
    public IGenerator Generator { get; }
    public Type EntityType { get; }
    private const string FixedTag = ".Domain";
    private const string ResourceFixed = "Girvs.CodeGenerator.CodeTemplates.";

    private XmlCommentHelper _xmlCommentHelper;

    private string[] _needFilterFileds =
    {
        nameof(BaseEntity.Id),
        nameof(IIncludeCreateTime.CreateTime),
        nameof(IIncludeCreatorId<int>.CreatorId),
        nameof(IIncludeCreatorName.CreatorName),
        nameof(IIncludeMultiTenant<int>.TenantId),
        nameof(IIncludeUpdateTime.UpdateTime),
        nameof(IIncludeMultiTenantName.TenantName),
        nameof(IIncludeDeleteField.IsDelete),
        nameof(IIncludeInitField.IsInitData)
    };

    public ParameterManager(IGenerator generator, Type entityType)
    {
        Generator = generator;
        EntityType = entityType;
        InitXmlComment();
    }

    private void InitXmlComment()
    {
        _xmlCommentHelper = new XmlCommentHelper();
        var xmlFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            $"{EntityType.Assembly.GetName().Name}.xml");
        _xmlCommentHelper.Load(xmlFilePath);
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
        return new TemplateParameter
        {
            PrimarykeyName = "Id",
            PrimarykeyTypeName = EntityType.GetProperty("Id")?.PropertyType.Name,
            EntityName = EntityType.Name,
            Fields = CreateTemplateFieldParameter().ToArray(),
            Namespace = CreateTemplateNamespaceParameter(),
            Comment = _xmlCommentHelper.GetTypeComment(EntityType)
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

    protected virtual (string, int) GetDbTypeName(Type fieldType)
    {
        if (fieldType == typeof(Guid))
        {
            return (string.Empty, -1);
        }

        if (fieldType == typeof(string))
        {
            return ("varchar", 50);
        }

        if (fieldType == typeof(DateTime))
        {
            return ("datetime", -1);
        }

        if (fieldType == typeof(decimal))
        {
            return ("decimal", 20);
        }

        if (fieldType.IsGenericType)
        {
            return ("text", -1);
        }

        return (string.Empty, -1);
    }

    protected virtual IEnumerable<TemplateFieldParameter> CreateTemplateFieldParameter()
    {
        var fields = EntityType.GetProperties().Where(x => x.CanWrite && !_needFilterFileds.Contains(x.Name));
        return fields.Select(x =>
        {
            var (dbType, dbFieldMaxLength) = GetDbTypeName(x.PropertyType);
            return new TemplateFieldParameter()
            {
                FieldName = x.Name,
                FieldTypeName = GetFieldTypeAlias(x.PropertyType),
                DbType = dbType,
                MaxLength = dbFieldMaxLength,
                IsPrimarykey = x.Name == "Id",
                Comment = _xmlCommentHelper.GetFieldOrPropertyComment(x),
                IsGenericType = x.PropertyType.IsGenericType
            };
        });
    }

    protected virtual string GetFieldTypeAlias(Type fieldType)
    {
        if (fieldType == typeof(string))
        {
            return "string";
        }

        if (fieldType == typeof(bool))
        {
            return "bool";
        }

        if (fieldType == typeof(long))
        {
            return "long";
        }

        if (fieldType == typeof(decimal))
        {
            return "decimal";
        }

        if (fieldType.IsGenericType)
        {
            if (fieldType.FullName == null) return "List<>";
            var fullName = fieldType.FullName;
            var tagIndex = fullName.IndexOf("[[", StringComparison.Ordinal);
            var typeStr = fullName[(tagIndex+2)..(fullName.Length-2)];
            var subType = Type.GetType(typeStr);
            return subType != null ? $"List<{subType.Name}>" : "List<>";
        }

        return fieldType.Name;
    }
}
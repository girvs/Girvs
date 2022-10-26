namespace Girvs.CodeGenerator.Generator;

public class TemplateParameter
{
    public string PrimarykeyName { get; set; }

    public string PrimarykeyTitleCaseName
    {
        get
        {
            if (PrimarykeyName.Length > 0)
            {
                return PrimarykeyName[..1].ToLower() + PrimarykeyName[1..];
            }

            return PrimarykeyName;
        }
    }

    public string PrimarykeyTypeName { get; set; }
    public string EntityName { get; set; }
    
    public string EntityNameTitleCaseName
    {
        get
        {
            if (EntityName.Length > 0)
            {
                return EntityName[..1].ToLower() + EntityName[1..];
            }

            return EntityName;
        }
    }
    
    public TemplateFieldParameter[] Fields { get; set; }
    public TemplateNamespaceParameter Namespace { get; set; }
    public string Comment { get; set; }
}
using System.Collections.Generic;

namespace Girvs.CodeGenerator.Generator;

public class TemplateParameter
{
    public string PrimarykeyName { get; set; }
    public string PrimarykeyTypeName { get; set; }
    public string EntityName { get; set; }
    public TemplateFieldParameter[] Fields { get; set; }
    public TemplateNamespaceParameter Namespace { get; set; }
    public string Comment { get; set; }
}
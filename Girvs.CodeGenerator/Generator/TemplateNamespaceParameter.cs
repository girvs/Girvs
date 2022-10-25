namespace Girvs.CodeGenerator.Generator;

public class TemplateNamespaceParameter :ILiquidizable
{
    public string EntityNamespaceName { get; set; }
    public string CurrentNamespaceName { get; set; }
    public string CurrentNamespacePrefixName { get; set; }
    public object ToLiquid()
    {
        return new
        {
            EntityNamespaceName = this.EntityNamespaceName,
            CurrentNamespaceName = this.CurrentNamespaceName,
            CurrentNamespacePrefixName = this.CurrentNamespacePrefixName
        };
    }
}
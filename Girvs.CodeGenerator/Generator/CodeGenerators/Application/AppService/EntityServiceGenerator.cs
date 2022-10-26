namespace Girvs.CodeGenerator.Generator.CodeGenerators.Application.AppService;

public class EntityServiceGenerator : GirvsAbstractGenerator
{
    public override string OutputFileName { get; } = "{EntityName}Service.cs";
    public override string GeneratorName { get; } = "Service";
    public override string TemplateResourceName { get; } = "Girvs.CodeGenerator.CodeTemplates.Application.AppService.Service.tt";
}
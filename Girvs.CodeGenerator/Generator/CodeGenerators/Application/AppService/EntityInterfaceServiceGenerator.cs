namespace Girvs.CodeGenerator.Generator.CodeGenerators.Application.AppService;

public class EntityInterfaceServiceGenerator : GirvsAbstractGenerator
{
    public override string OutputFileName { get; } = "I{EntityName}Service.cs";
    public override string GeneratorName { get; } = "IService";
    public override string TemplateResourceName { get; } = "Girvs.CodeGenerator.CodeTemplates.Application.AppService.IService.tt";
}
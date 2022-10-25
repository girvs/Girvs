namespace Girvs.CodeGenerator.Generator.CodeGenerators.Infrastructure.EntityConfigurations;

public class EntityTypeConfiguationGenerator : GirvsAbstractGenerator
{
    public override string OutputFileName { get; } = "{EntityName}EntityTypeConfiguation.cs";
    public override string GeneratorName { get; } = "EntityTypeConfiguation";
    public override string TemplateResourceName { get; } = "Girvs.CodeGenerator.CodeTemplates.Infrastructure.EntityConfigurations.EntityTypeConfiguation.tt";
}
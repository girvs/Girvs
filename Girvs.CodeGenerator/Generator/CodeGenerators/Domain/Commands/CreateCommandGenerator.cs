namespace Girvs.CodeGenerator.Generator.CodeGenerators.Domain.Commands;


public class CreateCommandGenerator : GirvsAbstractGenerator
{
    public override string OutputFileName { get; } = "Create{EntityName}Command.cs";
    public override string GeneratorName { get; } = "CreateCommand";
    protected override string TemplateResourceName { get; } = "Girvs.CodeGenerator.CodeTemplates.Domain.Commands.CreateCommand.tt";
}
namespace Girvs.CodeGenerator.Generator.CodeGenerators.Domain.Commands;


public class CreateCommandGenerator : GirvsAbstractGenerator
{
    public override string OutputFileName { get; } = "{EntityName}\\Create{EntityName}Command.cs";
    public override string GeneratorName { get; } = "CreateCommand";
    public override string TemplateResourceName { get; } = "Girvs.CodeGenerator.CodeTemplates.Domain.Commands.CreateCommand.tt";
}
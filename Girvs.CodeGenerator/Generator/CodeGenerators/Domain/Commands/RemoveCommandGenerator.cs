namespace Girvs.CodeGenerator.Generator.CodeGenerators.Domain.Commands;


public class RemoveCommandGenerator : GirvsAbstractGenerator
{
    public override string OutputFileName { get; } = "Remove{EntityName}Command.cs";
    public override string GeneratorName { get; } = "RemoveCommand";
    protected override string TemplateResourceName { get; } = "Girvs.CodeGenerator.CodeTemplates.Domain.Commands.RemoveCommand.tt";
}
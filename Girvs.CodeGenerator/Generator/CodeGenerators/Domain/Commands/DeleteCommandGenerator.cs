namespace Girvs.CodeGenerator.Generator.CodeGenerators.Domain.Commands;


public class DeleteCommandGenerator : GirvsAbstractGenerator
{
    public override string OutputFileName { get; } = "{EntityName}\\Delete{EntityName}Command.cs";
    public override string GeneratorName { get; } = "DeleteCommand";
    public override string TemplateResourceName { get; } = "Girvs.CodeGenerator.CodeTemplates.Domain.Commands.DeleteCommand.tt";
}
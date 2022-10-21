namespace Girvs.CodeGenerator.Generator.CodeGenerators.Domain.Commands;


public class UpdateCommandGenerator : GirvsAbstractGenerator
{
    public override string OutputFileName { get; } = "Update{EntityName}Command.cs";
    public override string GeneratorName { get; } = "UpdateCommand";
    protected override string TemplateResourceName { get; } = "Girvs.CodeGenerator.CodeTemplates.Domain.Commands.UpdateCommand.tt";
}
namespace Girvs.CodeGenerator.Generator.CodeGenerators.Domain.CommandHandlers;

public class CommandHandlerGenerator : GirvsAbstractGenerator
{
    public override string OutputFileName { get; } = "{EntityName}CommandHandler.cs";
    public override string GeneratorName { get; } = "CommandHandler";
    public override string TemplateResourceName { get; } = "Girvs.CodeGenerator.CodeTemplates.Domain.CommandHandlers.CommandHandler.tt";
}
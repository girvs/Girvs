namespace Girvs.CodeGenerator.Generator.CodeGenerators.Application.ViewModels;

public class QueryViewModelGenerator : GirvsAbstractGenerator
{
    public override string OutputFileName { get; } = "{EntityName}QueryViewModel.cs";
    public override string GeneratorName { get; } = "QueryViewModel";
    public override string TemplateResourceName { get; } = "Girvs.CodeGenerator.CodeTemplates.Application.ViewModels.QueryViewModel.tt";
}
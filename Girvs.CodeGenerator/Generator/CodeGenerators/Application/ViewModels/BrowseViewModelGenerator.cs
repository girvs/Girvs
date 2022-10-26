namespace Girvs.CodeGenerator.Generator.CodeGenerators.Application.ViewModels;

public class BrowseViewModelGenerator : GirvsAbstractGenerator
{
    public override string OutputFileName { get; } = "{EntityName}BrowseViewModel.cs";
    public override string GeneratorName { get; } = "BrowseViewModel";
    public override string TemplateResourceName { get; } = "Girvs.CodeGenerator.CodeTemplates.Application.ViewModels.BrowseViewModel.tt";
}
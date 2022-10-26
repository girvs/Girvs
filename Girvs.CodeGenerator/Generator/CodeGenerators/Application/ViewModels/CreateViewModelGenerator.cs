namespace Girvs.CodeGenerator.Generator.CodeGenerators.Application.ViewModels;

public class CreateViewModelGenerator : GirvsAbstractGenerator
{
    public override string OutputFileName { get; } = "{EntityName}\\{EntityName}CreateViewModel.cs";
    public override string GeneratorName { get; } = "CreateViewModel";
    public override string TemplateResourceName { get; } = "Girvs.CodeGenerator.CodeTemplates.Application.ViewModels.CreateViewModel.tt";
}
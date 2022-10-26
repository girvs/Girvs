namespace Girvs.CodeGenerator.Generator.CodeGenerators.Application.ViewModels;

public class UpdateViewModelGenerator : GirvsAbstractGenerator
{
    public override string OutputFileName { get; } = "{EntityName}UpdateViewModel.cs";
    public override string GeneratorName { get; } = "UpdateViewModel";
    public override string TemplateResourceName { get; } = "Girvs.CodeGenerator.CodeTemplates.Application.ViewModels.UpdateViewModel.tt";
}
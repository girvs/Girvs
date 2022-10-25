namespace Girvs.CodeGenerator.Generator.CodeGenerators.Domain.Repositories;

public class EntityInterfaceRepositoryGenerator : GirvsAbstractGenerator
{
    public override string OutputFileName { get; } = "I{EntityName}Repository.cs";
    public override string GeneratorName { get; } = "IEntityRepository";
    public override string TemplateResourceName { get; } = "Girvs.CodeGenerator.CodeTemplates.Domain.Repositories.IEntityRepository.tt";
}
namespace Girvs.CodeGenerator.Generator.CodeGenerators.Infrastructure.Repositories;

public class EntityRepositoryGenerator : GirvsAbstractGenerator
{
    public override string OutputFileName { get; } = "{EntityName}Repository.cs";
    public override string GeneratorName { get; } = "EntityRepository";
    public override string TemplateResourceName { get; } = "Girvs.CodeGenerator.CodeTemplates.Infrastructure.Repositories.EntityRepository.tt";
}
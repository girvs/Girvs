namespace Girvs.CodeGenerator.Generator.CodeGenerators.Domain.Queries;

public class EntityQueryGenerator : GirvsAbstractGenerator
{
    public override string OutputFileName { get; } = "{EntityName}Query.cs";
    public override string GeneratorName { get; } = "EntityQueryGenerator";
    public override string TemplateResourceName { get; } = "Girvs.CodeGenerator.CodeTemplates.Domain.Queries.EntityQuery.tt";
}
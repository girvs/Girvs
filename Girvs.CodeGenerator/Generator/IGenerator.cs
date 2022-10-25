namespace Girvs.CodeGenerator.Generator;

public interface IGenerator
{
    string OutputFileName { get; }
    string GeneratorName { get; }
    string TemplateResourceName { get; }
    
    string Generate(Type entityType,TemplateParameter templateParameter);
}
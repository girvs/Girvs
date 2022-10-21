namespace Girvs.CodeGenerator.Generator;

public interface IGenerator
{
    string OutputFileName { get; }
    string GeneratorName { get; }
    GenerateResult Generate(Type entityType);
}
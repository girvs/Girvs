namespace Girvs.ServiceGovernance.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class OtlpEnvironmentVariableCollection
{
    public const string Name = "OTLP 环境变量串行测试";
}

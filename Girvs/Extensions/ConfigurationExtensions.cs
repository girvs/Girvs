namespace Girvs.Extensions;

public static partial class ConfigurationExtensions
{
    public static void ReplaceEnvironmentVariables(this IConfiguration configuration)
    {
        foreach (var section in configuration.GetChildren())
        {
            ReplaceEnvironmentVariables(section);
        }
    }

    private static void ReplaceEnvironmentVariables(IConfigurationSection section)
    {
        if (section.Value != null)
        {
            section.Value = ReplaceEnvironmentVariablesInValue(section.Value);
        }

        foreach (var child in section.GetChildren())
        {
            ReplaceEnvironmentVariables(child);
        }
    }

    /// <summary>
    /// 替换单个配置值中的 ${ENV_VAR} 占位符；环境变量不存在时保留原占位符。
    /// </summary>
    internal static string ReplaceEnvironmentVariablesInValue(string value)
    {
        if (string.IsNullOrEmpty(value) || !value.Contains("${", StringComparison.Ordinal))
            return value;

        return EnvironmentVariableRegex()
            .Replace(
                value,
                match =>
                {
                    var envVarName = match.Groups[1].Value;
                    var envVarValue = Environment.GetEnvironmentVariable(envVarName);
                    return envVarValue ?? match.Value; // 如果找不到环境变量，保留占位符
                }
            );
    }

    [GeneratedRegex(@"\$\{([a-zA-Z_][a-zA-Z0-9_]*)\}")]
    private static partial Regex EnvironmentVariableRegex();
}

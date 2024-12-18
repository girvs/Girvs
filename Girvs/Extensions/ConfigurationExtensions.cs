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
            // 替换掉占位符 ${}，例如 ${DB_SERVER} 替换为环境变量 DB_SERVER 的值
            section.Value = MyRegex()
                .Replace(
                    section.Value,
                    match =>
                    {
                        var envVarName = match.Groups[1].Value;
                        var envVarValue = Environment.GetEnvironmentVariable(envVarName);
                        return envVarValue ?? match.Value; // 如果找不到环境变量，保留占位符
                    }
                );
        }

        foreach (var child in section.GetChildren())
        {
            ReplaceEnvironmentVariables(child);
        }
    }

    [GeneratedRegex(@"\$\{([a-zA-Z_][a-zA-Z0-9_]*)\}")]
    private static partial Regex MyRegex();
}

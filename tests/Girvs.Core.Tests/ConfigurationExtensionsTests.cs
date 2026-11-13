using Girvs.Extensions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Girvs.Core.Tests;

public class ConfigurationExtensionsTests
{
    [Fact]
    public void ReplaceEnvironmentVariables_已有环境变量_替换占位符()
    {
        const string variableName = "GIRVS_TEST_PLACEHOLDER";
        Environment.SetEnvironmentVariable(variableName, "resolved-value");

        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Section:Key"] = $"prefix-${{{variableName}}}-suffix",
                    }
                )
                .Build();

            configuration.ReplaceEnvironmentVariables();

            Assert.Equal("prefix-resolved-value-suffix", configuration["Section:Key"]);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, null);
        }
    }

    [Fact]
    public void ReplaceEnvironmentVariables_环境变量缺失_保留占位符()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Section:Key"] = "${GIRVS_NOT_EXISTS_PLACEHOLDER}",
                }
            )
            .Build();

        configuration.ReplaceEnvironmentVariables();

        Assert.Equal("${GIRVS_NOT_EXISTS_PLACEHOLDER}", configuration["Section:Key"]);
    }

    [Fact]
    public void ReplaceEnvironmentVariables_嵌套节点_递归替换()
    {
        const string variableName = "GIRVS_TEST_NESTED";
        Environment.SetEnvironmentVariable(variableName, "nested-value");

        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["Outer:Inner:Value"] = $"${{{variableName}}}",
                    }
                )
                .Build();

            configuration.ReplaceEnvironmentVariables();

            Assert.Equal("nested-value", configuration["Outer:Inner:Value"]);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, null);
        }
    }

    [Fact]
    public void ReplaceEnvironmentVariables_Json配置源_替换占位符()
    {
        const string variableName = "GIRVS_TEST_JSON_PLACEHOLDER";
        Environment.SetEnvironmentVariable(variableName, "json-value");

        var path = Path.Combine(Path.GetTempPath(), $"girvs-config-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, "{\"Section\":{\"Key\":\"${" + variableName + "}\"}}");

        try
        {
            var configuration = new ConfigurationBuilder().AddJsonFile(path).Build();

            configuration.ReplaceEnvironmentVariables();

            Assert.Equal("json-value", configuration["Section:Key"]);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, null);
            File.Delete(path);
        }
    }
}

namespace Girvs.Aspire.Hosting;

public static class GirvsServiceAppSettings
{
    /// <summary>合并共享默认配置与服务本地配置，服务本地配置优先。</summary>
    public static IConfiguration ReadEffective(IConfiguration sharedConfiguration, string projectDirectory)
    {
        var builder = new ConfigurationBuilder();
        if (sharedConfiguration != null)
            builder.AddConfiguration(sharedConfiguration);
        if (!string.IsNullOrEmpty(projectDirectory))
        {
            builder.AddJsonFile(
                Path.Combine(projectDirectory, "appsettings.json"),
                optional: true,
                reloadOnChange: false
            );
        }

        return builder.Build();
    }

    /// <summary>
    /// 读取服务项目目录下的 appsettings.json（与服务端 ConfigurationDefaults.AppSettingsFilePath 一致）。
    /// 目录为空或文件不存在时返回空配置（各贡献器随之降级跳过）。
    /// </summary>
    public static IConfiguration Read(string projectDirectory)
    {
        var builder = new ConfigurationBuilder();
        if (!string.IsNullOrEmpty(projectDirectory))
        {
            builder.AddJsonFile(
                Path.Combine(projectDirectory, "appsettings.json"),
                optional: true,
                reloadOnChange: false
            );
        }

        return builder.Build();
    }
}

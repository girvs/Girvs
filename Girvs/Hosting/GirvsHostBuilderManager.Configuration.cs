namespace Girvs;

public static partial class GirvsHostBuilderManager
{
    public static void HostUseGirvsConfig(
        this IConfigurationBuilder config,
        IWebHostEnvironment webHostEnvironment,
        string[] otherJsonFiles = null,
        string[] args = null
    )
    {
        //清除原有的源
        config.Sources.Clear();

        // 共享配置文件(GIRVS_SHARED_CONFIG 指向,由 Aspire AppHost 注入或 K8s ConfigMap 挂载):
        // 作为最低优先级配置源,服务本地 appsettings*.json 与环境变量天然覆盖其中的同名键。
        var sharedConfigPath = Environment.GetEnvironmentVariable("GIRVS_SHARED_CONFIG");

        if (string.IsNullOrWhiteSpace(sharedConfigPath))
            sharedConfigPath = "girvs.shared.json";

        config.AddJsonFile(sharedConfigPath, optional: true, reloadOnChange: true);

        config.AddJsonFile(ConfigurationDefaults.AppSettingsFilePath, true, true);
        // 临时兼容旧项目对 Serilog.json 的依赖，待业务全部迁移至 appsettings.json 的 Serilog 节后移除。
        config.AddJsonFile(ConfigurationDefaults.SerilogSettingFilePath, true, true);
        if (otherJsonFiles is {Length: > 0})
        {
            foreach (var otherJsonFile in otherJsonFiles)
            {
                config.AddJsonFile(otherJsonFile, true, true);
            }
        }

        var appSettingsEnvironmentNameFilePath =
            ConfigurationDefaults.AppSettingsEnvironmentNameFilePath(
                webHostEnvironment.EnvironmentName
            );
        config.AddJsonFile(appSettingsEnvironmentNameFilePath, true, true);

        if (
            webHostEnvironment.IsDevelopment()
            && webHostEnvironment.ApplicationName is {Length: > 0}
        )
        {
            try
            {
                var appAssembly = Assembly.Load(
                    new AssemblyName(webHostEnvironment.ApplicationName)
                );
                config.AddUserSecrets(appAssembly, optional: true, reloadOnChange: true);
            }
            catch (FileNotFoundException)
            {
                // The assembly cannot be found, so just skip it.
            }
        }

        config.AddEnvironmentVariables();
        if (args is {Length: > 0})
            config.AddCommandLine(args);

        // 占位符替换：配置值可能来自多个源（JSON/环境变量/命令行），先构建一次快照取出合并后的值，
        // 再把替换成功的键以最高优先级内存源叠加。这样 ${ENV_VAR} 真正生效，
        // 同时保留原配置源及其 reloadOnChange（不再用已构建配置替换 Sources）。
        var placeholderOverrides = ResolveEnvironmentVariablePlaceholders(config);
        if (placeholderOverrides.Count > 0)
            config.AddInMemoryCollection(placeholderOverrides);
    }

    private static IDictionary<string, string> ResolveEnvironmentVariablePlaceholders(
        IConfigurationBuilder config
    )
    {
        var overrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Build 会为每个源创建独立的 provider 实例；用完立即释放，避免遗留文件监视器。
        var snapshot = config.Build();
        try
        {
            foreach (var item in snapshot.AsEnumerable())
            {
                if (item.Value is not {Length: > 0} value)
                    continue;

                var replaced = Girvs
                    .Extensions
                    .ConfigurationExtensions
                    .ReplaceEnvironmentVariablesInValue(value);
                if (!string.Equals(replaced, value, StringComparison.Ordinal))
                    overrides[item.Key] = replaced;
            }
        }
        finally
        {
            (snapshot as IDisposable)?.Dispose();
        }

        return overrides;
    }
}

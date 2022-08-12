namespace Girvs.Configuration;

public class AppSettings
{
    public CommonConfig CommonConfig { get; set; } = new CommonConfig();

    public HostingConfig HostingConfig { get; set; } = new HostingConfig();

    public IConfig this[string index]
    {
        get => ModuleConfigurations[index];
        set => ModuleConfigurations[index] = value;
    }

    public IDictionary<string, IConfig> ModuleConfigurations { get; private set; } = null;

    public void PreLoadModelConfig()
    {
        ModuleConfigurations = new Dictionary<string, IConfig>();
    }

    /// <summary>
    /// Get configuration parameters by type
    /// </summary>
    /// <typeparam name="TConfig">Configuration type</typeparam>
    /// <returns>Configuration parameters</returns>
    public TConfig Get<TConfig>() where TConfig : class, IConfig
    {
        if (ModuleConfigurations[typeof(TConfig).Name] is not TConfig config)
            throw new GirvsException($"No configuration with type '{typeof(TConfig)}' found");

        return config;
    }
    
    /// <summary>
    /// Get configuration parameters by type
    /// </summary>
    /// <returns>Configuration parameters</returns>
    public dynamic Get(string moduleConfigName)
    {
        return ModuleConfigurations[moduleConfigName];
    }
}
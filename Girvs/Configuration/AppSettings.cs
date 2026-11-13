using Girvs.Configuration.Resources;

namespace Girvs.Configuration;


public class AppSettings
{
    public Dictionary<string, GirvsInfrastructureResource> Resources { get; set; } = new();

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
        if (TryGet<TConfig>(out var config))
            return config;

        // 区分“配置系统尚未初始化”和“确实缺少该模块配置”，便于启动期定位问题。
        var message = ModuleConfigurations is null
            ? "AppSettings 尚未初始化，请先执行 PreLoadModelConfig 或通过依赖注入容器获取配置"
            : $"No configuration with type '{typeof(TConfig)}' found";

        throw new GirvsException(message);
    }

    /// <summary>
    /// 尝试获取指定类型的模块配置，未初始化或不存在时返回 false。
    /// </summary>
    /// <typeparam name="TConfig">Configuration type</typeparam>
    /// <param name="config">匹配到的配置；不存在时为 null</param>
    /// <returns>是否找到配置</returns>
    public bool TryGet<TConfig>(out TConfig config) where TConfig : class, IConfig
    {
        config = null;

        return ModuleConfigurations is not null
            && ModuleConfigurations.TryGetValue(typeof(TConfig).Name, out var value)
            && (config = value as TConfig) is not null;
    }

    /// <summary>
    /// Get configuration parameters by name
    /// </summary>
    /// <returns>Configuration parameters</returns>
    public dynamic Get(string moduleConfigName)
    {
        if (ModuleConfigurations is null)
            return null;

        return ModuleConfigurations.TryGetValue(moduleConfigName, out var config) ? config : null;
    }
}

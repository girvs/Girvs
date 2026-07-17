namespace Girvs.Aspire.Hosting;

/// <summary>
/// 跨服务共享配置声明：非敏感项与敏感项分开收集，供 AddGirvsProject 注入到各服务。
/// 一处声明、全体生效；改配置需重新发布（不支持运行时热更新）。
/// </summary>
public class GirvsSharedConfiguration
{
    private IConfiguration _configuration = new ConfigurationBuilder().Build();
    private readonly List<(string Key, string Value)> _settings = new();
    private readonly List<(string Key, IResourceBuilder<ParameterResource> Parameter)> _secrets = new();

    /// <summary>非敏感共享配置（如日志等级）：publish 时作为普通环境变量/ConfigMap。</summary>
    public void AddSetting(string key, string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        _settings.Add((key, value));
    }

    /// <summary>敏感共享配置（如 JwtSecret）：以 secret 参数承载，publish 时落为 K8s Secret。</summary>
    public void AddSecret(string key, IResourceBuilder<ParameterResource> parameter)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(parameter);
        _secrets.Add((key, parameter));
    }

    public IReadOnlyList<(string Key, string Value)> Settings => _settings;
    public IReadOnlyList<(string Key, IResourceBuilder<ParameterResource> Parameter)> Secrets => _secrets;

    /// <summary>添加供所有 Girvs 服务继承的共享默认配置。</summary>
    public void AddConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _configuration = new ConfigurationBuilder()
            .AddConfiguration(_configuration)
            .AddConfiguration(configuration)
            .Build();
    }

    public IConfiguration Configuration => _configuration;
}

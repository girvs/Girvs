namespace Girvs.Refit.Extensions;

public static class IEngineExtensions
{
    /// <summary>
    /// 同步获取 Refit 客户端。走 Consul 服务发现时会同步等待网络调用，
    /// 高并发请求路径建议优先使用 RestServiceAsync
    /// </summary>
    public static T RestService<T>(this IEngine engine) =>
        engine.RestServiceAsync<T>().GetAwaiter().GetResult();

    /// <summary>
    /// 异步获取 Refit 客户端，Consul 服务发现全程异步，无线程阻塞
    /// </summary>
    public static async Task<T> RestServiceAsync<T>(this IEngine engine)
    {
        var attr = typeof(T).GetCustomAttribute(typeof(RefitServiceAttribute)) as RefitServiceAttribute;
        if (attr == null || string.IsNullOrEmpty(attr.ServiceName))
        {
            throw new GirvsException("请求配置错误");
        }

        var config = EngineContext.Current.GetAppModuleConfig<RefitConfig>();
        var requestUrl = attr.InConsul
            ? await LookupServiceAsync(attr.ServiceName)
            : config[attr.ServiceName];
        if (!attr.InConsul && !config.ServiceAddress.ContainsKey(attr.ServiceName))
        {
            throw new GirvsException($"未配置{attr.ServiceName}的请求地址");
        }

        if (string.IsNullOrEmpty(requestUrl))
        {
            throw new GirvsException($"未配置{attr.ServiceName}的请求地址");
        }

        return global::Refit.RestService.For<T>(requestUrl);
    }

    private static async Task<string> LookupServiceAsync(string serviceName)
    {
        var consulAddress = GetConsulAddress();
        // using 确保 ConsulClient 及时释放
        using var consulClient = new ConsulClient(configuration =>
        {
            configuration.Address = new Uri(consulAddress);
        });

        var servicesEntry = (await consulClient.Health.Service(serviceName, string.Empty, true)).Response;
        if (servicesEntry != null && servicesEntry.Any())
        {
            int index = new Random().Next(servicesEntry.Count());
            var entry = servicesEntry.ElementAt(index);
            return $"http://{entry.Service.Address}:{entry.Service.Port}";
        }

        return null;
    }

    private static string GetConsulAddress()
    {
        try
        {
            const string ConfigNodeName = "ConsulConfig";
            var config = Singleton<AppSettings>.Instance.Get(ConfigNodeName);
            return config?.ConsulAddress;
        }
        catch 
        {
            var _refitConfig = EngineContext.Current.GetAppModuleConfig<RefitConfig>();
            return _refitConfig.ConsulServiceHost;
        }
    }
}
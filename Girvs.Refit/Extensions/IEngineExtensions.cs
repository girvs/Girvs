namespace Girvs.Refit.Extensions;

public static class IEngineExtensions
{
    public static T RestService<T>(this IEngine engine)
    {
        var attr = typeof(T).GetCustomAttribute(typeof(RefitServiceAttribute)) as RefitServiceAttribute;
        if (attr == null || string.IsNullOrEmpty(attr.ServiceName))
        {
            throw new GirvsException("请求配置错误");
        }

        var config = EngineContext.Current.GetAppModuleConfig<RefitConfig>();
        var requestUrl = attr.InConsul ? LookupService(attr.ServiceName) : config[attr.ServiceName];
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
        
    private static string LookupService(string serviceName)
    {
        var consulAddress = GetConsulAddress();
        var consulClient = new ConsulClient(configuration =>
        {
            configuration.Address = new Uri(consulAddress);
        });

        var servicesEntry = consulClient.Health.Service(serviceName, string.Empty, true).Result.Response;
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
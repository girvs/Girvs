namespace Girvs.Refit.HttpClientHandlers;

public class AuthenticatedHttpClientHandler : DelegatingHandler
{
    private readonly RefitServiceAttribute _refitServiceAttribute;
    private readonly RefitConfig _refitConfig;
    private readonly ILogger<AuthenticatedHttpClientHandler> _logger;

    public AuthenticatedHttpClientHandler(RefitServiceAttribute refitServiceAttribute)
    {
        _refitConfig = EngineContext.Current.GetAppModuleConfig<RefitConfig>();
        _refitServiceAttribute =
            refitServiceAttribute ?? throw new ArgumentNullException(nameof(refitServiceAttribute));
        _logger = EngineContext.Current.Resolve<ILogger<AuthenticatedHttpClientHandler>>();
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var headers = EngineContext.Current.HttpContext?.Request.Headers.ToList();

        if (headers != null && headers.Any())
        {
            foreach (var (key, value) in headers)
            {
                request.Headers.TryAddWithoutValidation(key, value.ToString());
            }
        }

        var current = request.RequestUri;
        var serverUrl = _refitServiceAttribute.InConsul
            ? LookupService(_refitServiceAttribute.ServiceName)
            : _refitConfig[_refitServiceAttribute.ServiceName];

        _logger.LogInformation($"Girvs开始请求，请求ServerUrl地址为：{serverUrl}");
            
            
        if (serverUrl.IsNullOrEmpty()) throw new GirvsException("GirvsRefit请求地址不能为空！");
        string requestUriStr; 
        if (_refitServiceAttribute.InConsul)
        {
            requestUriStr = $"{current.Scheme}://{serverUrl}{current.PathAndQuery}";
        }
        else
        {
            requestUriStr = $"{serverUrl}{current.PathAndQuery}";
        }
            

        _logger.LogInformation($"Girvs开始请求，请求地址为：{requestUriStr}");
        request.RequestUri = new Uri(requestUriStr);
            
        return base.SendAsync(request, cancellationToken);
    }

    private string LookupService(string serviceName)
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
            return $"{entry.Service.Address}:{entry.Service.Port}";
        }

        return null;
    }

    private string GetConsulAddress()
    {
        try
        {
            const string ConfigNodeName = "ConsulConfig";
            var config = Singleton<AppSettings>.Instance.Get(ConfigNodeName);
            return config?.ConsulAddress;
        }
        catch 
        {
            return _refitConfig.ConsulServiceHost;
        }
    }
        
}
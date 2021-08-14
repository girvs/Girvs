using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Girvs.Infrastructure;
using Girvs.Refit.Configuration;
using NConsul;

namespace Girvs.Refit.HttpClientHandlers
{
    public class AuthenticatedHttpClientHandler : DelegatingHandler
    {
        private readonly RefitServiceAttribute _refitServiceAttribute;
        private readonly RefitConfig _refitConfig;

        public AuthenticatedHttpClientHandler(RefitServiceAttribute refitServiceAttribute)
        {
            _refitConfig = EngineContext.Current.GetAppModuleConfig<RefitConfig>();
            _refitServiceAttribute =
                refitServiceAttribute ?? throw new ArgumentNullException(nameof(refitServiceAttribute));
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var headers = EngineContext.Current.HttpContext.Request.Headers.ToList();

            if (headers.Any())
            {
                foreach (var (key, value) in headers)
                {
                    request.Headers.Add(key, value.ToString());
                }
            }

            var current = request.RequestUri;
            var serverUrl = string.Empty;
            if (_refitServiceAttribute.InConsul)
            {
                serverUrl = LookupService(_refitServiceAttribute.ServiceName);
            }
            else
            {
                serverUrl = _refitConfig[_refitServiceAttribute.ServiceName];
            }
            
            request.RequestUri = new Uri($"{current.Scheme}://{serverUrl}{current.PathAndQuery}");
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        
        private string LookupService(string serviceName)
        {
            var consulClient = new ConsulClient(configuration =>
            {
                configuration.Address = new Uri(_refitConfig.ConsulServiceHost);
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

        // public async Task<List<AgentService>> GetAgentServices()
        // {
        //     var consulClient = new ConsulClient(configuration =>
        //     {
        //         configuration.Address = new Uri(_refitConfig.ConsulServiceHost);
        //     });
        //     
        //     var services = await consulClient.Agent.Services();
        //
        //     List<AgentService> result = new List<AgentService>();
        //     foreach (AgentService responseValue in services.Response.Values)
        //     {
        //         if (result.All(x => x.Service != responseValue.Service))
        //         {
        //             result.Add(responseValue);
        //         }
        //     }
        //
        //     return result;
        // }
    }
}
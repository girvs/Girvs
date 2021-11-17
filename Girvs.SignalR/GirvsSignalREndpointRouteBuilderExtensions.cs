using Girvs.Infrastructure;
using Girvs.TypeFinder;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;

namespace Girvs.SignalR
{
    public static class GirvsSignalREndpointRouteBuilderExtensions
    {
        public static void AutoMapSignalREndpointRouteBuilder(this IEndpointRouteBuilder builder)
        {
            var typeFinder = EngineContext.Current.Resolve<ITypeFinder>();
            var signalRServices = typeFinder.FindOfType<Hub>();
            foreach (var signalRService in signalRServices)
            {
                if (signalRService.IsAbstract) continue;
                var method = typeof(IEndpointRouteBuilder).GetMethod(nameof(HubEndpointRouteBuilderExtensions.MapHub))
                    ?.MakeGenericMethod(signalRService);
                if (method != null) method.Invoke(null, new object[] {builder, signalRService.Name});
            }
        }
    }
}
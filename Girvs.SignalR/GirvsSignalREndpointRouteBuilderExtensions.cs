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
                var method = typeof(GirvsSignalREndpointRouteBuilderExtensions).GetMethod("GirvsMapHub")
                    ?.MakeGenericMethod(signalRService);
                if (method != null) method.Invoke(null, new object[] { builder});
            }
        }

        public static void GirvsMapHub<THub>(this IEndpointRouteBuilder builder) where THub : Hub
        {
            builder.MapHub<THub>(typeof(THub).Name);
        }
    }
}
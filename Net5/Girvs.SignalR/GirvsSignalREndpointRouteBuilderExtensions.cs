using Girvs.Infrastructure;
using Girvs.TypeFinder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;

namespace Girvs.WebFrameWork.Plugins.SignalR
{
    public static class GirvsSignalREndpointRouteBuilderExtensions
    {
        public static void AutoMapSignalREndpointRouteBuilder(this IEndpointRouteBuilder builder)
        {
            var typeFinder = EngineContext.Current.Resolve<ITypeFinder>();
            var signalRServices = typeFinder.FindClassesOfType<Hub>();
            foreach (var signalRService in signalRServices)
            {
                if (signalRService.IsAbstract) continue;
                var method = typeof(IEndpointRouteBuilder).GetMethod("MapHub")?.MakeGenericMethod(signalRService);
                if (method != null) method.Invoke(signalRService.Name, new object[] {builder});
            }
        }
    }
}
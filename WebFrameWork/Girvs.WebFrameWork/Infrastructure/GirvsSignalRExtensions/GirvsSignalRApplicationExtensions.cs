using Girvs.Domain.Infrastructure;
using Girvs.Domain.TypeFinder;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;

namespace Girvs.WebFrameWork.Infrastructure.SpSignalRExtensions
{
    public static class GirvsSignalRApplicationExtensions
    {
        public static void UseAutoSignalRServices(this IApplicationBuilder app)
        {
            app.UseEndpoints(AddSignalRServices);
        }

        private static void AddSignalRServices(IEndpointRouteBuilder builder)
        {
            var typeFinder = EngineContext.Current.Resolve<ITypeFinder>();
            var signalRServices = typeFinder.FindClassesOfType<Hub>();
            foreach (var signalRService in signalRServices)
            {
                if (signalRService.IsAbstract) continue;
                var method = typeof(IEndpointRouteBuilder).GetMethod("MapHub")?.MakeGenericMethod(signalRService);
                if (method != null) method.Invoke(signalRService.Name, new object[] {builder});
            }

            ;
            // signalRServices = typeFinder.FindClassesOfType<Hub>();
            // foreach (var signalRService in signalRServices)
            // {
            //     var method = typeof(HubRouteBuilder).GetMethod("MapHub")?.MakeGenericMethod(signalRService);
            //     if (method != null) method.Invoke(null, new[] { builder });
            // };
        }
    }
}
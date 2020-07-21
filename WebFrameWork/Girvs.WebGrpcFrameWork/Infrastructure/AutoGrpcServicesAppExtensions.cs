using Girvs.Application;
using Girvs.Domain.Infrastructure;
using Girvs.Domain.TypeFinder;
using Girvs.Infrastructure.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Girvs.WebGrpcFrameWork.Infrastructure
{
    public static class AutoGrpcServicesAppExtensions
    {
        /// <summary>
        /// 自动映射Grpc服务，服务必须继承IGrpcServiceBase接口 
        /// </summary>
        /// <param name="app"></param>
        public static void UseAutoGrpcServices(this IApplicationBuilder app)
        {
            app.UseEndpoints(AddGrpcServices);
        }


        public static void AddGrpcServices(this IEndpointRouteBuilder builder)
        {
            var typeFinder = EngineContext.Current.Resolve<ITypeFinder>();
            var grpcServices = typeFinder.FindClassesOfType<IService>();
            foreach (var grpcService in grpcServices)
            {
                var method = typeof(GrpcEndpointRouteBuilderExtensions).GetMethod("MapGrpcService")?.MakeGenericMethod(grpcService);
                if (method != null) method.Invoke(null, new object[] { builder });
            };
        }
    }
}
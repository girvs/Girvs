using Girvs.Application;
using Girvs.Domain.Infrastructure;
using Girvs.Domain.TypeFinder;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

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
            var grpcServices = typeFinder.FindClassesOfType<IGrpcService>();

            var logger = EngineContext.Current.Resolve<ILogger<object>>();
            foreach (var grpcService in grpcServices)
            {
                logger.LogInformation($"注册GRPC服务：{grpcService.FullName}");
                var method = typeof(GrpcEndpointRouteBuilderExtensions)
                    .GetMethod(nameof(GrpcEndpointRouteBuilderExtensions.MapGrpcService))
                    ?.MakeGenericMethod(grpcService);
                if (method != null) method.Invoke(null, new object[] {builder});
            }
            
            
            builder.MapGet("/", async context =>
            {
                await context.Response.WriteAsync("服务已启动！");
            });
        }
    }
}
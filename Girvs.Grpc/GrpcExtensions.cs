using Girvs.Infrastructure;
using Girvs.TypeFinder;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;

namespace Girvs.Grpc
{
    public static class GrpcExtensions
    {
        public static void AddEndpointRouteBuilderGrpcServices(this IEndpointRouteBuilder builder)
        {
            var typeFinder = EngineContext.Current.Resolve<ITypeFinder>();
            var grpcServices = typeFinder.FindOfType<IAppGrpcService>();

            var logger = EngineContext.Current.Resolve<ILogger<object>>();
            foreach (var grpcService in grpcServices)
            {
                logger.LogInformation($"注册GRPC服务：{grpcService.FullName}");
                var method = typeof(GrpcEndpointRouteBuilderExtensions)
                    .GetMethod(nameof(GrpcEndpointRouteBuilderExtensions.MapGrpcService))
                    ?.MakeGenericMethod(grpcService);
                if (method != null) method.Invoke( null, new object[] { builder });
            }


            builder.MapGet("/", async context =>
            {
                await context.Response.WriteAsync("服务已启动！");
            });
        }
    }
}
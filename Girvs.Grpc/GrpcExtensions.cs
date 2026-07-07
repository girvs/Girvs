namespace Girvs.Grpc;

public static class GrpcExtensions
{
    public static void AddEndpointRouteBuilderGrpcServices(this IEndpointRouteBuilder builder)
    {
        var typeFinder = EngineContext.Current.Resolve<ITypeFinder>();
        var grpcServices = typeFinder.FindOfType<IAppGrpcService>();
        var mapGrpcServiceMethod = GetMapGrpcServiceMethod();

        var logger = EngineContext.Current.Resolve<ILogger<object>>();
        foreach (var grpcService in grpcServices)
        {
            logger.LogInformation($"开始注册GRPC服务：{grpcService.FullName}");
            var method = mapGrpcServiceMethod
                ?.MakeGenericMethod(grpcService);
            if (method != null)
            {
                var invokeResult = method.Invoke(null, [builder]) as IEndpointConventionBuilder;
                
                logger.LogInformation($"成功注册GRPC服务：{grpcService.FullName}");
                
                if (invokeResult != null)
                {
                    logger.LogInformation($"启用GrpcWeb服务：{grpcService.FullName}");
                    invokeResult.EnableGrpcWeb();
                }
                else
                {
                    logger.LogInformation($"启用GrpcWeb服务失败：{grpcService.FullName}");
                }
            }
            
            logger.LogInformation($"结束注册GRPC服务：{grpcService.FullName}");
        }


        builder.MapGet("/", async context => { await context.Response.WriteAsync("服务已启动！"); });
    }

    private static System.Reflection.MethodInfo GetMapGrpcServiceMethod()
    {
        return typeof(GrpcEndpointRouteBuilderExtensions)
            .GetMethods()
            .SingleOrDefault(method =>
                method.Name == nameof(GrpcEndpointRouteBuilderExtensions.MapGrpcService) &&
                method.IsGenericMethodDefinition &&
                method.GetGenericArguments().Length == 1 &&
                method.GetParameters() is [{ ParameterType: var parameterType }] &&
                parameterType == typeof(IEndpointRouteBuilder));
    }
}

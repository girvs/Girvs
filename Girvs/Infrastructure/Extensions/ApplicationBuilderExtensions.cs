namespace Girvs.Infrastructure.Extensions;

public static class ApplicationBuilderExtensions
{
    // 复用同一份选项，让 JsonSerializer 的元数据缓存得以生效；
    // 原实现每次异常都 new JsonSerializerOptions，等于每次重建序列化元数据。
    private static readonly JsonSerializerOptions ExceptionJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static void ConfigureRequestPipeline(
        this IApplicationBuilder application,
        IWebHostEnvironment env
    )
    {
        EngineContext.Current.ConfigureRequestPipeline(application, env);
    }

    /// <summary>
    /// 配置应用程序HTTP请求管道
    /// </summary>
    /// <param name="application">Builder for configuring an application's request pipeline</param>
    /// <param name="endpointRouteBuilder"></param>
    public static void ConfigureEndpointRouteBuilder(
        this IEndpointRouteBuilder endpointRouteBuilder
    )
    {
        EngineContext.Current.ConfigureEndpointRouteBuilder(endpointRouteBuilder);
    }

    public static void UseGirvsEndpoints(
        this IApplicationBuilder application,
        Action<IEndpointRouteBuilder> configure
    )
    {
        application.UseEndpoints(endpoints =>
        {
            configure(endpoints);
            EngineContext.Current.ConfigureEndpointRouteBuilder(endpoints);
        });
    }

    /// <summary>
    /// 添加一个特殊的处理程序，该处理程序检查状态码为400（错误请求）的响应
    /// </summary>
    public static void UseBadRequestResult(this IApplicationBuilder application)
    {
        application.UseStatusCodePages(context =>
        {
            //handle 404 (Bad request)
            if (context.HttpContext.Response.StatusCode == StatusCodes.Status400BadRequest)
            {
                var logger = EngineContext.Current.Resolve<ILogger>();
                logger.LogError("Error 400. Bad request");
            }

            return Task.CompletedTask;
        });
    }

    /// <summary>
    /// 配置中间件以动态压缩HTTP响应
    /// </summary>
    public static void UseGirvsResponseCompression(this IApplicationBuilder application)
    {
        //whether to use compression (gzip by default)
        application.UseResponseCompression();
    }

    /// <summary>
    /// 添加了身份验证中间件，该中间件启用了身份验证功能。
    /// </summary>
    public static void UseGirvsAuthentication(this IApplicationBuilder application)
    {
        application.UseMiddleware<AuthenticationMiddleware>();
    }

    /// <summary>
    /// 添加异常处理
    /// </summary>
    public static void UseGirvsExceptionHandler(this IApplicationBuilder application)
    {
        application.UseExceptionHandler(handler =>
        {
            handler.Run(async context =>
            {
                var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
                if (exception == null)
                    return;

                var logger =
                    application.ApplicationServices.GetService(typeof(ILogger<object>))
                    as ILogger<object>;
                logger?.LogError(exception, exception.Message);

                var hostingEnvironment = EngineContext.Current.Resolve<IWebHostEnvironment>();
                var useDetailedExceptionPage =
                    Singleton<AppSettings>.Instance.CommonConfig.DisplayFullErrorStack
                    || hostingEnvironment.IsDevelopment();

                var result = new ExceptionResult();

                if (exception is GirvsException girvsException)
                {
                    context.Response.StatusCode = girvsException.StatusCode;
                    result.Errors = girvsException.Error;
                }
                else
                {
                    // 非业务异常统一返回 500：否则默认状态码 200 会在下面查
                    // ClientErrorMapping 时抛 KeyNotFoundException，异常处理器自身二次失败。
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                }

                result.Errors ??= exception.Message;

                if (context.Response.StatusCode == 568)
                {
                    result.Title = "系统预置错误";
                    result.Link = "https://github.com/girvs/Girvs";
                }
                else
                {
                    var apiBehaviorOptions = EngineContext.Current.Resolve<
                        IOptions<ApiBehaviorOptions>
                    >();

                    // ClientErrorMapping 只覆盖 4xx 与 500，其余状态码使用兜底文案。
                    if (
                        apiBehaviorOptions?.Value.ClientErrorMapping.TryGetValue(
                            context.Response.StatusCode,
                            out var clientError
                        ) == true
                    )
                    {
                        result.Link = clientError.Link;
                        result.Title = clientError.Title;
                    }
                    else
                    {
                        result.Link = "https://github.com/girvs/Girvs";
                        result.Title = "服务器内部错误";
                    }
                }

                result.Status = context.Response.StatusCode;
                result.TraceId = context.TraceIdentifier;
                result.StackTrace = useDetailedExceptionPage ? exception.StackTrace : string.Empty;

                var resultContext = JsonSerializer.Serialize(result, ExceptionJsonOptions);

                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(resultContext, Encoding.UTF8);
            });
        });
    }
}

public class ExceptionResult
{
    public string Title { get; set; }
    public string Link { get; set; }
    public dynamic Errors { get; set; }
    public string TraceId { get; set; }
    public int Status { get; set; }
    public string StackTrace { get; set; }
}

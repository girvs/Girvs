using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Girvs.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Girvs.Infrastructure.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static void ConfigureRequestPipeline(this IApplicationBuilder application)
        {
            EngineContext.Current.ConfigureRequestPipeline(application);
        }
        
        /// <summary>
        /// 配置应用程序HTTP请求管道
        /// </summary>
        /// <param name="application">Builder for configuring an application's request pipeline</param>
        public static void ConfigureEndpointRouteBuilder(this IEndpointRouteBuilder endpointRouteBuilder)
        {
            EngineContext.Current.ConfigureEndpointRouteBuilder(endpointRouteBuilder);
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
                    logger.LogError("Error 400. Bad request", null);
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

                    if (exception is GirvsException girvsException)
                    {
                        context.Response.StatusCode = girvsException.StatusCode;
                    }

                    var logger = application.ApplicationServices.GetService(typeof(ILogger<object>)) as ILogger<object>;
                    logger.LogError(exception, exception.Message);
                    var appSettings = EngineContext.Current.Resolve<AppSettings>();
                    var hostingEnvironment = EngineContext.Current.Resolve<IWebHostEnvironment>();
                    var useDetailedExceptionPage = appSettings.CommonConfig.DisplayFullErrorStack || hostingEnvironment.IsDevelopment();
                    dynamic result = new
                    {
                        type = "http://tools.ietf.org/html/rfc2774#section-7",
                        message = exception.Message,
                        status = context.Response.StatusCode,
                        traceId = context.TraceIdentifier,
                        stackTrace = useDetailedExceptionPage ? exception.StackTrace : string.Empty
                    };

                    string resultContext = JsonSerializer.Serialize(result);
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(resultContext, Encoding.UTF8);
                });
            });
        }
    }
}
using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Girvs.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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


        public static void UseGirvsEndpoints(this IApplicationBuilder application,
            Action<IEndpointRouteBuilder> configure)
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

                    var logger = application.ApplicationServices.GetService(typeof(ILogger<object>)) as ILogger<object>;
                    logger.LogError(exception, exception.Message);

                    var hostingEnvironment = EngineContext.Current.Resolve<IWebHostEnvironment>();
                    var useDetailedExceptionPage = Singleton<AppSettings>.Instance.CommonConfig.DisplayFullErrorStack ||
                                                   hostingEnvironment.IsDevelopment();

                    var result = new ExceptionResult();

                    if (exception is GirvsException girvsException)
                    {
                        context.Response.StatusCode = girvsException.StatusCode;
                        result.Errors = girvsException.Error;
                    }

                    result.Errors ??= exception.Message;
                    
                    if (context.Response.StatusCode == 568)
                    {
                        result.Title = "系统预置错误";
                        result.Link = "https://github.com/girvs/Girvs";
                    }
                    else
                    {
                        var apiBehaviorOptions =  EngineContext.Current.Resolve<IOptions<ApiBehaviorOptions>>();
                        var clientError = apiBehaviorOptions.Value.ClientErrorMapping[context.Response.StatusCode];
                        result.Link = clientError.Link;
                        result.Title = clientError.Title;
                    }

                    result.Status = context.Response.StatusCode;
                    result.TraceId = context.TraceIdentifier;
                    result.StackTrace = useDetailedExceptionPage ? exception.StackTrace : string.Empty;

                    var resultContext = JsonSerializer.Serialize(result, new JsonSerializerOptions()
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

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
}
using System.Text;
using System.Text.Json;
using Girvs.Domain;
using Girvs.Domain.Configuration;
using Girvs.Domain.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Girvs.WebApiFrameWork.Infrastructure
{
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// 添加异常处理
        /// </summary>
        public static void UseSpExceptionHandler(this IApplicationBuilder application)
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

                    EngineContext.Current.Resolve<ILogger<object>>().LogError(exception.Message, exception);
                    var spConfig = EngineContext.Current.Resolve<GirvsConfig>();
                    var hostingEnvironment = EngineContext.Current.Resolve<IWebHostEnvironment>();
                    var useDetailedExceptionPage =
                        spConfig.DisplayFullErrorStack || hostingEnvironment.IsDevelopment();
                    dynamic result = new
                    {
                        type = "http://tools.ietf.org/html/rfc2774#section-7",
                        title = exception.Message,
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
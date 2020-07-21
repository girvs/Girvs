using System.Text;
using System.Text.Json;
using Girvs.Domain;
using Girvs.Domain.Configuration;
using Girvs.Domain.Infrastructure;
using Girvs.Infrastructure.Infrastructure;
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

                    context.Response.StatusCode = exception is GirvsBusinessException ? 568 : StatusCodes.Status500InternalServerError;

                    try
                    {
                        EngineContext.Current.Resolve<ILogger<object>>().LogError(exception.Message, exception);
                    }
                    finally
                    {
                        var girvsConfig = EngineContext.Current.Resolve<GirvsConfig>();
                        var hostingEnvironment = EngineContext.Current.Resolve<IWebHostEnvironment>();
                        var useDetailedExceptionPage = girvsConfig.DisplayFullErrorStack || hostingEnvironment.IsDevelopment();
                        dynamic result = new
                        {
                            Type = "http://tools.ietf.org/html/rfc2774#section-7",
                            Title = exception.Message,
                            Status = context.Response.StatusCode,
                            TraceId = context.TraceIdentifier,
                            StackTrace = useDetailedExceptionPage ? exception.StackTrace : string.Empty
                        };

                        string resultContext = JsonSerializer.Serialize(result);
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(resultContext, Encoding.UTF8);
                    }
                });
            });
        }
    }
}
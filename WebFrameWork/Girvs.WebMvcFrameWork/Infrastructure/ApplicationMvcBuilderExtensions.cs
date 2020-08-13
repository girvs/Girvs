using Girvs.Domain;
using Girvs.Domain.Configuration;
using Girvs.Domain.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Girvs.Infrastructure.Infrastructure;

namespace Girvs.WebMvcFrameWork.Infrastructure
{
    /// <summary>
    /// 表示IApplicationBuilder的扩展
    /// </summary>
    public static class ApplicationMvcBuilderExtensions
    {
        /// <summary>
        /// 添加异常处理
        /// </summary>
        public static void UseSpMvcExceptionHandler(this IApplicationBuilder application)
        {
            var girvsConfig = EngineContext.Current.Resolve<GirvsConfig>();
            var hostingEnvironment = EngineContext.Current.Resolve<IWebHostEnvironment>();
            var useDetailedExceptionPage = girvsConfig.DisplayFullErrorStack || hostingEnvironment.IsDevelopment();
            if (useDetailedExceptionPage)
            {
                application.UseDeveloperExceptionPage();
            }
            else
            {
                application.UseExceptionHandler(handler =>
                {
                    handler.Run(async context =>
                    {
                        await Task.Run(() =>
                        {
                            var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
                            if (exception != null)
                            {
                                context.Response.StatusCode = exception is GirvsException
                                    ? 568
                                    : StatusCodes.Status500InternalServerError;

                                try
                                {
                                    EngineContext.Current.Resolve<ILogger<object>>()
                                        .LogError(exception.Message, exception);
                                }
                                finally
                                {

                                    //dynamic result = new
                                    //{
                                    //    Type = "http://tools.ietf.org/html/rfc2774#section-7",
                                    //    Title = exception.Message,
                                    //    Status = context.Response.StatusCode,
                                    //    TraceId = context.TraceIdentifier,
                                    //    StackTrace = useDetailedExceptionPage ? exception.StackTrace : string.Empty
                                    //};
                                    context.Response.Redirect("/Home/Error?");
                                }
                            }
                        });
                    });
                });
            }

        }
    }
}
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Girvs.Domain;
using Girvs.Domain.Configuration;
using Girvs.Domain.FileProvider;
using Girvs.Domain.Infrastructure;
using Girvs.Infrastructure.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Girvs.WebFrameWork.Plugins
{
    /// <summary>
    /// 表示IApplicationBuilder的扩展
    /// </summary>
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// 配置应用程序HTTP请求管道
        /// </summary>
        /// <param name="application">Builder for configuring an application's request pipeline</param>
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
        /// 添加一个特殊的处理程序，该处理程序检查没有正文的404状态代码的响应
        /// </summary>
        public static void UsePageNotFound(this IApplicationBuilder application)
        {
            application.UseStatusCodePages(async context =>
            {
                if (context.HttpContext.Response.StatusCode == StatusCodes.Status404NotFound)
                {
                    var webHelper = EngineContext.Current.Resolve<IWebHelper>();
                    if (!webHelper.IsStaticResource())
                    {
                        var originalPath = context.HttpContext.Request.Path;
                        var originalQueryString = context.HttpContext.Request.QueryString;

                        context.HttpContext.Features.Set<IStatusCodeReExecuteFeature>(new StatusCodeReExecuteFeature
                        {
                            OriginalPathBase = context.HttpContext.Request.PathBase.Value,
                            OriginalPath = originalPath.Value,
                            OriginalQueryString = originalQueryString.HasValue ? originalQueryString.Value : null
                        });

                        //get new path
                        context.HttpContext.Request.Path = "/page-not-found";
                        context.HttpContext.Request.QueryString = QueryString.Empty;

                        try
                        {
                            //re-execute request with new path
                            await context.Next(context.HttpContext);
                        }
                        finally
                        {
                            //return original path to request
                            context.HttpContext.Request.QueryString = originalQueryString;
                            context.HttpContext.Request.Path = originalPath;
                            context.HttpContext.Features.Set<IStatusCodeReExecuteFeature>(null);
                        }
                    }
                }
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
        /// 配置静态文件服务
        /// </summary>
        public static void UseGirvsStaticFiles(this IApplicationBuilder application)
        {
            void staticFileResponse(StaticFileResponseContext context)
            {
                var commonSettings = EngineContext.Current.Resolve<CommonSettings>();
                if (!string.IsNullOrEmpty(commonSettings.StaticFilesCacheControl))
                    context.Context.Response.Headers.Append(HeaderNames.CacheControl, commonSettings.StaticFilesCacheControl);
            }

            var fileProvider = EngineContext.Current.Resolve<IGirvsFileProvider>();

            //common static files
            application.UseStaticFiles(new StaticFileOptions { OnPrepareResponse = staticFileResponse });

            //themes static files
            application.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(fileProvider.MapPath(@"Themes")),
                RequestPath = new PathString("/Themes"),
                OnPrepareResponse = staticFileResponse
            });

            //plugins static files
            var staticFileOptions = new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(fileProvider.MapPath(@"Plugins")),
                RequestPath = new PathString("/Plugins"),
                OnPrepareResponse = staticFileResponse
            };
            var securitySettings = EngineContext.Current.Resolve<SecuritySettings>();
            if (!string.IsNullOrEmpty(securitySettings.PluginStaticFileExtensionsBlacklist))
            {
                var fileExtensionContentTypeProvider = new FileExtensionContentTypeProvider();

                foreach (var ext in securitySettings.PluginStaticFileExtensionsBlacklist
                    .Split(';', ',')
                    .Select(e => e.Trim().ToLower())
                    .Select(e => $"{(e.StartsWith(".") ? string.Empty : ".")}{e}")
                    .Where(fileExtensionContentTypeProvider.Mappings.ContainsKey))
                {
                    fileExtensionContentTypeProvider.Mappings.Remove(ext);
                }

                staticFileOptions.ContentTypeProvider = fileExtensionContentTypeProvider;
            }

            application.UseStaticFiles(staticFileOptions);

            //add support for backups
            var provider = new FileExtensionContentTypeProvider
            {
                Mappings = { [".bak"] = MimeTypes.ApplicationOctetStream }
            };

            application.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(fileProvider.GetAbsolutePath("db_backups")),
                RequestPath = new PathString("/db_backups"),
                ContentTypeProvider = provider
            });

            //add support for webmanifest files
            provider.Mappings[".webmanifest"] = MimeTypes.ApplicationManifestJson;

            application.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(fileProvider.GetAbsolutePath("icons")),
                RequestPath = "/icons",
                ContentTypeProvider = provider
            });
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
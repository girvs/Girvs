using Girvs.Domain.Infrastructure;
using Girvs.Infrastructure.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using WebMarkupMin.AspNetCore2;
using WebMarkupMin.NUglify;

namespace Girvs.Service.FrameWork.Plugins.WebMarkupMin
{
    public static class WebMarkupMinServiceExtensions
    {
        /// <summary>
        /// 添加和配置WebMarkupMin服务
        /// </summary>
        public static IServiceCollection AddSpWebMarkupMin(this IServiceCollection services)
        {
            services
                .AddWebMarkupMin(options =>
                {
                    options.AllowMinificationInDevelopmentEnvironment = true;
                    options.AllowCompressionInDevelopmentEnvironment = true;
                    options.DisableMinification = !EngineContext.Current.Resolve<CommonSettings>().EnableHtmlMinification;
                    options.DisableCompression = true;
                    options.DisablePoweredByHttpHeaders = true;
                })
                .AddHtmlMinification(options =>
                {
                    var settings = options.MinificationSettings;

                    options.CssMinifierFactory = new NUglifyCssMinifierFactory();
                    options.JsMinifierFactory = new NUglifyJsMinifierFactory();
                })
                .AddXmlMinification(options =>
                {
                    var settings = options.MinificationSettings;
                    settings.RenderEmptyTagsWithSpace = true;
                    settings.CollapseTagsWithoutContent = true;
                });
            return services;
        }
    }
}
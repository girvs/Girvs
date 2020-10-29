using Girvs.Domain.Http;
using Girvs.Domain.Infrastructure;
using Girvs.Infrastructure.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.Service.FrameWork.Plugins.AntiForgery
{

    /// <summary>
    /// 添加防伪支持所需的服务
    /// </summary>
    public static class AntiForgeryServiceExtensions
    {
        public static void AddGrivsAntiforgery(this IServiceCollection services)
        {
            services.AddAntiforgery(options =>
            {
                options.Cookie.Name = $"{GirvsCookieDefaults.Prefix}{GirvsCookieDefaults.AntiforgeryCookie}";
                options.Cookie.SecurePolicy = EngineContext.Current.Resolve<SecuritySettings>().ForceSslForAllPages
                    ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.None;
            });
        }
    }
}
using Girvs.Domain.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.WebFrameWork.Infrastructure.HttpSessionExtensions
{
    public static class HttpSessionServiceExtensions
    {
        /// <summary>
        /// 添加应用程序会话状态所需的服务
        /// </summary>
        public static IServiceCollection AddHttpSession(this IServiceCollection services)
        {
            services.AddSession(options =>
            {
                options.Cookie.Name = $"{GirvsCookieDefaults.Prefix}{GirvsCookieDefaults.SessionCookie}";
                options.Cookie.HttpOnly = true;

                //是否允许使用其他商店页面上不受SSL保护的页面上的会话值
                //options.Cookie.SecurePolicy = EngineContext.Current.Resolve<SecuritySettings>().ForceSslForAllPages ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.None;
            });
            return services;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP;
using Girvs.AuthorizePermission.Services;
using Girvs.EventBus;
using Girvs.EventBus.Extensions;
using Girvs.Infrastructure;
using Microsoft.AspNetCore.Builder;
using ZhuoFan.Wb.Common.Events.Authorize;

namespace ZhuoFan.Wb.Common
{
    public static class ApplicationStart
    {
        public static void InitAuthorizeData(this IApplicationBuilder application)
        {
            Task.Run(() =>
            {
                //1分钟后发个事件消息
                Thread.Sleep(60 * 1000);
                IEventBus eventBus = EngineContext.Current.Resolve<IEventBus>();
                if (eventBus == null) return;

                IGirvsAuthorizePermissionService permissionService = new GirvsAuthorizePermissionService();
                var authorizeEvent = new AuthorizeEvent()
                {
                    AuthorizePermissions = permissionService.GetAuthorizePermissionList().Result,
                    AuthorizeDataRules = permissionService.GetAuthorizeDataRuleList().Result
                };

                //由于此处是在程序启动时就会发送此消息，所以没有请求头等，自动构建消息。
                var customHeader = new Dictionary<string, string>
                {
                    { ClaimTypes.Sid, "58205e0e-1552-4282-bedc-a92d0afb37df" },
                    { ClaimTypes.GroupSid, Guid.Empty.ToString() },
                    { ClaimTypes.Name, "系统管理员" },
                    { ClaimTypes.GivenName, "系统管理员" }
                };

                // Task.Run()

                //需要重新设置身份认证头
                EngineContext.Current.ClaimManager.CapEventBusReSetClaim(new CapHeader(customHeader));
                eventBus.PublishAsync(authorizeEvent).Wait();
            });
        }
    }
}
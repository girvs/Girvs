using System;
using System.Reflection;
using Girvs.AuthorizePermission.Configuration;
using Girvs.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Girvs.AuthorizePermission.AuthorizeCompare
{
    public class ActionPermissionFilter : ActionFilterAttribute
    {
        private readonly ILogger<ActionPermissionFilter> _logger;
        private readonly AuthorizeConfig _authorizeConfig;

        public ActionPermissionFilter(ILogger<ActionPermissionFilter> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _authorizeConfig = EngineContext.Current.GetAppModuleConfig<AuthorizeConfig>();
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!_authorizeConfig.UseServiceMethodPermissionCompare)
            {
                base.OnActionExecuting(context);
                return;
            }

            var ad = context.ActionDescriptor as ControllerActionDescriptor;

            if (ad.ControllerName.Contains("HealthController"))
            {
                base.OnActionExecuting(context);
                return;
            }

            var service = context.Controller;
            var spd =
                service.GetType().GetCustomAttribute(typeof(ServicePermissionDescriptorAttribute)) as
                    ServicePermissionDescriptorAttribute;

            if (spd == null)
            {
                base.OnActionExecuting(context);
                return;
            }

            var isActionMethodPermission =
                ad.MethodInfo.IsDefined(typeof(ServiceMethodPermissionDescriptorAttribute), false);

            if (!isActionMethodPermission)
            {
                base.OnActionExecuting(context);
                return;
            }

            var swamped =
                ad.MethodInfo.GetCustomAttribute(typeof(ServiceMethodPermissionDescriptorAttribute), false) as
                    ServiceMethodPermissionDescriptorAttribute;

            _logger.LogInformation($"ServiceName:{spd.ServiceName}  ActionMethodName:{swamped.MethodName}");

            var serviceMethodPermissionCompare = EngineContext.Current.Resolve<IServiceMethodPermissionCompare>();
            if (serviceMethodPermissionCompare != null)
            {
                var result = serviceMethodPermissionCompare.PermissionCompare(spd.ServiceId, swamped.Permission);
                if (!result)
                {
                    throw new GirvsException($"当前没有‘{spd.ServiceName}’的‘{swamped.MethodName}’权限",
                        StatusCodes.Status403Forbidden);
                }
            }
            else
            {
                _logger.LogWarning("当前服务没有实现接口权限认证，需要实现接口：IServiceMethodPermissionCompare");
            }

            base.OnActionExecuting(context);
        }
    }
}
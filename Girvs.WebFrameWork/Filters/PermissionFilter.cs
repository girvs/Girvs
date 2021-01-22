using System;
using System.Reflection;
using Girvs.Application.Attributes;
using Girvs.Domain;
using Girvs.Domain.Infrastructure;
using Girvs.Domain.Managers;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace Girvs.WebFrameWork.Filters
{
    public class PermissionFilter : ActionFilterAttribute
    {
        private readonly ILogger<PermissionFilter> _logger;

        public PermissionFilter(ILogger<PermissionFilter> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            ControllerActionDescriptor ad = context.ActionDescriptor as ControllerActionDescriptor;

            if (ad.ControllerName.Contains("HealthController"))
            {
                base.OnActionExecuting(context);
                return;
            }

            var service = context.Controller;
            var spd = service.GetType().GetCustomAttribute(typeof(ServicePermissionDescriptorAttribute)) as ServicePermissionDescriptorAttribute;

            if (spd == null)
            {
                base.OnActionExecuting(context);
                return;
            }

            var isActionMethodPermission = ad.MethodInfo.IsDefined(typeof(ServiceMethodPermissionDescriptorAttribute), false);

            if (!isActionMethodPermission)
            {
                base.OnActionExecuting(context);
                return;
            }

            var smpd = ad.MethodInfo.GetCustomAttribute(typeof(ServiceMethodPermissionDescriptorAttribute), false) as ServiceMethodPermissionDescriptorAttribute;

            _logger.LogError($"ServiceName:{spd.ServiceName}  ActionMethodName:{smpd.MethodName}");

            var serviceMethodPermissionCompare = EngineContext.Current.Resolve<IServiceMethodPermissionCompare>();
            if (serviceMethodPermissionCompare != null)
            {
                var result = serviceMethodPermissionCompare.PermissionCompare(spd.ServiceId, smpd.Permission).Result;
                if (!result)
                {
                    throw new GirvsException($"当前没有‘{spd.ServiceName}’的‘{smpd.MethodName}’权限", 568);
                }
            }

            base.OnActionExecuting(context);
        }
    }
}
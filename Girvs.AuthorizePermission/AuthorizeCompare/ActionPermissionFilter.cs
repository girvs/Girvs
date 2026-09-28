namespace Girvs.AuthorizePermission.AuthorizeCompare;

public class ActionPermissionFilter(ILogger<ActionPermissionFilter> logger) : ActionFilterAttribute
{
    private readonly AuthorizeConfig authorizeConfig = EngineContext.Current.GetAppModuleConfig<AuthorizeConfig>();

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!authorizeConfig.UseServiceMethodPermissionCompare)
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

        if (service.GetType().GetCustomAttribute(typeof(ServicePermissionDescriptorAttribute)) is not ServicePermissionDescriptorAttribute spd)
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

        logger.LogInformation($"ServiceName:{spd.ServiceName}  ActionMethodName:{swamped.MethodName}");

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
            logger.LogWarning("当前服务没有实现接口权限认证，需要实现接口：IServiceMethodPermissionCompare");
        }

        base.OnActionExecuting(context);
    }
}
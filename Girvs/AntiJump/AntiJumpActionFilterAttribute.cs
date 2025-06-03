using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Girvs.AntiJump;

public class AntiJumpActionFilterAttribute : ActionFilterAttribute
{
    /// <summary>
    /// 判断当前用户是否登陆
    /// </summary>
    /// <returns></returns>
    private bool IsLogin()
    {
        //return (EngineContext.Current.ClaimManager?.CurrentClaims ?? Array.Empty<Claim>()).Any();
        var httpContext = EngineContext.Current.HttpContext;
        return httpContext?.User.Identity is {IsAuthenticated: true};
    }

    public override Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.ActionDescriptor is not ControllerActionDescriptor actionDescriptor)
            return base.OnActionExecutionAsync(context, next);

        var antiJumpAttribute = actionDescriptor.MethodInfo.GetCustomAttribute<AntiJumpAttribute>(false);
        if (antiJumpAttribute == null) return base.OnActionExecutionAsync(context, next);

        var token = string.Empty;

        if (IsLogin())
            token = context.HttpContext.Request.Headers.Authorization;



        if ((antiJumpAttribute.AntiJumpLogic & AntiJumpLogic.Verify) == AntiJumpLogic.Verify)
        {
            var antiJumpKey = $"{antiJumpAttribute.VerifyKey}_{token}".ToLower().ToMd5();

            if (context.HttpContext.Request.Headers.TryGetValue(antiJumpAttribute.RelationName, out var value))
            {
                var checkValue = value.ToString();
                if (antiJumpKey != checkValue)
                    throw new GirvsException("非法请求");
            }
            else
            {
                throw new GirvsException("非法请求");
            }
        }

        if ((antiJumpAttribute.AntiJumpLogic & AntiJumpLogic.Generate) == AntiJumpLogic.Generate)
        {
            var antiJumpKey = $"{antiJumpAttribute.GenerateKey}_{token}".ToLower().ToMd5();
            context.HttpContext.Response.Headers.Append(antiJumpAttribute.RelationName, antiJumpKey);
        }

        return base.OnActionExecutionAsync(context, next);
    }
}
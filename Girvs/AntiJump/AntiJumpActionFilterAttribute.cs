using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Girvs.AntiJump;

/// <summary>
/// 防止表单跳跃提交，此处可能会存在一些问题，需要根据实际情况进行调整，特别是否存在性能问题
/// </summary>
public class AntiJumpActionFilterAttribute : ActionFilterAttribute
{
    public override Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.ActionDescriptor is not ControllerActionDescriptor actionDescriptor)
            return base.OnActionExecutionAsync(context, next);

        var antiJumpAttribute = actionDescriptor.MethodInfo.GetCustomAttribute<AntiJumpAttribute>(false);
        if (antiJumpAttribute == null) return base.OnActionExecutionAsync(context, next);

        var token =  context.HttpContext.Request.Headers.Authorization.ToString();

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
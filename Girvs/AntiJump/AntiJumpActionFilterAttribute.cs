using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Girvs.AntiJump;

/// <summary>
/// 防止表单跳跃提交，此处可能会存在一些问题，需要根据实际情况进行调整，特别是否存在性能问题
/// </summary>
public class AntiJumpActionFilterAttribute : ActionFilterAttribute
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.ActionDescriptor is not ControllerActionDescriptor actionDescriptor)
        {
            await next();
            return;
        }

        var antiJumpAttribute = actionDescriptor.MethodInfo.GetCustomAttribute<AntiJumpAttribute>(false);
        if (antiJumpAttribute == null)
        {
            await next();
            return;
        }

        var token = context.HttpContext.Request.Headers.Authorization.ToString();

        var currentDatetime = DateTime.Now.ToUnixTimestamp();

        if ((antiJumpAttribute.AntiJumpLogic & AntiJumpLogic.Verify) == AntiJumpLogic.Verify)
        {
            if (context.HttpContext.Request.Headers.TryGetValue(antiJumpAttribute.RelationName, out var value))
            {
                var requestValue = value.ToString();
                //上一次请求生成的key
                var requestKey = requestValue.Left(32);

                //上一次请求的时间戳
                var requestTime = Convert.ToDouble(requestValue.Right(10));

                // 生成校验Key
                var verifyKey = BuildKey(antiJumpAttribute.GenerateKey, token, requestTime);
                if (requestKey != verifyKey)
                    throw new GirvsException("非法请求");

                //计算时间差，判断请求的有效性
                if (currentDatetime - requestTime > 15)
                    throw new GirvsException("请求已超时，请重试！");
            }
            else
            {
                throw new GirvsException("非法请求");
            }
        }

        var resultContext = await next();

        if ((antiJumpAttribute.AntiJumpLogic & AntiJumpLogic.Generate) == AntiJumpLogic.Generate)
        {
            var antiJumpKey = BuildKey(antiJumpAttribute.GenerateKey, token, currentDatetime);
            var headerValue = $"{antiJumpKey}_{currentDatetime}";
            resultContext.HttpContext.Response.Headers.Append(antiJumpAttribute.RelationName, headerValue);
        }
    }

    string BuildKey(string key, string token, double timestamp)
    {
        return $"{key}_{token}_{timestamp}".ToLower().ToMd5();
    }
}
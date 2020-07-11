using Microsoft.AspNetCore.Mvc.Filters;

namespace Girvs.WebFrameWork.Filters
{
    public class CustomExceptionAttribute : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            //    int status = 568;
            //    context.ExceptionHandled = true;
            //    if (context.Exception is SpBusinessException)
            //    {
            //        context.Result = new CustomExceptionResult((int)HttpStatusCode.OK, context.Exception);
            //    }
            //    else
            //    {
            //        log.LogError(context.Exception, "未处理异常");
            //        context.Result = new CustomExceptionResult((int)status, context.Exception);
            //    }
            //}
        }
    }
}
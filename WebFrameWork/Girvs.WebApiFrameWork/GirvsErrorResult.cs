using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Girvs.WebApiFrameWork
{
    public class GirvsErrorResult : StatusCodeResult
    {
        private readonly string _message;
        private readonly int _defaultStatusCode;
        public GirvsErrorResult(string message, int statusCode = 568) : base(statusCode)
        {
            _defaultStatusCode = statusCode;
            _message = message;
        }

        public override async Task ExecuteResultAsync(ActionContext context)
        {
            await base.ExecuteResultAsync(context);
            await context.HttpContext.Response.WriteAsync(_message);
        }
    }
}
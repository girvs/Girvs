using Microsoft.AspNetCore.Mvc;

namespace Girvs.WebApiFrameWork.Infrastructure
{
    public static class ControllerExtensions
    {
        public static GirvsErrorResult GirvsBusinessError(this ControllerBase controllerBase, string message, int statusCode = 568)
        {
            return new GirvsErrorResult(message, statusCode);
        }
    }
}
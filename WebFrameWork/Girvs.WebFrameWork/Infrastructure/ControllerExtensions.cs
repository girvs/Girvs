using Microsoft.AspNetCore.Mvc;

namespace Girvs.WebFrameWork.Infrastructure
{
    public static class ControllerExtensions
    {
        public static GirvsErrorResult GirvsBusinessError(this ControllerBase controllerBase, string message)
        {
            return new GirvsErrorResult(message);
        }
    }
}
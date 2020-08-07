using Microsoft.AspNetCore.Mvc;

namespace Girvs.WebFrameWork.Infrastructure
{
    public static class ControllerExtensions
    {
        public static GirvsBusinessErrorResult GirvsBusinessError(this ControllerBase controllerBase, string message)
        {
            return new GirvsBusinessErrorResult(message);
        }
    }
}
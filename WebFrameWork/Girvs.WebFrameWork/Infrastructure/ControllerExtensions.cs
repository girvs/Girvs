using Microsoft.AspNetCore.Mvc;

namespace Girvs.WebFrameWork.Infrastructure
{
    public static class ControllerExtensions
    {
        public static SpBusinessErrorResult SpBusinessError(this ControllerBase controllerBase, string message)
        {
            return new SpBusinessErrorResult(message);
        }
    }
}
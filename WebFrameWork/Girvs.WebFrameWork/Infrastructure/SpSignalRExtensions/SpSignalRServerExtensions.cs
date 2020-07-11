using Microsoft.Extensions.DependencyInjection;

namespace Girvs.WebFrameWork.Infrastructure.SpSignalRExtensions
{
    public static class SpSignalRServerExtensions
    {
        public static void AddSpSignalRServer(this IServiceCollection servers)
        {
            servers.AddSignalRCore();
        }
    }
}
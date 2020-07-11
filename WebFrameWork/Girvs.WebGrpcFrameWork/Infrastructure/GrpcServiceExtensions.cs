using Microsoft.Extensions.DependencyInjection;

namespace Girvs.WebGrpcFrameWork.Infrastructure
{
    public static class GrpcServiceExtensions
    {
        public static void AddSpGrpcService(this IServiceCollection services)
        {
            services.AddGrpc();
        }
    }
}
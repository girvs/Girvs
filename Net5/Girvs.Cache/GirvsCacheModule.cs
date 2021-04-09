using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.Cache
{
    public class GirvsCacheModule : IAppModuleStartup
    {
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            
        }

        public void Configure(IApplicationBuilder application)
        {
            
        }

        public int Order { get; }
    }
}
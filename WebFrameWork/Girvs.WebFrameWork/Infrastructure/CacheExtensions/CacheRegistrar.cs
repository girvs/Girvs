using Girvs.Domain.Configuration;
using Girvs.Domain.Infrastructure.DependencyManagement;
using Girvs.Domain.TypeFinder;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.WebFrameWork.Infrastructure.CacheExtensions
{
    public class CacheRegistrar : IDependencyRegistrar
    {
        public void Register(IServiceCollection services, ITypeFinder typeFinder, GirvsConfig config)
        {
            services.AddSpDataProtection();
            services.AddMemoryCache();
            services.AddEasyCaching();
            services.AddCacheService(config);
        }

        public int Order { get; } = 4;
    }
}
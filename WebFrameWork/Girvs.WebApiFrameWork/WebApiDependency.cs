using Girvs.Application;
using Girvs.Domain.Configuration;
using Girvs.Domain.Infrastructure.DependencyManagement;
using Girvs.Domain.TypeFinder;
using Girvs.WebFrameWork.Infrastructure.ServicesExtensions;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.WebApiFrameWork
{
    public class WebApiDependency : IDependencyRegistrar
    {
        public void Register(IServiceCollection services, ITypeFinder typeFinder, GirvsConfig config)
        {
            services.RegisterType<IAppWebApiService>(typeFinder);
        }

        public int Order { get; } = 9;
    }
}
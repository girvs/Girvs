using Girvs.Domain.Configuration;
using Girvs.Domain.Infrastructure.DependencyManagement;
using Girvs.Domain.TypeFinder;
using Girvs.WebFrameWork.Infrastructure.ServicesExtensions;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.WebFrameWork.Infrastructure
{
    public class DependencyRegistrar : IDependencyRegistrar
    {
        public virtual void Register(IServiceCollection services, ITypeFinder typeFinder, GirvsConfig config)
        {
            services.AddRegisterBusinessServices(typeFinder);
        }


        public int Order => 9;
    }
}
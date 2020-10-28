using Girvs.Domain.Configuration;
using Girvs.Domain.Infrastructure.DependencyManagement;
using Girvs.Domain.IRepositories;
using Girvs.Domain.Managers;
using Girvs.Domain.TypeFinder;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.Service.FrameWork.Infrastructure
{
    public class DependencyRegistrar : IDependencyRegistrar
    {
        public virtual void Register(IServiceCollection services, ITypeFinder typeFinder, GirvsConfig config)
        {
            services.RegisterType(typeof(IRepository<>), typeFinder, true);
            services.RegisterType<IManager>(typeFinder, true);
        }


        public int Order => 0;
    }
}
using Girvs.Domain.Configuration;
using Girvs.Domain.Infrastructure.DependencyManagement;
using Girvs.Domain.Managers;
using Girvs.Domain.TypeFinder;
using Microsoft.Extensions.DependencyInjection;

namespace Test.Infrastructure
{
    public class test:IDependencyRegistrar
    {
        public void Register(IServiceCollection services, ITypeFinder typeFinder, GirvsConfig config)
        {
            services.AddSingleton<IUnitOfWork, CmmpDbContext>();
        }

        public int Order { get; } = int.MaxValue;
    }
}
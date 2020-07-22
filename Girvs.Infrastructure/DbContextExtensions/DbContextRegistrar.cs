using Girvs.Domain.Configuration;
using Girvs.Domain.Infrastructure.DependencyManagement;
using Girvs.Domain.IRepositories;
using Girvs.Domain.TypeFinder;
using Girvs.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.Infrastructure.DbContextExtensions
{
    public class DbContextRegistrar: IDependencyRegistrar
    {
        public void Register(IServiceCollection services, ITypeFinder typeFinder, GirvsConfig config)
        {
            services.AddScoped(typeof(IRepository<>),typeof(Repository<>));
            services.AddSpObjectContext();
        }

        public int Order { get; } = 1;
    }
}
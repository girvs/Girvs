using Girvs.Domain.Configuration;
using Girvs.Domain.Infrastructure.DependencyManagement;
using Girvs.Domain.TypeFinder;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.Service.FrameWork.Infrastructure.DataProtectionExtensions
{
    public class DataProtectionDependencyRegistrar : IDependencyRegistrar
    {
        public void Register(IServiceCollection services, ITypeFinder typeFinder, GirvsConfig config)
        {
            services.AddGirvsDataProtection();
        }

        public int Order { get; } = 103;
    }
}
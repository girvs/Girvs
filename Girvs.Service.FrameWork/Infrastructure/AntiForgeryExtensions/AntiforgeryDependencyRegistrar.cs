using Girvs.Domain.Configuration;
using Girvs.Domain.Infrastructure.DependencyManagement;
using Girvs.Domain.TypeFinder;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.Service.FrameWork.Infrastructure.AntiForgeryExtensions
{
    public class AntiforgeryDependencyRegistrar : IDependencyRegistrar
    {
        public void Register(IServiceCollection services, ITypeFinder typeFinder, GirvsConfig config)
        {
            services.AddGrivsAntiforgery();
        }

        public int Order { get; } = 101;
    }
}
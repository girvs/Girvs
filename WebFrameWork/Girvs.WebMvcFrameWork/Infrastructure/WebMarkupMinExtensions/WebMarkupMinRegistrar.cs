using Girvs.Domain.Configuration;
using Girvs.Domain.Infrastructure.DependencyManagement;
using Girvs.Domain.TypeFinder;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.WebMvcFrameWork.Infrastructure.WebMarkupMinExtensions
{
    public class WebMarkupMinRegistrar : IDependencyRegistrar
    {
        public void Register(IServiceCollection services, ITypeFinder typeFinder, GirvsConfig config)
        {
            services.AddSpWebMarkupMin();
        }

        public int Order { get; }
    }
}
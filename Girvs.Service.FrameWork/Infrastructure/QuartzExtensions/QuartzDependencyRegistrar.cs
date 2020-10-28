using System;
using System.Linq;
using Girvs.Domain.Configuration;
using Girvs.Domain.Infrastructure.DependencyManagement;
using Girvs.Domain.TypeFinder;
using Microsoft.Extensions.DependencyInjection;
using Quartz.Spi;

namespace Girvs.Service.FrameWork.Infrastructure.QuartzExtensions
{
    public class QuartzDependencyRegistrar : IDependencyRegistrar
    {
        public void Register(IServiceCollection services, ITypeFinder typeFinder, GirvsConfig config)
        {
            services.AddQuartzHosted();
        }

        public int Order { get; } = 105;
    }
}
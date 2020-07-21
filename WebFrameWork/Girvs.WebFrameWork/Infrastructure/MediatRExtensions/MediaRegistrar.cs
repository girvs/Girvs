using Girvs.Domain.Configuration;
using Girvs.Domain.Infrastructure.DependencyManagement;
using Girvs.Domain.TypeFinder;
using Girvs.WebFrameWork.Infrastructure.ServicesExtensions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.WebFrameWork.Infrastructure.MediatRExtensions
{
    public class MediaRegistrar : IDependencyRegistrar
    {
        public void Register(IServiceCollection services, ITypeFinder typeFinder, GirvsConfig config)
        {
            services.AddMediatR(typeof(MediaRegistrar));
            services.RegisterType(typeof(INotificationHandler<>), typeFinder);
            services.RegisterType(typeof(IRequestHandler<>), typeFinder);
        }

        public int Order { get; } = 108;
    }
}
using FluentValidation;
using Girvs.Domain.Configuration;
using Girvs.Domain.Driven.Behaviors;
using Girvs.Domain.Driven.Commands;
using Girvs.Domain.Infrastructure.DependencyManagement;
using Girvs.Domain.TypeFinder;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.Service.FrameWork.Infrastructure.MediatRExtensions
{
    public class MediaDependencyRegistrar : IDependencyRegistrar
    {
        public void Register(IServiceCollection services, ITypeFinder typeFinder, GirvsConfig config)
        {
            services.AddMediatR(typeof(MediaDependencyRegistrar));
            services.RegisterType(typeof(INotificationHandler<>), typeFinder, asType: null);
            services.RegisterType(typeof(CommandHandler), typeFinder, asType: null);
            services.RegisterIValidatorType(typeof(IValidator), typeFinder);
            //添加验证管道
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidatorBehavior<,>));
        }


        public int Order { get; } = 108;
    }
}
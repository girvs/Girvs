using System;
using System.Linq;
using FluentValidation;
using Girvs.Domain.Configuration;
using Girvs.Domain.Driven.Behaviors;
using Girvs.Domain.Driven.Commands;
using Girvs.Domain.Infrastructure.DependencyManagement;
using Girvs.Domain.TypeFinder;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Test.Domain.Commands.User;
using Test.Domain.Validations;

namespace Girvs.WebFrameWork.Infrastructure.MediatRExtensions
{
    public class MediaRegistrar : IDependencyRegistrar
    {
        public void Register(IServiceCollection services, ITypeFinder typeFinder, GirvsConfig config)
        {
            services.AddMediatR(typeof(MediaRegistrar));

            //services.AddScoped<IRequestHandler<RemoveByKeyCommand, bool>, RemoveByKeyCommandHandler>();
            RegisterType(services, typeof(INotificationHandler<>), typeFinder);
            RegisterType(services, typeof(CommandHandler), typeFinder);
            //RegisterType(services, typeof(IValidator<>), typeFinder);
            services.AddScoped<IValidator<CreateUserCommand>, CreateUserCommandValidation>();

            //添加验证管道
            services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidatorBehavior<,>));
        }


        public void RegisterType(IServiceCollection services, Type type, ITypeFinder typeFinder ,Type asType = null)
        {
            var types = typeFinder.FindClassesOfType(type, false, true);
            var interFaceTypes = types.Where(x => x.Name != type.Name).ToList();
            foreach (var repositoryType in interFaceTypes)
            {
                var implementedInterfaces = ((System.Reflection.TypeInfo) repositoryType).ImplementedInterfaces.ToList();
                if (implementedInterfaces.Any())
                {
                    foreach (var bcType in implementedInterfaces)
                    {
                        if (asType != null)
                        {
                            services.AddScoped(asType, bcType);
                        }
                        else
                        {
                            services.AddScoped(bcType, repositoryType);
                        }
                    }
                }
            }
        }

        public int Order { get; } = 108;
    }
}
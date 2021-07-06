using System.Linq;
using Girvs.BusinessBasis;
using Girvs.BusinessBasis.Repositories;
using Girvs.TypeFinder;
using Microsoft.Extensions.DependencyInjection;

namespace Girvs.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static void RegisterRepository(this IServiceCollection services)
        {
            var typeFinder = new WebAppTypeFinder();

            //获取所有继承IRepository<>的接口
            var repositoryTypes = typeFinder.FindOfType(typeof(IRepository<,>), FindType.Interface)
                .Where(x => !x.Name.Contains("IRepository`"));
            foreach (var repositoryType in repositoryTypes)
            {
                var imp = typeFinder.FindOfType(repositoryType).FirstOrDefault();
                if (imp != null)
                {
                    services.AddScoped(repositoryType, imp);
                }
            }
        }

        public static void RegisterUow(this IServiceCollection services)
        {
            // var typeFinder = new WebAppTypeFinder();
            // var uow = typeFinder.FindClassesOfType(typeof(IUnitOfWork)).First();
            // var uowGeneric = typeFinder.FindClassesOfType(typeof(IUnitOfWork<>)).First();
            //
            // if (uowGeneric != null)
            //     services.AddScoped(typeof(IUnitOfWork<>), uowGeneric);
            // else if (uow != null)
            //     services.AddScoped(typeof(IUnitOfWork), uow);
            // else
            //     throw new GirvsException("未找到对应的IUnitOfWork或IUnitOfWork<Entity>实现");
        }

        public static void RegisterManager(this IServiceCollection services)
        {
            var typeFinder = new WebAppTypeFinder();
            var managerInterfaceTypes = typeFinder.FindOfType<IManager>(FindType.Interface)
                .Where(x => x.Name != nameof(IManager));

            foreach (var managerInterfaceType in managerInterfaceTypes)
            {
                var imp = typeFinder.FindOfType(managerInterfaceType).FirstOrDefault();
                if (imp != null)
                {
                    services.AddScoped(managerInterfaceType, imp);
                }
            }
        }
    }
}
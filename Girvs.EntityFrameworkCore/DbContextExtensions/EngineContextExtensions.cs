using System.Linq;
using Girvs.BusinessBasis.Entities;
using Girvs.EntityFrameworkCore.Context;
using Girvs.Infrastructure;
using Girvs.TypeFinder;
using Microsoft.EntityFrameworkCore;

namespace Girvs.EntityFrameworkCore.DbContextExtensions
{
    public static class EngineContextExtensions
    {
        public static DbContext GetEntityRelatedDbContext<TEntity>(this IEngine engine) where TEntity : Entity
        {
            var typeFinder = EngineContext.Current.Resolve<ITypeFinder>();
            var ts = typeFinder.FindOfType(typeof(GirvsDbContext));

            return ts.Where(x =>
                    x.GetProperties().Any(propertyInfo => propertyInfo.PropertyType == typeof(DbSet<TEntity>)))
                .Select(x => EngineContext.Current.Resolve(x) as GirvsDbContext).FirstOrDefault();
        }
    }
}
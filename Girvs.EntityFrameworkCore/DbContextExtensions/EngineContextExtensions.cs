using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Girvs.BusinessBasis.Entities;
using Girvs.EntityFrameworkCore.Context;
using Girvs.Infrastructure;
using Girvs.TypeFinder;
using Microsoft.EntityFrameworkCore;

namespace Girvs.EntityFrameworkCore.DbContextExtensions
{
    public static class EngineContextExtensions
    {
        private static readonly IDictionary<Type, Type> _relatedDbContextCache = new Dictionary<Type, Type>();
        private static object _async = new object();

        public static DbContext GetEntityRelatedDbContext<TEntity>(this IEngine engine) where TEntity : Entity
        {
            lock (_async)
            {
                var entityType = typeof(TEntity);
                if (!_relatedDbContextCache.ContainsKey(entityType))
                {
                    var typeFinder = EngineContext.Current.Resolve<ITypeFinder>();
                    var ts = typeFinder.FindOfType(typeof(GirvsDbContext));

                    var dbContextType = ts
                        .FirstOrDefault(x =>
                            x.GetProperties().Any(propertyInfo => propertyInfo.PropertyType == typeof(DbSet<TEntity>)));

                    if (dbContextType == null)
                    {
                        throw new GirvsException($"未找到对应的DbContext   {typeof(TEntity).Name}");
                    }

                    _relatedDbContextCache.Add(entityType, dbContextType);
                }

                return EngineContext.Current.Resolve(_relatedDbContextCache[entityType]) as DbContext;
            }
        }
    }
}
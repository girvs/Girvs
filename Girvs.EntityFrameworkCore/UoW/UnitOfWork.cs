using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Girvs.BusinessBasis.Entities;
using Girvs.BusinessBasis.UoW;
using Girvs.EntityFrameworkCore.Context;
using Girvs.Infrastructure;
using Girvs.TypeFinder;
using Microsoft.EntityFrameworkCore;

namespace Girvs.EntityFrameworkCore.UoW
{
    public class UnitOfWork<TEntity> : IUnitOfWork<TEntity> where TEntity : Entity
    {
        private readonly IDbContext _context;

        //构造函数注入
        public UnitOfWork()
        {
            _context = GetRelatedDbContext() ?? throw new ArgumentNullException(nameof(DbContext));
            _context.SwitchMasterDataBase();
        }

        IDbContext GetRelatedDbContext()
        {
            var typeFinder = EngineContext.Current.Resolve<ITypeFinder>();
            var ts = typeFinder.FindOfType(typeof(IDbContext));

            return ts.Where(x => x.GetProperties().Any(propertyInfo => propertyInfo.PropertyType == typeof(DbSet<TEntity>)))
                .Select(x => EngineContext.Current.Resolve(x) as GirvsDbContext).FirstOrDefault();

            //return (from type in ts
            //    where type.GetProperties().Any(x => x.PropertyType == typeof(DbSet<TEntity>))
            //    select EngineContext.Current.Resolve(type) as GirvsDbContext).FirstOrDefault();
        }


        //手动回收
        public void Dispose()
        {
            _context.Dispose();
        }

        public async Task<bool> Commit(CancellationToken cancellationToken = new CancellationToken())
        {
            return await _context.SaveChangesAsync(cancellationToken) > 0;
        }
    }
}
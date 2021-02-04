using System;
using System.Threading;
using System.Threading.Tasks;
using Girvs.Domain.Infrastructure;
using Girvs.Domain.Managers;
using Girvs.Domain.Models;
using Girvs.Domain.TypeFinder;
using Microsoft.EntityFrameworkCore;

namespace Girvs.Infrastructure.UoW
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
            var ts = typeFinder.FindClassesOfType(typeof(IDbContext), true, false);
            foreach (var type in ts)
            {
                var dbcontext = EngineContext.Current.Resolve(type) as GirvsDbContext;
                if (dbcontext.ModelTypes.Contains(typeof(TEntity)))
                {
                    return dbcontext;
                }
            }

            return null;
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
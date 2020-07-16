using System;
using System.Threading.Tasks;
using Girvs.Domain.Managers;

namespace SmartProducts.Person.Infrastructure.UoW
{
    /// <summary>
    /// 工作单元
    /// </summary>
    public class UnitOfWork : IUnitOfWork
    {
        private readonly PersonDbContext _dbContext;

        public UnitOfWork(PersonDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<bool> Commit()
        {
            return await _dbContext.SaveChangesAsync() > 0;
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }
    }
}
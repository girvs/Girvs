using System.Threading.Tasks;
using Girvs.Domain.Managers;
using Girvs.Infrastructure;

namespace Test.Infrastructure.UoW
{
    public class UnitOfWork : IUnitOfWork
    {
        //数据库上下文
        private readonly IDbContext _context;

        //构造函数注入
        public UnitOfWork(IDbContext context)
        {
            _context = context;
        }

        //上下文提交
        public async Task<bool> Commit()
        {
            return await _context.SaveChangesAsync() > 0;
        }

        //手动回收
        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
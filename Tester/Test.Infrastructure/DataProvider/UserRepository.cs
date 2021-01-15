using System.Threading.Tasks;
using Girvs.Infrastructure;
using Girvs.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Test.Domain.Models;
using Test.Domain.Repositories;

namespace Test.Infrastructure.DataProvider
{
    public class UserRepository : Repository<User>, IUserRepository
    {
        public UserRepository(IDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<User> GetUserByLoginNameAsync(string loginName)
        {
            return await DbSet.FirstOrDefaultAsync(x => x.UserAccount == loginName);
        }
    }
}
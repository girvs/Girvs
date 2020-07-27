using System;
using System.Threading.Tasks;
using Girvs.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Test.Domain.Models;
using Test.Domain.Repositories;

namespace Test.Infrastructure.DataProvider
{
    public class UserRepository : Repository<User>, IUserRepository
    {
        private readonly CmmpDbContext _dbContext;

        public UserRepository(CmmpDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        public async Task<User> GetUserByLoginNameAsync(string loginName)
        {
            return await _dbContext.Users.FirstOrDefaultAsync(x => x.UserAccount == loginName);
        }
    }
}
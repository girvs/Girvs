using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Girvs.Domain.Extensions;
using Girvs.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Power.BasicManagement.Domain.Models;
using Power.BasicManagement.Domain.Repositories;

namespace Power.BasicManagement.Infrastructure.Repositories
{
    public class UserRepository : Repository<User>, IUserRepository
    {
        public async Task<User> GetUserByLoginNameAsync(string loginName)
        {
            return await DbSet.Include(x => x.UserRoles).FirstOrDefaultAsync(x => x.UserAccount == loginName);
        }

        public Task<User> GetUserByIdIncludeRolesAsync(Guid userId)
        {
            return DbSet.Include(x => x.UserRoles).FirstOrDefaultAsync(x => x.Id == userId);
        }

        public Task<User> GetUserByOtherIdAsync(Guid otherId)
        {
            return DbSet.FirstOrDefaultAsync(u => u.OtherId == otherId);
        }

        public override async Task<List<User>> GetAllAsync(params string[] fields)
        {
            if (fields.Any())
            {
                //临时方法，待改进,不科学的方法
                return await Task.Run(() =>
                    DbSet.SelectProperties(fields).Where(u => !u.IsInitData).ToList());
            }
            else
            {
                return await DbSet.Where(u => !u.IsInitData).ToListAsync();
            }
        }
    }
}
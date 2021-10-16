using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Girvs.AuthorizePermission.Enumerations;
using Girvs.EntityFrameworkCore.Repositories;
using Girvs.Infrastructure;
using Microsoft.EntityFrameworkCore;
using ZhuoFan.Wb.BasicService.Domain.Models;
using ZhuoFan.Wb.BasicService.Domain.Repositories;

namespace ZhuoFan.Wb.BasicService.Infrastructure.Repositories
{
    public class UserRepository : Repository<User>, IUserRepository
    {
        public Task<User> GetUserByLoginNameAsync(string loginName)
        {
            //此处需地绕过所有的查询条件
            var dbContext = EngineContext.Current.Resolve<BasicManagementDbContext>();
            return dbContext.Set<User>().FirstOrDefaultAsync(x => x.UserAccount == loginName);
        }

        public Task<User> GetUserByIdIncludeRolesAsync(Guid userId)
        {
            return Queryable.Include(x => x.Roles).FirstOrDefaultAsync(x => x.Id == userId);
        }

        public Task<User> GetUserByOtherIdAsync(Guid otherId)
        {
            return GetAsync(u => u.OtherId == otherId);
        }

        public Task<User> GetUserByIdIncludeRoleAndDataRule(Guid userId)
        {
            //此处需地绕过所有的查询条件，防止无限死循环
            var dbContext = EngineContext.Current.Resolve<BasicManagementDbContext>();
            return dbContext.Set<User>().Include(x => x.Roles).Include(x => x.RulesList).FirstOrDefaultAsync(x => x.Id == userId);
        }

        public Task CreateTenantIdAdmin(User user)
        {
            //此处创建的用户是通过事件创建的租户管理员，是通过总管理员创建，需要绕过租户的判断
            var dbContext = EngineContext.Current.Resolve<BasicManagementDbContext>();
            dbContext.Set<User>().AddAsync(user);
            return Task.CompletedTask;
        }

        public Task<List<User>> GetUsersIncludeRolesAndDataRule(Expression<Func<User, bool>> predicate)
        {
            return Queryable.Include(x => x.Roles).Include(x => x.RulesList).Where(predicate).ToListAsync();
        }
    }
}
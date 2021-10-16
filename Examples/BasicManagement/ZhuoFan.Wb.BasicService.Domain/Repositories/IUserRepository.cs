using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Girvs.BusinessBasis.Repositories;
using ZhuoFan.Wb.BasicService.Domain.Models;

namespace ZhuoFan.Wb.BasicService.Domain.Repositories
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User> GetUserByLoginNameAsync(string loginName);

        Task<User> GetUserByIdIncludeRolesAsync(Guid userId);

        Task<User> GetUserByOtherIdAsync(Guid otherId);

        Task<User> GetUserByIdIncludeRoleAndDataRule(Guid userId);
        Task CreateTenantIdAdmin(User user);

        Task<List<User>> GetUsersIncludeRolesAndDataRule(Expression<Func<User, bool>> predicate);
    }
}
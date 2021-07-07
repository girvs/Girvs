using System;
using System.Threading.Tasks;
using BasicManagement.Domain.Models;
using Girvs.BusinessBasis.Repositories;

namespace BasicManagement.Domain.Repositories
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User> GetUserByLoginNameAsync(string loginName);

        Task<User> GetUserByIdIncludeRolesAsync(Guid userId);

        Task<User> GetUserByOtherIdAsync(Guid otherId);
    }
}
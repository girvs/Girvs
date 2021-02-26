using System;
using System.Threading.Tasks;
using Girvs.Domain.IRepositories;
using Power.BasicManagement.Domain.Models;

namespace Power.BasicManagement.Domain.Repositories
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User> GetUserByLoginNameAsync(string loginName);

        Task<User> GetUserByIdIncludeRolesAsync(Guid userId);

        Task<User> GetUserByOtherIdAsync(Guid otherId);
    }
}
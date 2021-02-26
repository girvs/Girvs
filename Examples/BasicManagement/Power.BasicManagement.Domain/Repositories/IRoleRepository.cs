using System;
using System.Threading.Tasks;
using Girvs.Domain.IRepositories;
using Power.BasicManagement.Domain.Models;

namespace Power.BasicManagement.Domain.Repositories
{
    public interface IRoleRepository : IRepository<Role>
    {
        Task<Role> GetRoleByIdIncludeUsersAsync(Guid roleId);
    }
}
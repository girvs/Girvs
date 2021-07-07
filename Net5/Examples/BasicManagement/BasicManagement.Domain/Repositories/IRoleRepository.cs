using System;
using System.Threading.Tasks;
using BasicManagement.Domain.Models;
using Girvs.BusinessBasis.Repositories;

namespace BasicManagement.Domain.Repositories
{
    public interface IRoleRepository : IRepository<Role>
    {
        Task<Role> GetRoleByIdIncludeUsersAsync(Guid roleId);
    }
}
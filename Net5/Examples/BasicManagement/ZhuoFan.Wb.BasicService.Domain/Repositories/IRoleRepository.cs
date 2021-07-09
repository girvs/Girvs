using System;
using System.Threading.Tasks;
using Girvs.BusinessBasis.Repositories;
using ZhuoFan.Wb.BasicService.Domain.Models;

namespace ZhuoFan.Wb.BasicService.Domain.Repositories
{
    public interface IRoleRepository : IRepository<Role>
    {
        Task<Role> GetRoleByIdIncludeUsersAsync(Guid roleId);
    }
}
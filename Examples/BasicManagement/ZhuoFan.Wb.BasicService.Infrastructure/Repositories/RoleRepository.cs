using System;
using System.Threading.Tasks;
using Girvs.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;
using ZhuoFan.Wb.BasicService.Domain.Models;
using ZhuoFan.Wb.BasicService.Domain.Repositories;

namespace ZhuoFan.Wb.BasicService.Infrastructure.Repositories
{
    public class RoleRepository : Repository<Role>, IRoleRepository
    {
        public Task<Role> GetRoleByIdIncludeUsersAsync(Guid roleId)
        {
            return Queryable.Include(x => x.Users).FirstOrDefaultAsync(x => x.Id == roleId);
        }
        //
        // public override Task<Role> GetByIdAsync(Guid id)
        // {
        //     return Queryable.Include(r => r.Users).FirstOrDefaultAsync(r => r.Id == id);
        // }
    }
}
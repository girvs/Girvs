using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Girvs.EntityFrameworkCore.Repositories;
using Girvs.Extensions;
using Microsoft.EntityFrameworkCore;
using ZhuoFan.Wb.BasicService.Domain.Models;
using ZhuoFan.Wb.BasicService.Domain.Repositories;

namespace ZhuoFan.Wb.BasicService.Infrastructure.Repositories
{
    public class PermissionRepository : Repository<BasalPermission>, IPermissionRepository
    {
        public async Task<List<BasalPermission>> GetUserPermissionLimit(Guid userId)
        {
            Expression<Func<BasalPermission, bool>> condition = x => x.AppliedID == userId;
            return await DbSet.Where(condition).ToListAsync();
        }

        public async Task<List<BasalPermission>> GetRolePermissionLimit(Guid roleId)
        {
            Expression<Func<BasalPermission, bool>> condition = x => x.AppliedID == roleId;
            return await DbSet.Where(condition).ToListAsync();
        }

        public async Task<List<BasalPermission>> GetRoleListPermissionLimit(Guid[] roleIds)
        {
            Expression<Func<BasalPermission, bool>> condition = x => roleIds.Contains(x.AppliedID);
            return await DbSet.Where(condition).ToListAsync();
        }

        public async Task UpdatePermissions(List<BasalPermission> ps)
        {
            if (ps.Any())
            {
                var p = ps[0];
                Expression<Func<BasalPermission, bool>> condition = x => x.AppliedID == p.AppliedID;
                condition = condition.And(x => x.AppliedObjectType == p.AppliedObjectType);
                condition = condition.And(x => x.ValidateObjectType == p.ValidateObjectType);

                var list = await DbSet.Where(condition).ToListAsync();
                await DeleteRangeAsync(list);
                await AddRangeAsync(ps);
            }
        }
    }
}
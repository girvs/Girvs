using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Girvs.EntityFrameworkCore.Repositories;
using Girvs.Extensions;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using ZhuoFan.Wb.BasicService.Domain.Models;
using ZhuoFan.Wb.BasicService.Domain.Repositories;

namespace ZhuoFan.Wb.BasicService.Infrastructure.Repositories
{
    public class PermissionRepository : Repository<BasalPermission>, IPermissionRepository
    {
        private readonly BasicManagementDbContext _dbContext;

        public PermissionRepository([NotNull] BasicManagementDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }
        
        public Task<List<BasalPermission>> GetUserPermissionLimit(Guid userId)
        {
            return _dbContext.Set<BasalPermission>().Where(x => x.AppliedID == userId).ToListAsync();
        }

        public Task<List<BasalPermission>> GetRolePermissionLimit(Guid roleId)
        {
            return _dbContext.Set<BasalPermission>().Where(x => x.AppliedID == roleId).ToListAsync();
        }

        public Task<List<BasalPermission>> GetRoleListPermissionLimit(Guid[] roleIds)
        {
            return _dbContext.Set<BasalPermission>().Where(x => roleIds.Contains(x.AppliedID)).ToListAsync();
        }

        public async Task UpdatePermissions(List<BasalPermission> ps)
        {
            if (ps.Any())
            {
                var p = ps[0];
                Expression<Func<BasalPermission, bool>> condition = x => x.AppliedID == p.AppliedID;
                condition = condition.And(x => x.AppliedObjectType == p.AppliedObjectType);
                condition = condition.And(x => x.ValidateObjectType == p.ValidateObjectType);

                var list = await GetWhereAsync(condition);
                await DeleteRangeAsync(list);
                await AddRangeAsync(ps);
            }
        }
    }
}
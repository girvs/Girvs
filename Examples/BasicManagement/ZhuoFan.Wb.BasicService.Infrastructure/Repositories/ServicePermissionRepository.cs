using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Girvs.EntityFrameworkCore.Repositories;
using ZhuoFan.Wb.BasicService.Domain.Models;
using ZhuoFan.Wb.BasicService.Domain.Repositories;

namespace ZhuoFan.Wb.BasicService.Infrastructure.Repositories
{
    public class ServicePermissionRepository : Repository<ServicePermission, Guid>, IServicePermissionRepository
    {
        public Task<ServicePermission> GetEntityByWhere(Expression<Func<ServicePermission, bool>> expression)
        {
            return GetAsync(expression);
        }
    }
}
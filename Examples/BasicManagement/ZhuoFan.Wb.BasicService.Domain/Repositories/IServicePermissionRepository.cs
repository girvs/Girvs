using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Girvs.BusinessBasis.Repositories;
using ZhuoFan.Wb.BasicService.Domain.Models;

namespace ZhuoFan.Wb.BasicService.Domain.Repositories
{
    public interface IServicePermissionRepository : IRepository<ServicePermission, Guid>
    {
        Task<ServicePermission> GetEntityByWhere(Expression<Func<ServicePermission, bool>> expression);
    }
}
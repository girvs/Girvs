using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Girvs.BusinessBasis.Repositories;
using ZhuoFan.Wb.BasicService.Domain.Models;

namespace ZhuoFan.Wb.BasicService.Domain.Repositories
{
    public interface IServiceDataRuleRepository : IRepository<ServiceDataRule, Guid>
    {
        Task<ServiceDataRule> GetEntityByWhere(Expression<Func<ServiceDataRule, bool>> expression);
    }
}
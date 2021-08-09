using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Girvs.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;
using ZhuoFan.Wb.BasicService.Domain.Models;
using ZhuoFan.Wb.BasicService.Domain.Repositories;

namespace ZhuoFan.Wb.BasicService.Infrastructure.Repositories
{
    public class ServiceDataRuleRepository : Repository<ServiceDataRule, Guid>, IServiceDataRuleRepository
    {
        public Task<ServiceDataRule> GetEntityByWhere(Expression<Func<ServiceDataRule, bool>> expression)
        {
            return DbSet.FirstOrDefaultAsync(expression);
        }
    }
}
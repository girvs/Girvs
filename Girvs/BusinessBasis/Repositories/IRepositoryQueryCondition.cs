using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Girvs.BusinessBasis.Entities;

namespace Girvs.BusinessBasis.Repositories
{
    public interface IRepositoryOtherQueryCondition : IManager
    {
        Task<Expression<Func<TEntity, bool>>> GetOtherQueryCondition<TEntity>() where TEntity : Entity;
    }
}

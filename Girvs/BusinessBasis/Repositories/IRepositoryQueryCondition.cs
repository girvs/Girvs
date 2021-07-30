using System;
using System.Linq.Expressions;
using Girvs.BusinessBasis.Entities;

namespace Girvs.BusinessBasis.Repositories
{
    public interface IRepositoryQueryCondition : IManager
    {
        Expression<Func<TEntity, bool>> GetQueryCondition<TEntity>() where TEntity : Entity;
    }
}
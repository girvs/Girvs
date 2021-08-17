using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Girvs.BusinessBasis.Entities;
using Girvs.Infrastructure;

namespace Girvs.BusinessBasis.Repositories
{
    public abstract class GirvsRepositoryOtherQueryCondition : IRepositoryOtherQueryCondition
    {
        protected const string TENANTFIELDNAME = "TenantId";

        public Expression<Func<TEntity, bool>> BuilderTenantCondition<TEntity>(object tenantId = null)
            where TEntity : Entity
        {
            var propertyInfo = typeof(TEntity).GetProperty(TENANTFIELDNAME);
            if (propertyInfo != null)
            {
                tenantId = GirvsConvert.ToSpecifiedType(propertyInfo.PropertyType.ToString(),
                    tenantId ?? EngineContext.Current.ClaimManager.GetTenantId());

                var param = Expression.Parameter(typeof(TEntity), "entity");
                var left = Expression.Property(param, TENANTFIELDNAME);
                var right = Expression.Constant(tenantId);

                var be = Expression.Equal(left, right);

                return Expression.Lambda<Func<TEntity, bool>>(be, param);
            }

            return x => true;
        }

        public virtual bool TurnOnTenant(Type entityType)
        {
            return entityType.GetProperty(TENANTFIELDNAME) != null;
        }

        public virtual Task<Expression<Func<TEntity, bool>>> GetOtherQueryCondition<TEntity>() where TEntity : Entity
        {
            //默认判断如果存
            var ex = TurnOnTenant(typeof(TEntity)) ? BuilderTenantCondition<TEntity>() : x => true;
            return Task.FromResult<Expression<Func<TEntity, bool>>>(ex);
        }
    }
}
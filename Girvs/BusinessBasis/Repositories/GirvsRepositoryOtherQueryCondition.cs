using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Girvs.BusinessBasis.Entities;
using Girvs.Extensions;
using Girvs.Infrastructure;

namespace Girvs.BusinessBasis.Repositories
{
    public abstract class GirvsRepositoryOtherQueryCondition : IRepositoryOtherQueryCondition
    {
        protected const string TENANTFIELDNAME = "TenantId";

        protected object ConverToTkeyValue(PropertyInfo propertyInfo, object value)
        {
            if (propertyInfo?.PropertyType == typeof(Guid))
            {
                return value.ToString().ToHasGuid();
            }

            if (propertyInfo?.PropertyType == typeof(Int32))
            {
                return int.Parse(value.ToString());
            }

            return value;
        }

        public Expression<Func<TEntity, bool>> BuilderTenantCondition<TEntity>(object tenantId = null)
            where TEntity : Entity
        {
            var propertyInfo = typeof(TEntity).GetProperty(TENANTFIELDNAME);
            if (propertyInfo != null)
            {
                tenantId = ConverToTkeyValue(propertyInfo,
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
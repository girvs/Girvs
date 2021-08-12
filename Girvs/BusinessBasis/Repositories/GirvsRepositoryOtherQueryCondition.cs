using System;
using System.Linq.Expressions;
using System.Reflection;
using Girvs.BusinessBasis.Entities;
using Girvs.Extensions;
using Girvs.Infrastructure;

namespace Girvs.BusinessBasis.Repositories
{
    public abstract class GirvsRepositoryOtherQueryCondition : IRepositoryOtherQueryCondition
    {
        private const string TENANTID = "TenantId";

        protected object ConverToTkeyValue(PropertyInfo propertyInfo, object value)
        {
            if (propertyInfo?.GetType() == typeof(Guid))
            {
                return value.ToString().ToHasGuid();
            }

            if (propertyInfo?.GetType() == typeof(Int32))
            {
                return int.Parse(value.ToString());
            }

            return value;
        }

        public Expression<Func<TEntity, bool>> BuilderTenantCondition<TEntity>(object tenantId = null)
            where TEntity : Entity
        {
            var propertyInfo = typeof(TEntity).GetProperty(TENANTID);
            if (propertyInfo != null)
            {
                tenantId = ConverToTkeyValue(propertyInfo,
                    tenantId ?? EngineContext.Current.ClaimManager.GetTenantId());

                var param = Expression.Parameter(typeof(TEntity), "entity");
                var left = Expression.Property(param, TENANTID);
                var right = Expression.Constant(tenantId);

                var be = Expression.Equal(left, right);

                return Expression.Lambda<Func<TEntity, bool>>(be, param);
            }

            return x => true;
        }

        public virtual Expression<Func<TEntity, bool>> GetOtherQueryCondition<TEntity>() where TEntity : Entity
        {
            return x => true;
        }
    }
}
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

        protected Expression<Func<TEntity, bool>> BuilderBinaryExpression<TEntity>(string fieldName, object value,
            ExpressionType expressionType = ExpressionType.Equal)
        {
            var param = Expression.Parameter(typeof(TEntity), "entity");
            var left = Expression.Property(param, fieldName);
            var right = Expression.Constant(value);
            var be = Expression.MakeBinary(expressionType, left, right);
            return Expression.Lambda<Func<TEntity, bool>>(be, param);
        }

        public Expression<Func<TEntity, bool>> BuilderTenantBinaryExpression<TEntity>(object tenantId = null)
            where TEntity : Entity
        {
            var propertyInfo = typeof(TEntity).GetProperty(TENANTFIELDNAME);
            
            if (propertyInfo != null)
            {
                tenantId = GirvsConvert.ToSpecifiedType(propertyInfo.PropertyType.ToString(),
                    tenantId ?? EngineContext.Current.ClaimManager.GetTenantId());
                return BuilderBinaryExpression<TEntity>(TENANTFIELDNAME, tenantId);
            }

            return x => true;
        }

        public virtual bool EnableOnTenant(Type entityType)
        {
            return entityType.GetProperty(TENANTFIELDNAME) != null;
        }

        public virtual Expression<Func<TEntity, bool>> GetOtherQueryCondition<TEntity>() where TEntity : Entity
        {
            //默认判断如果存
            return EnableOnTenant(typeof(TEntity)) ? BuilderTenantBinaryExpression<TEntity>() : x => true;
        }
    }
}
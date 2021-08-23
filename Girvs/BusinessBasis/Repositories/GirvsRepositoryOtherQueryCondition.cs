using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Girvs.BusinessBasis.Entities;
using Girvs.Infrastructure;

namespace Girvs.BusinessBasis.Repositories
{
    public abstract class GirvsRepositoryOtherQueryCondition : IRepositoryOtherQueryCondition
    {
        protected readonly string tenantFieldName = nameof(IIncludeMultiTenant<object>.TenantId);
        public virtual bool ContainsPublicData { get; set; } = false;

        protected virtual Expression<Func<TEntity, bool>> BuilderBinaryExpression<TEntity>(
            string fieldName,
            object value,
            ExpressionType expressionType = ExpressionType.Equal
        )
        {
            var param = Expression.Parameter(typeof(TEntity), "entity");
            var left = Expression.Property(param, fieldName);
            var right = Expression.Constant(value);
            var be = Expression.MakeBinary(expressionType, left, right);
            return Expression.Lambda<Func<TEntity, bool>>(be, param);
        }

        protected virtual Expression<Func<TEntity, bool>> BuilderTenantBinaryExpression<TEntity>(object tenantId = null)
            where TEntity : Entity
        {
            var propertyInfo = typeof(TEntity).GetProperty(tenantFieldName);

            Expression<Func<TEntity, bool>> expression = x => true;

            if (propertyInfo != null)
            {
                var datas = new List<object>();

                datas.Add(GirvsConvert.ToSpecifiedType(propertyInfo.PropertyType.ToString(),
                    tenantId ?? EngineContext.Current.ClaimManager.GetTenantId()));

                if (ContainsPublicData)
                {
                    datas.Add(Guid.Empty);
                }

                expression = BuilderBinaryExpression<TEntity>(tenantFieldName, datas, ExpressionType.Constant);
            }

            return expression;
        }

        protected virtual bool EnableOnTenant(Type entityType)
        {
            return entityType.GetProperty(tenantFieldName) != null;
        }

        public virtual Expression<Func<TEntity, bool>> GetOtherQueryCondition<TEntity>() where TEntity : Entity
        {
            //默认判断如果存
            return EnableOnTenant(typeof(TEntity)) ? BuilderTenantBinaryExpression<TEntity>() : x => true;
        }
    }
}
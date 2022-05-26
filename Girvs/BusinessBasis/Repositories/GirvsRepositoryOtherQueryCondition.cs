using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Girvs.BusinessBasis.Entities;
using Girvs.Extensions;
using Girvs.Infrastructure;

namespace Girvs.BusinessBasis.Repositories
{
    public abstract class GirvsRepositoryOtherQueryCondition : IRepositoryOtherQueryCondition
    {
        protected readonly string tenantFieldName = nameof(IIncludeMultiTenant<object>.TenantId);
        public virtual bool ContainsPublicData { get; set; } = false;

        protected virtual Expression<Func<TEntity, bool>> BuilderBinaryExpression<TEntity>(
            string fieldName,
            string fieldType,
            ExpressionType expressionType = ExpressionType.Equal,
            params object[] values
        )
        {
            Expression<Func<TEntity, bool>> expression = x => true;
            
            if (!values.Any()) return expression;
            
            Expression<Func<TEntity, bool>> internalExpression = null;

            foreach (var value in values)
            {
                var convertValue = GirvsConvert.ToSpecifiedType(fieldType, value);
                var param = Expression.Parameter(typeof(TEntity), "entity");
                var left = Expression.Property(param, fieldName);
                var right = Expression.Constant(convertValue);
                var be = Expression.MakeBinary(ExpressionType.Equal, left, right);

                internalExpression = internalExpression == null ? 
                    Expression.Lambda<Func<TEntity, bool>>(be, param) : 
                    internalExpression.Or(Expression.Lambda<Func<TEntity, bool>>(be, param));
            }

            expression = expression.And(internalExpression);

            return expression;
        }

        protected virtual Expression<Func<TEntity, bool>> BuilderTenantBinaryExpression<TEntity>(object tenantId = null)
            where TEntity : Entity
        {
            var propertyInfo = typeof(TEntity).GetProperty(tenantFieldName);

            Expression<Func<TEntity, bool>> expression = x => true;

            if (propertyInfo != null)
            {
                var datas = new List<object>
                {
                    tenantId ?? EngineContext.Current.ClaimManager.IdentityClaim.TenantId
                };

                if (ContainsPublicData)
                {
                    datas.Add(Guid.Empty);
                }

                expression = BuilderBinaryExpression<TEntity>(
                    tenantFieldName,
                    propertyInfo.PropertyType.ToString(),
                    ExpressionType.Equal,
                    datas.ToArray());
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
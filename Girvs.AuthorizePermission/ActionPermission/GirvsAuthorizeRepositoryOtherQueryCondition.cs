using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Girvs.BusinessBasis.Repositories;
using Girvs.Extensions;

namespace Girvs.AuthorizePermission.ActionPermission
{
    public abstract class GirvsAuthorizeRepositoryOtherQueryCondition : GirvsRepositoryOtherQueryCondition
    {
        public abstract Task<IList<AuthorizeDataRuleModel>> GetEntityDataRules();


        public override async Task<Expression<Func<TEntity, bool>>> GetOtherQueryCondition<TEntity>()
        {
            //默认判断如果存
            Expression<Func<TEntity, bool>> expression =
                TurnOnTenant(typeof(TEntity)) ? BuilderTenantCondition<TEntity>() : x => true;

            var dataRuleModels = await GetEntityDataRules();

            if (dataRuleModels == null)
            {
                throw new GirvsException("未获取相关的数据授权信息", 568);
            }

            var currentEntityDataRule =
                dataRuleModels.FirstOrDefault(x => x.EntityTypeName == typeof(TEntity).FullName);

            if (currentEntityDataRule == null) return expression;
            
            foreach (var dataRuleFieldModel in currentEntityDataRule.AuthorizeDataRuleFieldModels)
            {
                var param = Expression.Parameter(typeof(TEntity), "entity");
                var left = Expression.Property(param, dataRuleFieldModel.FieldName);
                var right = Expression.Constant(dataRuleFieldModel.FieldValue);
                var be = Expression.MakeBinary(dataRuleFieldModel.ExpressionType, left, right);
                var newEx = Expression.Lambda<Func<TEntity, bool>>(be, param);
                expression = expression.And(newEx);
            }

            return expression;
        }
    }
}
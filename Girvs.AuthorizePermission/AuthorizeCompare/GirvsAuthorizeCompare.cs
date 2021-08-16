using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Girvs.AuthorizePermission.Enumerations;
using Girvs.BusinessBasis.Repositories;
using Girvs.Extensions;

namespace Girvs.AuthorizePermission.AuthorizeCompare
{
    public abstract class GirvsAuthorizeCompare : GirvsRepositoryOtherQueryCondition, IServiceMethodPermissionCompare
    {
        public abstract Task<AuthorizeModel> GetCurrnetUserAuthorize();

        public override Task<Expression<Func<TEntity, bool>>> GetOtherQueryCondition<TEntity>()
        {
            //默认判断如果存
            Expression<Func<TEntity, bool>> expression =
                TurnOnTenant(typeof(TEntity)) ? BuilderTenantCondition<TEntity>() : x => true;

            var currentUserAuthorize = GetCurrnetUserAuthorize().Result ?? new AuthorizeModel();
            var dataRuleModels = currentUserAuthorize.AuthorizeDataRules;

            if (dataRuleModels == null)
            {
                throw new GirvsException("未获取相关的数据授权信息", 568);
            }

            var currentEntityDataRule =
                dataRuleModels.FirstOrDefault(x => x.EntityTypeName == typeof(TEntity).FullName);

            if (currentEntityDataRule != null)
            {
                foreach (var dataRuleFieldModel in currentEntityDataRule.AuthorizeDataRuleFieldModels)
                {
                    var param = Expression.Parameter(typeof(TEntity), "entity");
                    var left = Expression.Property(param, dataRuleFieldModel.FieldName);
                    var right = Expression.Constant(dataRuleFieldModel.FieldValue);
                    var be = Expression.MakeBinary(dataRuleFieldModel.ExpressionType, left, right);
                    var newEx = Expression.Lambda<Func<TEntity, bool>>(be, param);
                    expression = expression.And(newEx);
                }
            }

            return Task.FromResult<Expression<Func<TEntity, bool>>>(expression);
        }

        public Task<bool> PermissionCompare(Guid functionId, Permission permission)
        {
            var currentUserAuthorize = GetCurrnetUserAuthorize().Result ?? new AuthorizeModel();

            var ps = currentUserAuthorize.AuthorizePermissions;

            if (ps == null || !ps.Any())
            {
                throw new GirvsException("未获取相关的功能授权信息", 568);
            }

            var key = permission.ToString();
            var result = ps.Any(x => x.ServiceId == functionId && x.Permissions.ContainsValue(key));
            return Task.FromResult(result);
        }
    }
}
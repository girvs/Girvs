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
        public abstract AuthorizeModel GetCurrnetUserAuthorize();

        public override Expression<Func<TEntity, bool>> GetOtherQueryCondition<TEntity>()
        {
            //默认判断如果存
            Expression<Func<TEntity, bool>> expression = base.GetOtherQueryCondition<TEntity>();

            var currentUserAuthorize = GetCurrnetUserAuthorize() ?? new AuthorizeModel();
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
                    var fieldValue =
                        GirvsConvert.ToSpecifiedType(dataRuleFieldModel.FieldType, dataRuleFieldModel.FieldValue);

                    var ex = BuilderBinaryExpression<TEntity>(dataRuleFieldModel.FieldName, fieldValue,
                        dataRuleFieldModel.ExpressionType);

                    expression = expression.And(ex);
                }
            }

            return expression;
        }

        public bool PermissionCompare(Guid functionId, Permission permission)
        {
            var currentUserAuthorize = GetCurrnetUserAuthorize() ?? new AuthorizeModel();

            var ps = currentUserAuthorize.AuthorizePermissions;

            if (ps == null)
            {
                throw new GirvsException("未获取相关的功能授权信息", 568);
            }

            var key = permission.ToString();
            return ps.Any(x => x.ServiceId == functionId && x.Permissions.ContainsValue(key));
        }
    }
}
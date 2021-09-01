using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
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

            // 如果用户没有设置数据权限，则直接抛出异常,并且配置设置用户不是默认为所有数据
            if (currentEntityDataRule == null || !currentEntityDataRule.AuthorizeDataRuleFieldModels.Any())
            {
                var config = EngineContext.Current.GetAppModuleConfig<AuthorizeConfig>();
                if (!config.UserDataRuleDefaultAll)
                {
                    throw new GirvsException("未配置当前用户对该模块的数据权限，请先获取权限", 568);
                }
            }
            
            if (currentEntityDataRule != null)
            {
                foreach (var dataRuleFieldModel in currentEntityDataRule.AuthorizeDataRuleFieldModels)
                {
                    if (string.IsNullOrEmpty(dataRuleFieldModel.FieldValue))
                        continue;

                    var fieldValues = dataRuleFieldModel.FieldValue.Split(',');
                    var ex = BuilderBinaryExpression<TEntity>(
                        dataRuleFieldModel.FieldName,
                        dataRuleFieldModel.FieldType,
                        dataRuleFieldModel.ExpressionType,
                        fieldValues);

                    expression = expression.And(ex);
                }
            }

            return expression;
        }

        // private List<object> ConverFieldValueToArray(string fieldType, string fieldValue)
        // {
        //     var values = fieldValue.Split(",");
        //     return values.Select(value => GirvsConvert.ToSpecifiedType(fieldType, value)).ToList();
        // }

        public virtual bool PermissionCompare(Guid functionId, Permission permission)
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

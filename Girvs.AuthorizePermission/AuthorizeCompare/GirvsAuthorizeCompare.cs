using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;
using Girvs.AuthorizePermission.Configuration;
using Girvs.AuthorizePermission.Enumerations;
using Girvs.AuthorizePermission.Extensions;
using Girvs.BusinessBasis.Repositories;
using Girvs.Extensions;
using Girvs.Infrastructure;

namespace Girvs.AuthorizePermission.AuthorizeCompare
{
    public abstract class GirvsAuthorizeCompare : GirvsRepositoryOtherQueryCondition, IServiceMethodPermissionCompare
    {
        public abstract AuthorizeModel GetCurrnetUserAuthorize();

        /// <summary>
        /// 判断当前实体是否包含数据校验规则，不包含则直接跳过
        /// </summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        public virtual bool IsIncludeVerifyDataRuleByEntity(Type entityType)
        {
            var properties = entityType.GetProperties();
            return properties.Select(propertyInfo => propertyInfo.GetCustomAttribute<DataRuleAttribute>())
                .Any(dataRule => dataRule != null);
        }

        /// <summary>
        /// 判断当前用户是否登陆
        /// </summary>
        /// <returns></returns>
        public virtual bool IsLogin()
        {
            return (EngineContext.Current.ClaimManager?.CurrentClaims ?? Array.Empty<Claim>()).Any();
            var httpContext = EngineContext.Current.HttpContext;
            return httpContext != null
                   && httpContext.User.Identity != null
                   && httpContext.User.Identity.IsAuthenticated;
        }

        public override Expression<Func<TEntity, bool>> GetOtherQueryCondition<TEntity>()
        {
            //如果当前用户没有登陆，则跳过
            if (!IsLogin())
            {
                return x => true;
            }

            //默认判断如果存
            Expression<Func<TEntity, bool>> expression = base.GetOtherQueryCondition<TEntity>();

            //当前实体不包含数据权限标识，跳过
            if (!IsIncludeVerifyDataRuleByEntity(typeof(TEntity)))
            {
                return expression;
            }

            //如果是前台或者事件，只添加租户判断
            var identityType = EngineContext.Current.ClaimManager.GetIdentityType();
            if (identityType == IdentityType.RegisterUser || identityType == IdentityType.EventMessageUser)
            {
                return expression;
            }

            //如果登陆的是系统管理员或者是租户管理员，则只返回租户条件，默认为所有的数据权限
            var userType = EngineContext.Current.ClaimManager.GetUserType();
            if (userType == UserType.AdminUser || userType == UserType.TenantAdminUser)
            {
                return expression;
            }

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
using System;
using System.Collections.Generic;
using System.Linq;
using Girvs;
using Girvs.AuthorizePermission;
using Girvs.AuthorizePermission.AuthorizeCompare;
using Girvs.AuthorizePermission.Enumerations;
using Girvs.Cache.Caching;
using Girvs.Extensions;
using Girvs.Infrastructure;
using IdentityServer4.Extensions;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ZhuoFan.Wb.BasicService.Domain;
using ZhuoFan.Wb.BasicService.Domain.Models;

namespace ZhuoFan.Wb.BasicService.Infrastructure.AuthorizeDataSource
{
    public class AuthorizeRepository : GirvsAuthorizeCompare
    {
        private readonly IStaticCacheManager _staticCacheManager;

        public AuthorizeRepository(
            [NotNull] IStaticCacheManager staticCacheManager
        )
        {
            _staticCacheManager = staticCacheManager ?? throw new ArgumentNullException(nameof(staticCacheManager));
        }

        public override AuthorizeModel GetCurrnetUserAuthorize()
        {
            //由于是循环依赖注入，导致此处要解决循环依赖的问题，所以在此处特殊处理。后续可能需要同步进行修改
            //2021-08-25 By xufeng
            if (EngineContext.Current.HttpContext == null || !EngineContext.Current.HttpContext.User.IsAuthenticated())
                return new AuthorizeModel();

            var key =
                $"{GirvsAuthorizePermissionCacheKeyManager.CurrentUserAuthorizeCacheKeyPrefix}:{EngineContext.Current.ClaimManager.GetUserId()}";

            var authorize = _staticCacheManager.Get(
                new CacheKey(key).Create(), () =>
                {
                    var dbContext = EngineContext.Current.Resolve<BasicManagementDbContext>();

                    var userId = EngineContext.Current.ClaimManager.GetUserId().ToHasGuid();

                    var user = dbContext.Users.AsNoTracking().Include(x => x.Roles).Include(x => x.RulesList)
                        .FirstOrDefault(x => x.Id == userId);
                    //如果当前用户类型为管理员或者租户管理员，则直接返回
                    if (user.UserType is UserType.AdminUser or UserType.TenantAdminUser)
                    {
                        var result = new AuthorizeModel
                        {
                            AuthorizePermissions = GetFunctionOperateList(dbContext, user),
                            AuthorizeDataRules = GetDataRuleList(dbContext, user)
                        };
                        return result;
                    }

                    var currentUserRole = user.Roles.Select(x => x.Id).ToArray();
                    var userBasalPermissions = dbContext.BasalPermissions.AsNoTracking().Where(x => x.AppliedID ==
                        userId).ToList();
                    ;
                    var roleBasalPermissions = dbContext.BasalPermissions.AsNoTracking()
                        .Where(x => currentUserRole.Contains(x.AppliedID)).ToList();
                    var mergeBasalPermissions = userBasalPermissions.Union(roleBasalPermissions).ToList();
                    mergeBasalPermissions =
                        PermissionHelper.MergeValidateObjectTypePermission(mergeBasalPermissions);


                    var permissionViewModels =
                        mergeBasalPermissions.Select(p => new AuthorizePermissionModel()
                        {
                            ServiceId = p.AppliedObjectID,
                            Permissions = PermissionHelper.ConvertPermissionToString(p).ToDictionary(x => x, x => x)
                        }).ToList();

                    var authorizeDataRuleModels = PermissionHelper.ConvertAuthorizeDataRuleModels(user.RulesList);

                    return new AuthorizeModel()
                    {
                        AuthorizePermissions = permissionViewModels,
                        AuthorizeDataRules = authorizeDataRuleModels
                    };
                });

            return authorize ?? new AuthorizeModel();
        }


        /// <summary>
        /// 获取需要授权的功能列表
        /// </summary>
        /// <returns></returns>
        private List<AuthorizePermissionModel> GetFunctionOperateList(BasicManagementDbContext dbContext,
            User currentUser)
        {
            var currentUserType = currentUser.UserType;

            var availableAuthorizePermissionList = dbContext.ServicePermissions.AsNoTracking().ToList();

            var currentAvailableAuthorizePermissionList = availableAuthorizePermissionList.Where(x =>
                x.OperationPermissions.Any(s => (s.UserType & currentUserType) == currentUserType)).ToList();

            var resultPermissions = new List<AuthorizePermissionModel>();
            foreach (var permissionModel in currentAvailableAuthorizePermissionList)
            {
                var permissions = new Dictionary<string, string>();
                foreach (var operationPermission in permissionModel.OperationPermissions)
                {
                    if (!permissions.ContainsKey(operationPermission.OperationName))
                    {
                        permissions.Add(operationPermission.OperationName, operationPermission.Permission.ToString());
                    }
                }

                resultPermissions.Add(new AuthorizePermissionModel()
                {
                    ServiceName = permissionModel.ServiceName,
                    ServiceId = permissionModel.ServiceId,
                    Permissions = permissions
                });
            }

            return resultPermissions;
        }

        /// <summary>
        /// 获取需要授权的数据规则列表
        /// </summary>
        /// <returns></returns>
        private List<AuthorizeDataRuleModel> GetDataRuleList(BasicManagementDbContext dbContext, User currentUser)
        {
            if (currentUser == null)
            {
                throw new GirvsException(StatusCodes.Status401Unauthorized, "未授权");
            }

            var currentUserType = currentUser.UserType;

            var availableAuthorizeDataRuleList = dbContext.ServiceDataRules.AsNoTracking().ToList();

            var result = new List<AuthorizeDataRuleModel>();

            foreach (var authorizeDataRule in availableAuthorizeDataRuleList.Where(x =>
                currentUserType == (x.UserType & currentUserType)))
            {
                var existReturnReuslt =
                    result.FirstOrDefault(x => x.EntityTypeName == authorizeDataRule.EntityTypeName);
                if (existReturnReuslt != null)
                {
                    existReturnReuslt.AuthorizeDataRuleFieldModels.Add(new AuthorizeDataRuleFieldModel()
                    {
                        ExpressionType = authorizeDataRule.ExpressionType,
                        FieldType = authorizeDataRule.FieldType,
                        FieldName = authorizeDataRule.FieldName,
                        FieldValue = authorizeDataRule.FieldValue,
                        FieldDesc = authorizeDataRule.FieldDesc
                    });
                }
                else
                {
                    existReturnReuslt =
                        new AuthorizeDataRuleModel()
                        {
                            EntityTypeName = authorizeDataRule.EntityTypeName,
                            EntityDesc = authorizeDataRule.EntityDesc
                        };

                    existReturnReuslt.AuthorizeDataRuleFieldModels.Add(new AuthorizeDataRuleFieldModel()
                    {
                        ExpressionType = authorizeDataRule.ExpressionType,
                        FieldType = authorizeDataRule.FieldType,
                        FieldName = authorizeDataRule.FieldName,
                        FieldValue = authorizeDataRule.FieldValue,
                        FieldDesc = authorizeDataRule.FieldDesc
                    });

                    result.Add(existReturnReuslt);
                }
            }

            return result;
        }
    }
}
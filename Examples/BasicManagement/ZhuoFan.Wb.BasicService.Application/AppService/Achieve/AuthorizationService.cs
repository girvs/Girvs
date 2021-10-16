using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Girvs;
using Girvs.AuthorizePermission;
using Girvs.AuthorizePermission.Services;
using Girvs.Cache.Caching;
using Girvs.Driven.Bus;
using Girvs.Driven.Notifications;
using Girvs.EventBus;
using Girvs.Infrastructure;
using JetBrains.Annotations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Panda.DynamicWebApi.Attributes;
using ZhuoFan.Wb.BasicService.Domain.Extensions;
using ZhuoFan.Wb.BasicService.Domain.Models;
using ZhuoFan.Wb.BasicService.Domain.Repositories;
using ZhuoFan.Wb.Common.Events.Authorize;

namespace ZhuoFan.Wb.BasicService.Application.AppService.Achieve
{
    [DynamicWebApi]
    [Authorize(AuthenticationSchemes = GirvsAuthenticationScheme.GirvsJwt)]
    public class AuthorizationService : IAuthorizationService
    {
        private readonly IStaticCacheManager _cacheManager;
        private readonly IServiceDataRuleRepository _serviceDataRuleRepository;
        private readonly IServicePermissionRepository _servicePermissionRepository;
        private readonly IMediatorHandler _bus;
        private readonly DomainNotificationHandler _notifications;

        public AuthorizationService(
            [NotNull] IStaticCacheManager cacheManager,
            [NotNull] IServiceDataRuleRepository serviceDataRuleRepository,
            [NotNull] IServicePermissionRepository servicePermissionRepository,
            [NotNull] IMediatorHandler bus,
            [NotNull] INotificationHandler<DomainNotification> notifications
        )
        {
            _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
            _serviceDataRuleRepository = serviceDataRuleRepository ??
                                         throw new ArgumentNullException(nameof(serviceDataRuleRepository));
            _servicePermissionRepository = servicePermissionRepository ??
                                           throw new ArgumentNullException(nameof(servicePermissionRepository));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _notifications = notifications as DomainNotificationHandler ??
                             throw new ArgumentNullException(nameof(notifications));
        }

        /// <summary>
        /// 获取需要授权的功能列表
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IList<AuthorizePermissionModel>> GetFunctionOperateList()
        {
            var currentUser = EngineContext.Current.GetCurrentUser();

            if (currentUser == null)
            {
                throw new GirvsException(StatusCodes.Status401Unauthorized, "未授权");
            }

            var currentUserType = currentUser.UserType;

            var availableAuthorizePermissionList = await _cacheManager.GetAsync(
                GirvsEntityCacheDefaults<ServicePermission>.AllCacheKey.Create(),
                async () =>
                    await _servicePermissionRepository.GetAllAsync());

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
        [HttpGet]
        public async Task<IList<AuthorizeDataRuleModel>> GetDataRuleList()
        {
            var currentUser = EngineContext.Current.GetCurrentUser();

            if (currentUser == null)
            {
                throw new GirvsException(StatusCodes.Status401Unauthorized, "未授权");
            }

            var currentUserType = currentUser.UserType;

            var availableAuthorizeDataRuleList = await _cacheManager.GetAsync(
                GirvsEntityCacheDefaults<ServiceDataRule>.AllCacheKey.Create(),
                async () =>
                    await _serviceDataRuleRepository.GetAllAsync());

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

        /// <summary>
        /// 初始化本模块的权限值
        /// </summary>
        [HttpGet]
        public async Task InitAuthorization()
        {
            IEventBus eventBus = EngineContext.Current.Resolve<IEventBus>();
            if (eventBus != null)
            {
                IGirvsAuthorizePermissionService permissionService = new GirvsAuthorizePermissionService();

                var authorizePermissionList = permissionService?.GetAuthorizePermissionList().Result;
                var authorizeDataRules = permissionService?.GetAuthorizeDataRuleList().Result;
                var authorizeEvent = new AuthorizeEvent()
                {
                    AuthorizePermissions = authorizePermissionList,
                    AuthorizeDataRules = authorizeDataRules
                };
                await eventBus.PublishAsync(authorizeEvent);
            }
        }
    }
}
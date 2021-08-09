using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Girvs;
using Girvs.AuthorizePermission.ActionPermission;
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
using Power.EventBus.Permission;
using ZhuoFan.Wb.BasicService.Application.ViewModels.ServiceDataRule;
using ZhuoFan.Wb.BasicService.Application.ViewModels.ServicePermission;
using ZhuoFan.Wb.BasicService.Domain.Commands.ServiceDataRule;
using ZhuoFan.Wb.BasicService.Domain.Commands.ServicePermission;
using ZhuoFan.Wb.BasicService.Domain.Models;
using ZhuoFan.Wb.BasicService.Domain.Repositories;

namespace ZhuoFan.Wb.BasicService.Application.AppService.Achieve
{
    [DynamicWebApi]
    [AllowAnonymous]
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
        public async Task<IList<ServicePermission>> GetFunctionOperateList()
        {
            return await _cacheManager.GetAsync(GirvsEntityCacheDefaults<ServicePermission>.AllCacheKey.Create(),
                async () =>
                    await _servicePermissionRepository.GetAllAsync());
        }


        /// <summary>
        /// 添加需要授权的功能列表
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async void CreateFunctionOperateListAsync([FromForm] List<CreateServicePermissionViewModel> models)
        {
            foreach (var model in models)
            {
                var command =
                    new CreateOrUpdateServicePermissionCommand(model.ServiceName, model.ServiceId, model.Permissions);
                await _bus.SendCommand(command);
            }

            if (_notifications.HasNotifications())
            {
                var errorMessage = _notifications.GetNotificationMessage();
                throw new GirvsException(StatusCodes.Status400BadRequest, errorMessage);
            }
        }

        /// <summary>
        /// 获取需要授权的数据规则列表
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IList<ServiceDataRule>> GetDataRuleList()
        {
            return await _cacheManager.GetAsync(GirvsEntityCacheDefaults<ServiceDataRule>.AllCacheKey.Create(),
                async () =>
                    await _serviceDataRuleRepository.GetAllAsync());
        }

        /// <summary>
        /// 添加需要授权的功能列表
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async void CreateDataRuleListAsync([FromForm] List<CreateServiceDataRuleViewModel> models)
        {
            foreach (var model in models)
            {
                var command =
                    new CreateOrUpdateServiceDataRuleCommand(
                        model.ServiceName, model.ModuleName, model.UserType, model.DataSource, model.FieldName,
                        model.FieldDesc);
                await _bus.SendCommand(command);
            }

            if (_notifications.HasNotifications())
            {
                var errorMessage = _notifications.GetNotificationMessage();
                throw new GirvsException(StatusCodes.Status400BadRequest, errorMessage);
            }
        }

        [HttpGet]
        public async Task InitAuthorization()
        {
            IEventBus eventBus = EngineContext.Current.Resolve<IEventBus>();
            if (eventBus != null)
            {
                IGirvsAuthorizePermissionService permissionService = new GirvsAuthorizePermissionService();

                var authorizePermissionList = permissionService?.GetAuthorizePermissionList().Result;
                var authorizeEvent = new PermissionAuthorizeEvent()
                {
                    PermissionAuthorizes = authorizePermissionList?.Select(x =>
                        new PermissionAuthorize
                        {
                            ServiceId = x.ServiceId,
                            ServiceName = x.ServiceName,
                            Permissions = x.Permissions
                        }).ToList()
                };
                await eventBus.PublishAsync(authorizeEvent);
            }
        }
    }
}
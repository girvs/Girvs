using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BasicManagement.Application.ViewModels.Role;
using BasicManagement.Domain.Commands.Role;
using BasicManagement.Domain.Models;
using BasicManagement.Domain.Repositories;
using Girvs;
using Girvs.AuthorizePermission;
using Girvs.AuthorizePermission.Enumerations;
using Girvs.AutoMapper.Extensions;
using Girvs.Cache;
using Girvs.Driven.Bus;
using Girvs.Driven.Notifications;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Panda.DynamicWebApi.Attributes;

namespace BasicManagement.Application.AppService.Achieve
{
    [DynamicWebApi]
    [ServicePermissionDescriptor("角色管理", "4a4fcf52-7696-47e9-b363-2acdd5735dc8")]
    public class RoleAppService : IRoleAppService
    {
        private readonly IStaticCacheManager _cacheManager;
        private readonly IMediatorHandler _bus;
        private readonly DomainNotificationHandler _notifications;
        private readonly IRoleRepository _roleRepository;

        public RoleAppService(
            IStaticCacheManager cacheManager,
            IMediatorHandler bus,
            INotificationHandler<DomainNotification> notifications,
            IRoleRepository roleRepository
        )
        {
            _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _notifications = (DomainNotificationHandler) notifications;
            _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
        }

        /// <summary>
        /// 根据Id获取角色
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
        [ServiceMethodPermissionDescriptor("浏览", Permission.View)]
        public async Task<RoleDetailViewModel> GetAsync(Guid id)
        {
            var role = await _cacheManager.GetAsync(
                GirvsEntityCacheDefaults<Role>.ByIdCacheKey.Create(id.ToString()),
                async () => await _roleRepository.GetByIdAsync(id));

            if (role == null)
                throw new GirvsException("未找到对应的用户", StatusCodes.Status404NotFound);

            return role.MapToDto<RoleDetailViewModel>();
        }

        /// <summary>
        /// 创建角色
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [ServiceMethodPermissionDescriptor("新增", Permission.Post)]
        public async Task<RoleEditViewModel> CreateAsync(RoleEditViewModel model)
        {
            CreateRoleCommand command = new CreateRoleCommand(model.Name, model.Desc, model.UserIds);
            await _bus.SendCommand(command);
            if (_notifications.HasNotifications())
            {
                var (errorCode, errorMessage) = _notifications.GetNotificationMessage();
                throw new GirvsException(errorMessage, errorCode);
            }

            model.Id = command.Id;
            return model;
        }

        /// <summary>
        /// 更新角色
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut]
        [ServiceMethodPermissionDescriptor("修改", Permission.Edit)]
        public async Task<RoleEditViewModel> UpdateAsync(RoleEditViewModel model)
        {
            UpdateRoleCommand command = new UpdateRoleCommand(model.Id, model.Name, model.Desc, model.UserIds);
            await _bus.SendCommand(command);
            if (_notifications.HasNotifications())
            {
                var (errorCode, errorMessage) = _notifications.GetNotificationMessage();
                throw new GirvsException(errorMessage, errorCode);
            }

            return model;
        }

        /// <summary>
        /// 删除角色
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        [ServiceMethodPermissionDescriptor("删除", Permission.Delete)]
        public async Task DeleteAsync(Guid id)
        {
            DeleteRoleCommand command = new DeleteRoleCommand(id);
            await _bus.SendCommand(command);
            if (_notifications.HasNotifications())
            {
                var (errorCode, errorMessage) = _notifications.GetNotificationMessage();
                throw new GirvsException(errorMessage, errorCode);
            }
        }

        /// <summary>
        /// 获取角色列表
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [ServiceMethodPermissionDescriptor("浏览", Permission.View)]
        public async Task<List<RoleListViewModel>> GetAsync()
        {
            var roles = await _cacheManager.GetAsync(GirvsEntityCacheDefaults<Role>.AllCacheKey, async () =>
                await _roleRepository.GetAllAsync());

            return roles.MapTo<List<RoleListViewModel>>();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Girvs;
using Girvs.AuthorizePermission;
using Girvs.AuthorizePermission.Enumerations;
using Girvs.AutoMapper.Extensions;
using Girvs.Cache.Caching;
using Girvs.Driven.Bus;
using Girvs.Driven.Notifications;
using JetBrains.Annotations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Panda.DynamicWebApi.Attributes;
using ZhuoFan.Wb.BasicService.Application.ViewModels.Role;
using ZhuoFan.Wb.BasicService.Application.ViewModels.User;
using ZhuoFan.Wb.BasicService.Domain;
using ZhuoFan.Wb.BasicService.Domain.Commands.BasalPermission;
using ZhuoFan.Wb.BasicService.Domain.Commands.Role;
using ZhuoFan.Wb.BasicService.Domain.Models;
using ZhuoFan.Wb.BasicService.Domain.Repositories;

namespace ZhuoFan.Wb.BasicService.Application.AppService.Achieve
{
    [DynamicWebApi]
    [Authorize(AuthenticationSchemes = GirvsAuthenticationScheme.GirvsJwt)]
    [ServicePermissionDescriptor("角色管理", "4a4fcf52-7696-47e9-b363-2acdd5735dc8")]
    public class RoleAppService : IRoleAppService
    {
        private readonly IStaticCacheManager _cacheManager;
        private readonly IMediatorHandler _bus;
        private readonly DomainNotificationHandler _notifications;
        private readonly IRoleRepository _roleRepository;
        private readonly IPermissionRepository _permissionRepository;

        public RoleAppService(
            IStaticCacheManager cacheManager,
            IMediatorHandler bus,
            INotificationHandler<DomainNotification> notifications,
            IRoleRepository roleRepository,
            [NotNull] IPermissionRepository permissionRepository
        )
        {
            _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _notifications = (DomainNotificationHandler)notifications;
            _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
            _permissionRepository = permissionRepository ?? throw new ArgumentNullException(nameof(permissionRepository));
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
        public async Task<RoleEditViewModel> CreateAsync([FromForm] RoleEditViewModel model)
        {
            var command = new CreateRoleCommand(model.Name, model.Desc, model.UserIds);
            await _bus.SendCommand(command);
            if (_notifications.HasNotifications())
            {
                var errorMessage = _notifications.GetNotificationMessage();
                throw new GirvsException(StatusCodes.Status400BadRequest, errorMessage);
            }

            model.Id = command.Id;
            return model;
        }

        /// <summary>
        /// 更新角色
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut("{id}")]
        [ServiceMethodPermissionDescriptor("修改", Permission.Edit)]
        public async Task<RoleEditViewModel> UpdateAsync(Guid id, [FromForm] RoleEditViewModel model)
        {
            var command = new UpdateRoleCommand(model.Id.Value, model.Name, model.Desc, model.UserIds);
            await _bus.SendCommand(command);
            if (_notifications.HasNotifications())
            {
                var errorMessage = _notifications.GetNotificationMessage();
                throw new GirvsException(StatusCodes.Status400BadRequest, errorMessage);
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
                var errorMessage = _notifications.GetNotificationMessage();
                throw new GirvsException(StatusCodes.Status400BadRequest, errorMessage);
            }
        }

        
        /// <summary>
        /// 批量删除角色
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpDelete("batch")]
        public async Task DeleteAsync(IList<Guid> ids)
        {
            var batchDeleteCommand = new BatchDeleteRoleCommand(ids);
            await _bus.SendCommand(batchDeleteCommand);
            if (_notifications.HasNotifications())
            {
                var errorMessage = _notifications.GetNotificationMessage();
                throw new GirvsException(StatusCodes.Status400BadRequest, errorMessage);
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
            var roles = await _cacheManager.GetAsync(GirvsEntityCacheDefaults<Role>.AllCacheKey.Create(), async () =>
                await _roleRepository.GetAllAsync());

            return roles?.MapTo<List<RoleListViewModel>>();
        }

        /// <summary>
        /// 添加角色用户
        /// </summary>
        /// <param name="model"></param>
        /// <exception cref="GirvsException"></exception>
        [HttpPost("{roleId}/User")]
        [ServiceMethodPermissionDescriptor("用户管理", Permission.Post_Extend1)]
        public async Task AddRoleUser(Guid roleId, [FromForm] EditRoleUserViewModel model)
        {
            var command = new AddRoleUserCommand(
                roleId,
                model.UserIds
            );

            await _bus.SendCommand(command);

            if (_notifications.HasNotifications())
            {
                var errorMessage = _notifications.GetNotificationMessage();
                throw new GirvsException(StatusCodes.Status400BadRequest, errorMessage);
            }
        }

        /// <summary>
        /// 删除角色用户
        /// </summary>
        /// <param name="model"></param>
        /// <exception cref="GirvsException"></exception>
        [HttpDelete("{roleId}/User")]
        [ServiceMethodPermissionDescriptor("用户管理", Permission.Post_Extend1)]
        public async Task DeleteRoleUser(Guid roleId, [FromForm] EditRoleUserViewModel model)
        {
            var command = new DeleteRoleUserCommand(
                roleId,
                model.UserIds
            );

            await _bus.SendCommand(command);

            if (_notifications.HasNotifications())
            {
                var errorMessage = _notifications.GetNotificationMessage();
                throw new GirvsException(StatusCodes.Status400BadRequest, errorMessage);
            }
        }

        /// <summary>
        /// 获取角色下指定的用户集合
        /// </summary>
        /// <param name="roleId"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpGet("{roleId}")]
        [ServiceMethodPermissionDescriptor("用户管理", Permission.Post_Extend1)]
        public async Task<List<UserQueryListViewModel>> GetRoleUsers(Guid roleId)
        {
            var role = await _roleRepository.GetRoleByIdIncludeUsersAsync(roleId);
            if (role == null)
            {
                throw new GirvsException(StatusCodes.Status404NotFound, "未找到相应数据");
            }

            return role.Users.MapTo<List<UserQueryListViewModel>>();
        }
        
         /// <summary>
        /// 获取指定角色的功能菜单操作权限
        /// </summary>
        /// <param name="roleId"></param>
        /// <returns></returns>
        [HttpGet("{roleId}")]
        [ServiceMethodPermissionDescriptor("权限管理", Permission.Read)]
        public async Task<List<AuthorizePermissionModel>> GetRolePermission(Guid roleId)
        {
            var roleBasalPermissions = await _permissionRepository.GetWhereAsync(x =>
                x.AppliedObjectType == PermissionAppliedObjectType.Role &&
                x.ValidateObjectType == PermissionValidateObjectType.FunctionMenu && x.AppliedID == roleId);
            var mergeBasalPermissions = PermissionHelper.MergeValidateObjectTypePermission(roleBasalPermissions);

            return mergeBasalPermissions.Select(p => new AuthorizePermissionModel()
            {
                ServiceId = p.AppliedObjectID,
                ServiceName = string.Empty,
                Permissions = PermissionHelper.ConvertPermissionToString(p).ToDictionary(x => x, x => x)
            }).ToList();
        }

        /// <summary>
        /// 保存指定角色的权限
        /// </summary>
        /// <param name="models"></param>
        /// <returns></returns>
        [HttpPost("{roleId}")]
        [ServiceMethodPermissionDescriptor("权限管理", Permission.Read)]
        public async Task SaveRolePermission(Guid roleId, [FromForm] List<AuthorizePermissionModel> models)
        {
            var role = await _roleRepository.GetByIdAsync(roleId);

            if (role == null)
            {
                throw new GirvsException("未找到对应的角色", StatusCodes.Status404NotFound);
            }

            var command = new SavePermissionCommand(
                roleId,
                PermissionAppliedObjectType.Role,
                PermissionValidateObjectType.FunctionMenu,
                models.Select(x => new ObjectPermission()
                {
                    AppliedObjectID = x.ServiceId,
                    PermissionOpeation =
                        PermissionHelper.ConvertStringToPermission(x.Permissions.Select(x => x.Value).ToList())
                }).ToList());

            await _bus.SendCommand(command);

            if (_notifications.HasNotifications())
            {
                var errorMessage = _notifications.GetNotificationMessage();
                throw new GirvsException(StatusCodes.Status400BadRequest, errorMessage);
            }
        }
    }
}
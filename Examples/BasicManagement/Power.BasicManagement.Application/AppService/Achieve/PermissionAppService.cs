using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Girvs.Domain.Caching.Interface;
using Girvs.Domain.Configuration;
using Girvs.Domain.Driven.Bus;
using Girvs.Domain.Enumerations;
using Girvs.Domain.GirvsAuthorizePermission;
using Girvs.Domain.Infrastructure;
using Girvs.Domain.Managers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Panda.DynamicWebApi.Attributes;
using Power.BasicManagement.Application.ViewModels.Permission;
using Power.BasicManagement.Domain.Commands.BasalPermission;
using Power.BasicManagement.Domain.Enumerations;
using Power.BasicManagement.Domain.Extensions;
using Power.BasicManagement.Domain.Models;
using Power.BasicManagement.Domain.Repositories;

namespace Power.BasicManagement.Application.AppService.Achieve
{
    [DynamicWebApi]
    [Authorize]
    [ServicePermissionDescriptor("权限管理", "152395a5-160a-4f7e-a565-e06d6a13e99a")]
    public class PermissionAppService : IPermissionAppService, IServiceMethodPermissionCompare
    {
        private readonly IPermissionRepository _permissionRepository;
        private readonly IStaticCacheManager _staticCacheManager;
        private readonly GirvsConfig _config;
        private readonly IMediatorHandler _bus;
        private readonly IServicePermissionAuthorizeStore _servicePermissionAuthorizeStore;
        private readonly string _keyPrefix;

        public PermissionAppService(
            IPermissionRepository permissionRepository,
            IStaticCacheManager staticCacheManager,
            GirvsConfig config,
            IMediatorHandler bus,
            IServicePermissionAuthorizeStore servicePermissionAuthorizeStore
        )
        {
            _permissionRepository =
                permissionRepository ?? throw new ArgumentNullException(nameof(permissionRepository));
            _staticCacheManager = staticCacheManager ?? throw new ArgumentNullException(nameof(staticCacheManager));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _servicePermissionAuthorizeStore = servicePermissionAuthorizeStore ?? throw new ArgumentNullException(nameof(servicePermissionAuthorizeStore));
            _keyPrefix = $"{AppDomain.CurrentDomain.FriendlyName}:PermissionCompare:functionId";
        }

        /// <summary>
        /// 获取当前用户的权限
        /// </summary>
        /// <returns></returns>
        [HttpGet("CurrentUserPermission")]
        public async Task<List<PermissionByCurrentUserViewModel>> Get()
        {
            var userType = EngineContext.Current.GetCurrentClaimByName("UserType")?.Value;
            if (!string.IsNullOrEmpty(userType) && userType == "SuperAdmin")
            {
                var ps = await GetAuthorizePermissionList();
                return ps.Select(x => new PermissionByCurrentUserViewModel()
                {
                    AppliedObjectID = x.ServiceId,
                    ValidateObjectType = PermissionValidateObjectType.FunctionMenu,
                    PermissionStr = x.Permissions.Values.ToList()
                }).ToList();
            }


            var currentUser = await EngineContext.Current.GetCurrentUser();
            var currentUserRole = currentUser.UserRoles.Select(x => x.RoleId).ToArray();
            var userBasalPermissions = await _permissionRepository.GetUserPermissionLimit(currentUser.Id);
            var roleBasalPermissions = await _permissionRepository.GetRoleListPermissionLimit(currentUserRole);
            var mergeBasalPermissions = userBasalPermissions.Union(roleBasalPermissions).ToList();
            mergeBasalPermissions = MergeValidateObjectTypePermission(mergeBasalPermissions);

            return mergeBasalPermissions.Select(bp => new PermissionByCurrentUserViewModel()
            {
                AppliedObjectID = bp.AppliedObjectID,
                ValidateObjectType = bp.ValidateObjectType,
                PermissionStr = ConvertPermissionToString(bp)
            }).ToList();
        }

        /// <summary>
        /// 获取当前用户指定功能模块权限
        /// </summary>
        /// <param name="appliedObjectId"></param>
        /// <returns></returns>
        [HttpGet("CurrentUserAppliedObjectPermission/{appliedObjectId}")]
        public async Task<List<string>> Get(Guid appliedObjectId)
        {
            var list = await Get(); 
            var p = list.FirstOrDefault(x => x.AppliedObjectID == appliedObjectId);
            if (p != null)
            {
                return p.PermissionStr;
            }
            return new List<string>();
        }


        /// <summary>
        /// 根据查询条件获取权限
        /// </summary>
        /// <param name="queryViewModel"></param>
        /// <returns></returns>
        [HttpGet]
        [ServiceMethodPermissionDescriptor("修改", Permission.Edit)]
        public async Task<List<PermissionDetailViewModel>> Get(PermissionQueryViewModel queryViewModel)
        {
            var ps = queryViewModel.PermissionAppliedObjectType == PermissionAppliedObjectType.Role
                ? await _permissionRepository.GetRolePermissionLimit(queryViewModel.AppliedID)
                : await _permissionRepository.GetUserPermissionLimit(queryViewModel.AppliedID);

            return ps.Select(p => new PermissionDetailViewModel()
            {
                AppliedID = p.AppliedID,
                AppliedObjectID = p.AppliedObjectID,
                AppliedObjectType = p.AppliedObjectType,
                ValidateObjectType = p.ValidateObjectType,
                PermissionStr = ConvertPermissionToString(p)
            }).ToList();
        }

        /// <summary>
        /// 保存设置权限
        /// </summary>
        /// <param name="saveViewModels"></param>
        /// <returns></returns>
        [HttpPost]
        [ServiceMethodPermissionDescriptor("修改", Permission.Edit)]
        public async Task Update(SavePermisssionEditViewModel saveViewModels)
        {
            var ps = saveViewModels.Ps.Select(vp => new BasalPermissionDto()
            {
                AppliedObjectID = vp.AppliedObjectID,
                ValidateObjectID = ConvertStringToPermissionValue(vp.PermissionStr),
                Permissions = ConvertStringToPermission(vp.PermissionStr)
            }).ToList();

            var command = new SaveBasalPermissionCommand(
                PermissionValidateObjectType.FunctionMenu,
                saveViewModels.AppliedObjectType,
                saveViewModels.AppliedId,
                ps
            );
            await _bus.SendCommand(command);
        }

        private int ConvertStringToPermissionValue(List<string> permissionStrs)
        {
            var validateObjectID = Permission.Undefined;
            foreach (var permissionStr in permissionStrs)
            {
                Permission c = (Permission) Enum.Parse(typeof(Permission), permissionStr, true);
                validateObjectID = validateObjectID | c;
            }

            return (int) validateObjectID;
        }

        private List<Permission> ConvertStringToPermission(List<string> permissionStrs)
        {
            List<Permission> permissions = new List<Permission>();
            foreach (var permissionStr in permissionStrs)
            {
                var permission = (Permission) Enum.Parse(typeof(Permission), permissionStr, true);
                permissions.Add(permission);
            }

            return permissions;
        }

        private List<string> ConvertPermissionToString(BasalPermission basalPermission)
        {
            var list = new List<string>();
            foreach (Permission value in typeof(Permission).GetEnumValues())
            {
                if (basalPermission.GetBit(value))
                {
                    list.Add(value.ToString());
                }
            }

            return list;
        }

        private List<BasalPermission> MergeValidateObjectTypePermission(List<BasalPermission> ps)
        {
            var psGroup = ps.GroupBy(p => p.AppliedObjectID).ToList();
            var newPs = new List<BasalPermission>();
            foreach (var item in psGroup)
            {
                if (!item.Any()) continue;
                var allowMask = Permission.Undefined;
                var denyMask = Permission.Undefined;
                foreach (var p in item)
                {
                    allowMask |= p.AllowMask;
                    denyMask |= p.DenyMask;
                }

                var m = item.FirstOrDefault();
                m.AllowMask = allowMask;
                m.DenyMask = denyMask;
                newPs.Add(m);
            }

            return newPs;
        }

        [NonAction]
        [AllowAnonymous]
        public async Task<bool> PermissionCompare(Guid functionId, Permission permission)
        {
            if (!_config.UseServiceMethodPermissionCompare)
            {
                return true;
            }

            var userType = EngineContext.Current.GetCurrentClaimByName("UserType")?.Value;
            if (!string.IsNullOrEmpty(userType) && userType == nameof(UserType.SuperAdmin))
            {
                return true;
            }

            var key = $"{_keyPrefix}:{functionId}";
            var funPermissions = await _staticCacheManager.GetAsync(
                key,
                async () => await Get(functionId),
                30);
            return funPermissions != null
                   && funPermissions.Contains(permission.ToString());
        }

        /// <summary>
        /// 获取需要的授权列表
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("AuthorizePermissionList")]
        public async Task<List<AuthorizePermissionModel>> GetAuthorizePermissionList()
        {
            return await _servicePermissionAuthorizeStore.GetList();
        }
    }
}
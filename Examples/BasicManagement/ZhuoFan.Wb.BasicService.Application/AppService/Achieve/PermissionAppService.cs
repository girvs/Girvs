using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Girvs.AuthorizePermission;
using Girvs.AuthorizePermission.Enumerations;
using Girvs.BusinessBasis.Repositories;
using Girvs.Cache.Caching;
using Girvs.Driven.Bus;
using Girvs.Extensions;
using Girvs.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Panda.DynamicWebApi.Attributes;
using ZhuoFan.Wb.BasicService.Application.ViewModels.Permission;
using ZhuoFan.Wb.BasicService.Domain.Commands.BasalPermission;
using ZhuoFan.Wb.BasicService.Domain.Models;
using ZhuoFan.Wb.BasicService.Domain.Repositories;

namespace ZhuoFan.Wb.BasicService.Application.AppService.Achieve
{
    [DynamicWebApi]
    [Authorize]
    [ServicePermissionDescriptor("权限管理", "152395a5-160a-4f7e-a565-e06d6a13e99a")]
    public class PermissionAppService : IPermissionAppService
    {
        private readonly IPermissionRepository _permissionRepository;
        private readonly IStaticCacheManager _staticCacheManager;
        private readonly IMediatorHandler _bus;
        private readonly IUserRepository _userRepository;
        private readonly IRepository<ServicePermission> _servicePermission;
        private readonly string _keyPrefix;

        public PermissionAppService(
            IPermissionRepository permissionRepository,
            IStaticCacheManager staticCacheManager,
            IMediatorHandler bus,
            IUserRepository userRepository,
            IRepository<ServicePermission> servicePermission
        )
        {
            _permissionRepository =
                permissionRepository ?? throw new ArgumentNullException(nameof(permissionRepository));
            _staticCacheManager = staticCacheManager ?? throw new ArgumentNullException(nameof(staticCacheManager));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _servicePermission = servicePermission ?? throw new ArgumentNullException(nameof(servicePermission));
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
                var ps = await GetServicePermission();
                return ps.Select(x => new PermissionByCurrentUserViewModel()
                {
                    AppliedObjectID = x.ServiceId,
                    ValidateObjectType = PermissionValidateObjectType.FunctionMenu,
                    PermissionStr = x.Permissions.Values.ToList()
                }).ToList();
            }

            var currentUser =
                await _userRepository.GetUserByIdIncludeRolesAsync(EngineContext.Current.ClaimManager.GetUserId()
                    .ToHasGuid().Value);

            var currentUserRole = currentUser.Roles.Select(x => x.Id).ToArray();
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

        /// <summary>
        /// 获取需要的授权列表
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("AuthorizePermissionList")]
        public async Task<dynamic> GetAuthorizePermissionList()
        {
            return await GetServicePermission();
        }

        private Task<List<ServicePermission>> GetServicePermission()
        {
            return _staticCacheManager.GetAsync(
                GirvsEntityCacheDefaults<ServicePermission>.AllCacheKey.Create(),
                async () => await _servicePermission.GetAllAsync());
        }
    }
}
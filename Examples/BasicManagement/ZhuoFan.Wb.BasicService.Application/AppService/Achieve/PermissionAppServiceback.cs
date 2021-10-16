// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;
// using Girvs;
// using Girvs.AuthorizePermission;
// using Girvs.AuthorizePermission.Enumerations;
// using Girvs.Driven.Bus;
// using Girvs.Driven.Notifications;
// using Girvs.Extensions;
// using Girvs.Infrastructure;
// using MediatR;
// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Http;
// using Microsoft.AspNetCore.Mvc;
// using Panda.DynamicWebApi.Attributes;
// using ZhuoFan.Wb.BasicService.Domain.Commands.BasalPermission;
// using ZhuoFan.Wb.BasicService.Domain.Commands.User;
// using ZhuoFan.Wb.BasicService.Domain.Models;
// using ZhuoFan.Wb.BasicService.Domain.Repositories;
//
// namespace ZhuoFan.Wb.BasicService.Application.AppService.Achieve
// {
//     [DynamicWebApi]
//     [Authorize(AuthenticationSchemes = GirvsAuthenticationScheme.GirvsJwt)]
//     [ServicePermissionDescriptor("权限管理", "152395a5-160a-4f7e-a565-e06d6a13e99a")]
//     public class PermissionAppService : IPermissionAppService
//     {
//         private readonly IPermissionRepository _permissionRepository;
//         private readonly IMediatorHandler _bus;
//         private readonly IUserRepository _userRepository;
//         private readonly IRoleRepository _roleRepository;
//         private readonly IAuthorizationService _authorizationService;
//         private readonly DomainNotificationHandler _notifications;
//
//         public PermissionAppService(
//             IPermissionRepository permissionRepository,
//             IMediatorHandler bus,
//             IUserRepository userRepository,
//             IRoleRepository roleRepository,
//             INotificationHandler<DomainNotification> notifications,
//             IAuthorizationService authorizationService
//         )
//         {
//             _permissionRepository =
//                 permissionRepository ?? throw new ArgumentNullException(nameof(permissionRepository));
//             _bus = bus ?? throw new ArgumentNullException(nameof(bus));
//             _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
//             _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
//             _authorizationService =
//                 authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
//             _notifications = (DomainNotificationHandler)notifications;
//         }
//
//
//         /// <summary>
//         /// 获取当前用户的权限、包含功能菜单以及数据权限
//         /// </summary>
//         /// <returns></returns>
//         [HttpGet]
//         public async Task<AuthorizeModel> GetCurrentUserAuthorization()
//         {
//             var currentUserId = EngineContext.Current.ClaimManager.GetUserId().ToHasGuid().Value;
//             var currentUser = await _userRepository.GetUserByIdIncludeRoleAndDataRule(currentUserId);
//
//             var currentUserRole = currentUser.Roles.Select(x => x.Id).ToArray();
//             var userBasalPermissions = await _permissionRepository.GetUserPermissionLimit(currentUser.Id);
//             var roleBasalPermissions = await _permissionRepository.GetRoleListPermissionLimit(currentUserRole);
//             var mergeBasalPermissions = userBasalPermissions.Union(roleBasalPermissions).ToList();
//             mergeBasalPermissions = PermissionHelper.MergeValidateObjectTypePermission(mergeBasalPermissions);
//
//
//             var permissionViewModels =
//                 mergeBasalPermissions.Select(p => new AuthorizePermissionModel()
//                 {
//                     ServiceId = p.AppliedObjectID,
//                     Permissions = PermissionHelper.ConvertPermissionToString(p).ToDictionary(x => x, x => x)
//                 }).ToList();
//
//             var authorizeDataRuleModels = PermissionHelper.ConvertAuthorizeDataRuleModels(currentUser.RulesList);
//
//             return new AuthorizeModel()
//             {
//                 AuthorizePermissions = permissionViewModels,
//                 AuthorizeDataRules = authorizeDataRuleModels
//             };
//         }
//
//
//         /// <summary>
//         /// 获取指定角色的功能菜单操作权限
//         /// </summary>
//         /// <param name="roleId"></param>
//         /// <returns></returns>
//         [HttpGet("{roleId}")]
//         [ServiceMethodPermissionDescriptor("角色权限", Permission.Read)]
//         public async Task<List<AuthorizePermissionModel>> GetRolePermission(Guid roleId)
//         {
//             var roleBasalPermissions = await _permissionRepository.GetWhereAsync(x =>
//                 x.AppliedObjectType == PermissionAppliedObjectType.Role &&
//                 x.ValidateObjectType == PermissionValidateObjectType.FunctionMenu && x.AppliedID == roleId);
//             var mergeBasalPermissions = PermissionHelper.MergeValidateObjectTypePermission(roleBasalPermissions);
//
//             var authorizationList = await _authorizationService.GetFunctionOperateList();
//
//             return mergeBasalPermissions.Select(p =>
//             {
//                 var authorizationFunction = authorizationList.FirstOrDefault(x => x.ServiceId == p.AppliedObjectID);
//
//                 var permissions = new Dictionary<string, string>();
//                 foreach (var pStr in PermissionHelper.ConvertPermissionToString(p))
//                 {
//                     var name = authorizationFunction.Permissions.FirstOrDefault(x => x.Value == pStr).Key;
//                     if (!string.IsNullOrEmpty(name))
//                     {
//                         permissions.Add(name, pStr);
//                     }
//                 }
//
//                 return new AuthorizePermissionModel()
//                 {
//                     ServiceId = p.AppliedObjectID,
//                     ServiceName = authorizationFunction?.ServiceName,
//                     Permissions = permissions
//                 };
//             }).ToList();
//         }
//
//         /// <summary>
//         /// 保存指定角色的权限
//         /// </summary>
//         /// <param name="models"></param>
//         /// <returns></returns>
//         [HttpPost("{roleId}")]
//         [ServiceMethodPermissionDescriptor("角色授权", Permission.Post)]
//         public async Task SaveRolePermission(Guid roleId, [FromForm] List<AuthorizePermissionModel> models)
//         {
//             var role = await _roleRepository.GetByIdAsync(roleId);
//
//             if (role == null)
//             {
//                 throw new GirvsException("未找到对应的角色", StatusCodes.Status404NotFound);
//             }
//
//             var command = new SavePermissionCommand(
//                 roleId,
//                 PermissionAppliedObjectType.Role,
//                 PermissionValidateObjectType.FunctionMenu,
//                 models.Select(x => new ObjectPermission()
//                 {
//                     AppliedObjectID = x.ServiceId,
//                     PermissionOpeation =
//                         PermissionHelper.ConvertStringToPermission(x.Permissions.Select(x => x.Value).ToList())
//                 }).ToList());
//
//             await _bus.SendCommand(command);
//
//             if (_notifications.HasNotifications())
//             {
//                 var errorMessage = _notifications.GetNotificationMessage();
//                 throw new GirvsException(StatusCodes.Status400BadRequest, errorMessage);
//             }
//         }
//
//         /// <summary>
//         /// 获取指定用户的数据权限
//         /// </summary>
//         /// <param name="userId"></param>
//         /// <returns></returns>
//         [HttpGet("{userId}")]
//         [ServiceMethodPermissionDescriptor("用户数据权限", Permission.Read_Extend1)]
//         public async Task<List<AuthorizeDataRuleModel>> GetUserDataRule(Guid userId)
//         {
//             var user = await _userRepository.GetUserByIdIncludeRoleAndDataRule(userId);
//             if (user == null)
//                 throw new GirvsException("未找到对应的用户", StatusCodes.Status404NotFound);
//
//             return PermissionHelper.ConvertAuthorizeDataRuleModels(user.RulesList);
//         }
//
//         /// <summary>
//         /// 保存指定用户的数据授权
//         /// </summary>
//         /// <param name="models"></param>
//         /// <returns></returns>
//         [HttpPost("{userId}")]
//         [ServiceMethodPermissionDescriptor("用户数据授权", Permission.Post_Extend1)]
//         public async Task SaveUserDataRule(Guid userId, [FromForm] List<AuthorizeDataRuleModel> models)
//         {
//             var userRules = new List<UserRule>();
//             foreach (var authorizeDataRule in models)
//             {
//                 foreach (var dataRuleFieldModel in authorizeDataRule.AuthorizeDataRuleFieldModels)
//                 {
//                     var userRule = new UserRule()
//                     {
//                         EntityTypeName = authorizeDataRule.EntityTypeName,
//                         EntityDesc = authorizeDataRule.EntityDesc,
//                         FieldName = dataRuleFieldModel.FieldName,
//                         FieldDesc = dataRuleFieldModel.FieldDesc,
//                         FieldType = dataRuleFieldModel.FieldType,
//                         FieldValue = dataRuleFieldModel.FieldValue,
//                         ExpressionType = dataRuleFieldModel.ExpressionType
//                     };
//
//                     userRules.Add(userRule);
//                 }
//             }
//
//             var command = new UpdateUserRuleCommand(userId, userRules);
//             await _bus.SendCommand(command);
//
//             if (_notifications.HasNotifications())
//             {
//                 var errorMessage = _notifications.GetNotificationMessage();
//                 throw new GirvsException(StatusCodes.Status400BadRequest, errorMessage);
//             }
//         }
//     }
// }
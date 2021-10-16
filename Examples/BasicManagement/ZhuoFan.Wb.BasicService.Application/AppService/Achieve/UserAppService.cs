using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Girvs;
using Girvs.AuthorizePermission;
using Girvs.AuthorizePermission.Enumerations;
using Girvs.AuthorizePermission.Extensions;
using Girvs.AutoMapper.Extensions;
using Girvs.Cache.Caching;
using Girvs.Driven.Bus;
using Girvs.Driven.Notifications;
using Girvs.Extensions;
using Girvs.Infrastructure;
using JetBrains.Annotations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Panda.DynamicWebApi.Attributes;
using ZhuoFan.Wb.BasicService.Application.ViewModels.Role;
using ZhuoFan.Wb.BasicService.Application.ViewModels.User;
using ZhuoFan.Wb.BasicService.Domain;
using ZhuoFan.Wb.BasicService.Domain.Commands.User;
using ZhuoFan.Wb.BasicService.Domain.Enumerations;
using ZhuoFan.Wb.BasicService.Domain.Extensions;
using ZhuoFan.Wb.BasicService.Domain.Models;
using ZhuoFan.Wb.BasicService.Domain.Queries;
using ZhuoFan.Wb.BasicService.Domain.Repositories;
using ILogger = Serilog.ILogger;

namespace ZhuoFan.Wb.BasicService.Application.AppService.Achieve
{
    [DynamicWebApi]
    [Authorize(AuthenticationSchemes = GirvsAuthenticationScheme.GirvsJwt)]
    [ServicePermissionDescriptor("用户管理", "587752d1-7937-4e6a-a035-ee013e58b99b")]
    public class UserAppService : IUserAppService
    {
        private readonly IStaticCacheManager _cacheManager;
        private readonly IMediatorHandler _bus;
        private readonly DomainNotificationHandler _notifications;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UserAppService> _logger;
        private readonly IPermissionRepository _permissionRepository;
        private readonly IAuthorizationService _authorizationService;

        public UserAppService(
            IStaticCacheManager cacheManager,
            IMediatorHandler bus,
            INotificationHandler<DomainNotification> notifications,
            IUserRepository userRepository,
            [NotNull] ILogger<UserAppService> logger,
            [NotNull] IPermissionRepository permissionRepository,
            [NotNull] IAuthorizationService authorizationService
        )
        {
            _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _notifications = (DomainNotificationHandler) notifications;
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _permissionRepository =
                permissionRepository ?? throw new ArgumentNullException(nameof(permissionRepository));
            _authorizationService =
                authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        }

        /// <summary>
        /// 根据Id获取用户
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet("{id}")]
        [ServiceMethodPermissionDescriptor("浏览", Permission.View)]
        public async Task<UserDetailViewModel> GetAsync(Guid id)
        {
            var user = await _cacheManager.GetAsync(
                GirvsEntityCacheDefaults<User>.ByIdCacheKey.Create(id.ToString()),
                async () => await _userRepository.GetByIdAsync(id));

            if (user == null)
                throw new GirvsException("未找到对应的用户", StatusCodes.Status404NotFound);


            return user.MapToDto<UserDetailViewModel>();
        }

        /// <summary>
        /// 创建用户
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [ServiceMethodPermissionDescriptor("新增", Permission.Post)]
        public async Task<UserEditViewModel> CreateAsync([FromForm] UserEditViewModel model)
        {
            var currentUser = EngineContext.Current.GetCurrentUser();
            if (currentUser == null)
            {
                throw new GirvsException(StatusCodes.Status401Unauthorized, "未登录");
            }

            //如果超级管理员创建的用户，则是特殊用户，如果是租户管理创建的用户，则是普通用户
            var targetUserType =
                currentUser.UserType == UserType.AdminUser ? UserType.SpecialUser : UserType.GeneralUser;

            var command = new CreateUserCommand(
                model.UserAccount,
                model.UserPassword.ToMd5(),
                model.UserName,
                model.ContactNumber,
                model.State,
                targetUserType,
                null
            );

            await _bus.SendCommand(command);
            if (_notifications.HasNotifications())
            {
                var errorMessage = _notifications.GetNotificationMessage();
                throw new GirvsException(StatusCodes.Status400BadRequest, errorMessage);
            }
            else
            {
                model.Id = command.Id;
                return model;
            }
        }

        /// <summary>
        /// 更新用户
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPut("{id}")]
        [ServiceMethodPermissionDescriptor("修改", Permission.Edit)]
        public async Task<UserEditViewModel> UpdateAsync(Guid id, [FromForm] UserEditViewModel model)
        {
            var command = new UpdateUserCommand(
                model.Id.Value,
                model.UserPassword.ToMd5(),
                model.UserName,
                model.ContactNumber,
                model.State,
                model.UserType
            );

            await _bus.SendCommand(command);

            if (_notifications.HasNotifications())
            {
                var errorMessage = _notifications.GetNotificationMessage();
                throw new GirvsException(StatusCodes.Status400BadRequest, errorMessage);
            }

            return model;
        }

        /// <summary>
        /// 删除用户
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete("{id}")]
        [ServiceMethodPermissionDescriptor("删除", Permission.Delete)]
        public async Task DeleteAsync(Guid id)
        {
            var command = new DeleteUserCommand(id);
            await _bus.SendCommand(command);
        }

        /// <summary>
        /// 批量删除用户
        /// </summary>
        /// <param name="ids"></param>
        /// <exception cref="GirvsException"></exception>
        [HttpDelete("batch")]
        public async Task DeleteAsync(IList<Guid> ids)
        {
            var batchDeleteCommand = new BatchDeleteUserCommand(ids);
            await _bus.SendCommand(batchDeleteCommand);
            if (_notifications.HasNotifications())
            {
                var errorMessage = _notifications.GetNotificationMessage();
                throw new GirvsException(StatusCodes.Status400BadRequest, errorMessage);
            }
        }

        /// <summary>
        /// 根据查询条件获取用户
        /// </summary>
        /// <param name="queryModel"></param>
        /// <returns></returns>
        [HttpGet]
        [ServiceMethodPermissionDescriptor("浏览", Permission.View,
            UserType.AdminUser | UserType.TenantAdminUser | UserType.SpecialUser | UserType.GeneralUser)]
        public async Task<UserQueryViewModel> GetAsync(UserQueryViewModel queryModel)
        {
            var query = queryModel.MapToQuery<UserQuery>();
            await _userRepository.GetByQueryAsync(query);

            // var tempQuery = await _cacheManager.GetAsync(
            //     GirvsEntityCacheDefaults<User>.QueryCacheKey.Create(query.GetCacheKey()), async () =>
            //     {
            //         await _userRepository.GetByQueryAsync(query);
            //         return query;
            //     });
            //
            // if (!query.Equals(tempQuery))
            // {
            //     query.RecordCount = tempQuery.RecordCount;
            //     query.Result = tempQuery.Result;
            // }

            return query.MapToQueryDto<UserQueryViewModel, User>();
        }

        /// <summary>
        /// 根据登录名称获取用户
        /// </summary>
        /// <param name="account">登陆名称</param>
        /// <returns></returns>
        // [AllowAnonymous]
        // [HttpGet("{account}")]
        // public async Task<UserDetailViewModel> GetByAccount(string account)
        // {
        //     var user = await _userRepository.GetUserByLoginNameAsync(account);
        //     if (user == null)
        //     {
        //         throw new GirvsException("未找到对应的用户", StatusCodes.Status404NotFound);
        //     }
        //
        //     return user.MapToDto<UserDetailViewModel>();
        // }

        /// <summary>
        /// 根据用户名和密码获取Token
        /// </summary>
        /// <param name="account">登陆用户名</param>
        /// <param name="password">密码</param>
        /// <returns></returns>
        /// <exception cref="GirvsException"></exception>
        // [AllowAnonymous]
        // [HttpGet("{account}/{password}")]
        // public async Task<string> GetToken(string account, string password)
        // {
        //     
        // }

        /// <summary>
        /// 根据用户名和密码获取Token
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost("LoginToken")]
        public async Task<string> GetToken([FromForm] UserLoginViewModel model)
        {
            var user = await _userRepository.GetUserByLoginNameAsync(model.UserAccount);

            if (user == null || user.UserPassword != model.Password.ToMd5())
            {
                throw new GirvsException("用户名或者密码错误，请重新输入");
            }

            if (user.State == DataState.Disable)
            {
                throw new GirvsException("当前用户已被禁用，无法登陆", 568);
            }

            return JwtBearerAuthenticationExtension.GenerateToken(user.Id.ToString(), user.UserName,
                user.TenantId.ToString(),
                user.UserName, user.UserType, IdentityType.ManagerUser);
        }

        /// <summary>
        /// 添加用户角色操作
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpPost("{userId}/Role")]
        [ServiceMethodPermissionDescriptor("角色管理", Permission.Post_Extend1)]
        public async Task AddUserRole(Guid userId, [FromForm] EditUserRoleViewModel model)
        {
            var command = new AddUserRoleCommand(
                userId,
                model.RoleIds
            );

            await _bus.SendCommand(command);

            if (_notifications.HasNotifications())
            {
                var errorMessage = _notifications.GetNotificationMessage();
                throw new GirvsException(StatusCodes.Status400BadRequest, errorMessage);
            }
        }

        /// <summary>
        /// 删除用户角色操作
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpDelete("{userId}/Role")]
        [ServiceMethodPermissionDescriptor("角色管理", Permission.Post_Extend1)]
        public async Task DeleteUserRole(Guid userId, [FromForm] EditUserRoleViewModel model)
        {
            var command = new DeleteUserRoleCommand(
                userId,
                model.RoleIds
            );

            await _bus.SendCommand(command);

            if (_notifications.HasNotifications())
            {
                var errorMessage = _notifications.GetNotificationMessage();
                throw new GirvsException(StatusCodes.Status400BadRequest, errorMessage);
            }
        }

        /// <summary>
        /// 启用用户
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpPatch("{userId}")]
        [ServiceMethodPermissionDescriptor("启用/禁用", Permission.Edit_Extend1)]
        public async Task Enable(Guid userId)
        {
            var command = new ChangeUserStateCommand(userId, DataState.Enable);
            await _bus.SendCommand(command);

            if (_notifications.HasNotifications())
            {
                var errorMessage = _notifications.GetNotificationMessage();
                throw new GirvsException(StatusCodes.Status400BadRequest, errorMessage);
            }
        }

        /// <summary>
        /// 禁用用户
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpPatch("{userId}")]
        [ServiceMethodPermissionDescriptor("启用/禁用", Permission.Edit_Extend1)]
        public async Task Disable(Guid userId)
        {
            var command = new ChangeUserStateCommand(userId, DataState.Disable);
            await _bus.SendCommand(command);

            if (_notifications.HasNotifications())
            {
                var errorMessage = _notifications.GetNotificationMessage();
                throw new GirvsException(StatusCodes.Status400BadRequest, errorMessage);
            }
        }

        /// <summary>
        /// 修改用户密码
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpPatch("{userId}")]
        [ServiceMethodPermissionDescriptor("重置密码", Permission.Edit_Extend2)]
        public async Task ChangeUserPassword(Guid userId, [FromForm] ChangeUserPassworkViewModel model)
        {
            var command = new ChangeUserPassworkCommand(userId, model.NewPassword);
            await _bus.SendCommand(command);

            if (_notifications.HasNotifications())
            {
                var errorMessage = _notifications.GetNotificationMessage();
                throw new GirvsException(StatusCodes.Status400BadRequest, errorMessage);
            }
        }

        /// <summary>
        /// 用户自行修改密码
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpPatch]
        public async Task UserEditPassword([FromForm] UserEditPasswordViewModel model)
        {
            var currentUserId = EngineContext.Current.ClaimManager.GetUserId();
            var command =
                new UserEditPasswordCommand(currentUserId.ToHasGuid().Value, model.OldPassword, model.NewPassword);
            await _bus.SendCommand(command);

            if (_notifications.HasNotifications())
            {
                var errorMessage = _notifications.GetNotificationMessage();
                throw new GirvsException(StatusCodes.Status400BadRequest, errorMessage);
            }
        }

        /// <summary>
        /// 获取用户所包含的角色
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        [HttpGet("{userId}")]
        public async Task<List<RoleListViewModel>> GetUserRoles(Guid userId)
        {
            var user = await _userRepository.GetUserByIdIncludeRolesAsync(userId);
            if (user == null)
            {
                throw new GirvsException(StatusCodes.Status404NotFound, "未找到相应数据");
            }

            return user.Roles.MapTo<List<RoleListViewModel>>();
        }

        /// <summary>
        /// 判断登陆名称是否已存在
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<bool> ExistAccount(string account)
        {
            var user = await _userRepository.GetUserByLoginNameAsync(account);
            return user != null;
        }

        /// <summary>
        /// 批量重置用户登陆密码
        /// </summary>
        /// <param name="model"></param>
        /// <exception cref="GirvsException"></exception>
        [HttpPatch]
        [ServiceMethodPermissionDescriptor("重置密码", Permission.Edit_Extend2)]
        public async Task BatchReSetUserPassword([FromForm] BatchResetUserPasswordViewModel model)
        {
            var command =
                new BatchChangeUserPasswordCommand(model.Ids, model.NewPassword);
            await _bus.SendCommand(command);

            if (_notifications.HasNotifications())
            {
                var errorMessage = _notifications.GetNotificationMessage();
                throw new GirvsException(StatusCodes.Status400BadRequest, errorMessage);
            }
        }

        /// <summary>
        /// 批量启用禁用用户名
        /// </summary>
        /// <param name="model"></param>
        /// <exception cref="GirvsException"></exception>
        [HttpPatch]
        [ServiceMethodPermissionDescriptor("启用/禁用", Permission.Edit_Extend1)]
        public async Task BatchChangeUserState([FromForm] BatchChangeUserStateViewModel model)
        {
            var command =
                new BatchChangeUserStateCommand(model.Ids, model.DataState);
            await _bus.SendCommand(command);

            if (_notifications.HasNotifications())
            {
                var errorMessage = _notifications.GetNotificationMessage();
                throw new GirvsException(StatusCodes.Status400BadRequest, errorMessage);
            }
        }


        [NonAction]
        public async Task<AuthorizeModel> GetCurrentUserAuthorization(Guid userId)
        {
            var allAuthorizePermissions = (await _authorizationService.GetFunctionOperateList()).ToList();
            var allAuthorizeDataRules = (await _authorizationService.GetDataRuleList()).ToList();
            var user = await _userRepository.GetUserByIdIncludeRoleAndDataRule(userId);
            //如果当前用户类型为管理员或者租户管理员，则直接返回
            if (user.UserType is UserType.AdminUser or UserType.TenantAdminUser)
            {
                var result = new AuthorizeModel
                {
                    AuthorizePermissions = allAuthorizePermissions,
                    AuthorizeDataRules = allAuthorizeDataRules
                };
                return result;
            }

            var currentUserRole = user.Roles.Select(x => x.Id).ToArray();
            var userBasalPermissions = await _permissionRepository.GetUserPermissionLimit(user.Id);
            var roleBasalPermissions = await _permissionRepository.GetRoleListPermissionLimit(currentUserRole);
            var mergeBasalPermissions = userBasalPermissions.Union(roleBasalPermissions).ToList();
            mergeBasalPermissions = PermissionHelper.MergeValidateObjectTypePermission(mergeBasalPermissions);


            var permissionViewModels =
                mergeBasalPermissions
                    .Where(p => allAuthorizePermissions.Any(a => a.ServiceId == p.AppliedObjectID))
                    .Select(p =>
                    {
                        var currentServicePermission =
                            allAuthorizePermissions.FirstOrDefault(x => x.ServiceId == p.AppliedObjectID);
                        if (currentServicePermission != null)
                        {
                            var convertPermissionList = PermissionHelper.ConvertPermissionToString(p);
                            var permissions = new Dictionary<string, string>();

                            foreach (var keyValue in convertPermissionList)
                            {
                                foreach (var keyValuePair in currentServicePermission.Permissions.Where(keyValuePair =>
                                    keyValuePair.Value == keyValue))
                                {
                                    permissions.TryAdd(keyValuePair.Key, keyValue);
                                }
                            }

                            return new AuthorizePermissionModel()
                            {
                                ServiceName = currentServicePermission?.ServiceName,
                                ServiceId = p.AppliedObjectID,
                                Permissions = permissions
                            };
                        }

                        return new AuthorizePermissionModel();
                    }).ToList();

            var authorizeDataRuleModels = PermissionHelper.ConvertAuthorizeDataRuleModels(user.RulesList);

            return new AuthorizeModel()
            {
                AuthorizePermissions = permissionViewModels,
                AuthorizeDataRules = authorizeDataRuleModels
            };
        }

        /// <summary>
        /// 获取当前用户的权限、包含功能菜单以及数据权限
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public Task<AuthorizeModel> GetCurrentUserAuthorization()
        {
            var currentUserId = EngineContext.Current.ClaimManager.GetUserId().ToHasGuid().Value;
            return GetCurrentUserAuthorization(currentUserId);
        }

        /// <summary>
        /// 获取指定用户的数据权限
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        [HttpGet("{userId}")]
        [ServiceMethodPermissionDescriptor("数据配置", Permission.Catalog_Read,
            UserType.GeneralUser | UserType.TenantAdminUser)]
        public async Task<List<UserDataRuleListViewModel>> GetUserDataRule(Guid userId)
        {
            var user = await _userRepository.GetUserByIdIncludeRoleAndDataRule(userId);
            if (user == null)
                throw new GirvsException("未找到对应的用户", StatusCodes.Status404NotFound);


            var rulesList = user.RulesList;
            var authorizeDataRuleModels = new List<UserDataRuleListViewModel>();

            if (rulesList == null || !rulesList.Any()) return authorizeDataRuleModels;

            foreach (var authorizeDataRule in rulesList)
            {
                var existReturnReuslt =
                    authorizeDataRuleModels.FirstOrDefault(
                        x => x.EntityTypeName == authorizeDataRule.EntityTypeName) ??
                    new UserDataRuleListViewModel
                    {
                        EntityTypeName = authorizeDataRule.EntityTypeName,
                        EntityDesc = authorizeDataRule.EntityDesc
                    };

                existReturnReuslt.DataRuleListFieldViewModels.Add(new UserDataRuleListFieldViewModel
                {
                    ExpressionType = authorizeDataRule.ExpressionType,
                    FieldType = authorizeDataRule.FieldType,
                    FieldName = authorizeDataRule.FieldName,
                    FieldValue = authorizeDataRule.FieldValue,
                    FieldDesc = authorizeDataRule.FieldDesc,
                    FieldValueText = authorizeDataRule.FieldValueText
                });

                authorizeDataRuleModels.Add(existReturnReuslt);
            }

            return authorizeDataRuleModels;
        }


        [HttpPost("{userId}")]
        [ServiceMethodPermissionDescriptor("数据配置", Permission.Catalog_Read,
            UserType.GeneralUser | UserType.TenantAdminUser)]
        public async Task SaveUserDataRule(Guid userId, [FromForm] List<SaveUserDataRuleViewModel> models)
        {
            if (models == null || models.Count == 0)
            {
                _logger.LogDebug("没有接到任何要保存的数据，默认为清除所有的数据规则授权");
            }
            else
            {
                _logger.LogDebug($"接到要保存的数据条数为：{models.Count}");
                _logger.LogDebug($"接到要保存的数据内容：{JsonSerializer.Serialize(models)}");
            }
            //
            // models.Clear();
            // userId = Guid.Parse("08d972d1-0e5f-424b-88be-d9768f229058");
            // models.Add(new SaveUserDataRuleViewModel()
            // {
            //     EntityDesc = "机构管理",
            //     EntityTypeName= "ZhuoFan.Wb.OrganizationService.Domain.Models.OrganizationEntity",
            //     UserType= UserType.GeneralUser,
            //     FieldName = "OrganizationId",
            //     FieldDesc = "机构",
            //     FieldType = "System.Guid",
            //     FieldValue = "08d97724-07fa-42f6-8a63-fe4dba762a92",
            //     FieldValueText = "string",
            //     ExpressionType = ExpressionType.Equal
            // });

            var userRules = models.Select(dataRuleFieldModel => new UserRule()
                {
                    EntityTypeName = dataRuleFieldModel.EntityTypeName,
                    EntityDesc = dataRuleFieldModel.EntityDesc,
                    FieldName = dataRuleFieldModel.FieldName,
                    FieldDesc = dataRuleFieldModel.FieldDesc,
                    FieldType = dataRuleFieldModel.FieldType,
                    FieldValue = dataRuleFieldModel.FieldValue,
                    FieldValueText = dataRuleFieldModel.FieldValueText,
                    ExpressionType = dataRuleFieldModel.ExpressionType
                })
                .ToList();

            var command = new UpdateUserRuleCommand(userId, userRules);
            await _bus.SendCommand(command);

            if (_notifications.HasNotifications())
            {
                var errorMessage = _notifications.GetNotificationMessage();
                throw new GirvsException(StatusCodes.Status400BadRequest, errorMessage);
            }
        }
    }
}
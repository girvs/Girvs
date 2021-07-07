using System;
using System.Threading.Tasks;
using BasicManagement.Application.ViewModels.User;
using BasicManagement.Domain.Commands.User;
using BasicManagement.Domain.Models;
using BasicManagement.Domain.Queries;
using BasicManagement.Domain.Repositories;
using Girvs;
using Girvs.AuthorizePermission;
using Girvs.AuthorizePermission.Enumerations;
using Girvs.AutoMapper.Extensions;
using Girvs.Cache;
using Girvs.Driven.Bus;
using Girvs.Driven.Notifications;
using Girvs.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Panda.DynamicWebApi.Attributes;

namespace BasicManagement.Application.AppService.Achieve
{
    [DynamicWebApi]
    [ServicePermissionDescriptor("用户管理", "587752d1-7937-4e6a-a035-ee013e58b99b")]
    public class UserAppService : IUserAppService
    {
        private readonly IStaticCacheManager _cacheManager;
        private readonly IMediatorHandler _bus;
        private readonly DomainNotificationHandler _notifications;
        private readonly IUserRepository _userRepository;

        public UserAppService(
            IStaticCacheManager cacheManager,
            IMediatorHandler bus,
            INotificationHandler<DomainNotification> notifications,
            IUserRepository userRepository
        )
        {
            _cacheManager = cacheManager ?? throw new ArgumentNullException(nameof(cacheManager));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _notifications = (DomainNotificationHandler) notifications;
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
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
        public async Task<UserEditViewModel> CreateAsync(UserEditViewModel model)
        {
            var existUser = await _userRepository.ExistEntityAsync(x =>
                x.UserAccount == model.UserAccount);

            if (existUser)
            {
                throw new GirvsException($"已存在UserAccount:{model.UserAccount} 对象");
            }

            var command = new CreateUserCommand(
                model.UserAccount,
                model.UserPassword.ToMd5(),
                model.UserName,
                model.ContactNumber,
                model.State,
                model.UserType
            );

            await _bus.SendCommand(command);
            if (_notifications.HasNotifications())
            {
                var (errorCode, errorMessage) = _notifications.GetNotificationMessage();
                throw new GirvsException(errorMessage, errorCode);
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
        [HttpPut]
        [ServiceMethodPermissionDescriptor("修改", Permission.Edit)]
        public async Task<UserEditViewModel> UpdateAsync(UserEditViewModel model)
        {
            var command = new UpdateUserCommand(
                model.Id,
                model.UserPassword.ToMd5(),
                model.UserName,
                model.ContactNumber,
                model.State,
                model.UserType
            );

            await _bus.SendCommand(command);

            if (_notifications.HasNotifications())
            {
                var (errorCode, errorMessage) = _notifications.GetNotificationMessage();
                throw new GirvsException(errorMessage, errorCode);
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
        /// 根据查询条件获取用户
        /// </summary>
        /// <param name="queryModel"></param>
        /// <returns></returns>
        [HttpGet]
        [ServiceMethodPermissionDescriptor("浏览", Permission.View)]
        public async Task<UserQueryViewModel> GetAsync(UserQueryViewModel queryModel)
        {
            var query = queryModel.MapToQuery<UserQuery>();

            var tempQuery = await _cacheManager.GetAsync(
                GirvsEntityCacheDefaults<User>.QueryCacheKey.Create(query.GetCacheKey()), async () =>
                {
                    await _userRepository.GetByQueryAsync(query);
                    return query;
                });

            if (!query.Equals(tempQuery))
            {
                query.RecordCount = tempQuery.RecordCount;
                query.Result = tempQuery.Result;
            }

            return query.MapToQueryDto<UserQueryViewModel, User>();
        }

        /// <summary>
        /// 根据登录名称获取用户
        /// </summary>
        /// <param name="account">登陆名称</param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("{account}")]
        public async Task<UserDetailViewModel> GetByAccount(string account)
        {
            var user = await _userRepository.GetUserByLoginNameAsync(account);
            if (user == null)
            {
                throw new GirvsException("未找到对应的用户", StatusCodes.Status404NotFound);
            }

            return user.MapToDto<UserDetailViewModel>();
        }

        /// <summary>
        /// 根据其它主键值获取用户
        /// </summary>
        /// <param name="otherId">其它主键值</param>
        /// <returns></returns>
        [HttpGet("{otherId}")]
        [ServiceMethodPermissionDescriptor("浏览", Permission.View)]
        public async Task<UserDetailViewModel> GetByOtherId(Guid otherId)
        {
            if (otherId == Guid.Empty)
            {
                throw new GirvsException("未找到对应的用户", StatusCodes.Status404NotFound);
            }

            var user = await _userRepository.GetUserByOtherIdAsync(otherId);
            if (user == null)
            {
                throw new GirvsException("未找到对应的用户", StatusCodes.Status404NotFound);
            }

            return user.MapToDto<UserDetailViewModel>();
        }
    }
}
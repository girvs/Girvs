using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Girvs.AuthorizePermission.Enumerations;
using Girvs.BusinessBasis.Repositories;
using Girvs.BusinessBasis.UoW;
using Girvs.Cache.Caching;
using Girvs.Driven.Bus;
using Girvs.Driven.CacheDriven.Events;
using Girvs.Driven.Commands;
using Girvs.Driven.Notifications;
using Girvs.Extensions;
using JetBrains.Annotations;
using MediatR;
using Microsoft.AspNetCore.Http;
using ZhuoFan.Wb.BasicService.Domain.Commands.User;
using ZhuoFan.Wb.BasicService.Domain.Events;
using ZhuoFan.Wb.BasicService.Domain.Models;
using ZhuoFan.Wb.BasicService.Domain.Repositories;

namespace ZhuoFan.Wb.BasicService.Domain.CommandHandlers
{
    public class UserCommandHandler : CommandHandler,
        IRequestHandler<CreateUserCommand, bool>,
        IRequestHandler<UpdateUserCommand, bool>,
        IRequestHandler<DeleteUserCommand, bool>,
        IRequestHandler<ChangeUserStateCommand, bool>,
        IRequestHandler<UpdateUserRoleCommand, bool>,
        IRequestHandler<ChangeUserPassworkCommand, bool>,
        IRequestHandler<UpdateUserEventCommand, bool>,
        IRequestHandler<AddUserRoleCommand, bool>,
        IRequestHandler<DeleteUserRoleCommand, bool>,
        IRequestHandler<UpdateUserRuleCommand, bool>,
        IRequestHandler<UserEditPasswordCommand, bool>,
        IRequestHandler<EventCreateUserCommand, bool>,
        IRequestHandler<BatchDeleteUserCommand, bool>,
        IRequestHandler<BatchChangeUserPasswordCommand,bool>,
        IRequestHandler<BatchChangeUserStateCommand,bool>
    {
        private readonly IMediatorHandler _bus;
        private readonly IUserRepository _userRepository;
        private readonly IRepository<UserRule> _userRuleRepository;
        private readonly IRoleRepository _roleRepository;

        public UserCommandHandler(
            IMediatorHandler bus,
            IUserRepository userRepository,
            [NotNull] IRepository<UserRule> userRuleRepository,
            IUnitOfWork<User> unitOfWork,
            [NotNull] IRoleRepository roleRepository
        ) : base(unitOfWork,
            bus)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _userRuleRepository = userRuleRepository ?? throw new ArgumentNullException(nameof(userRuleRepository));
            _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
        }

        public async Task<bool> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            var existUser = await _userRepository.GetUserByLoginNameAsync(request.UserAccount);

            if (existUser != null)
            {
                await _bus.RaiseEvent(new DomainNotification(nameof(request.UserAccount), "登陆名称已存在"),
                    cancellationToken);
                return false;
            }

            // var currentUser = EngineContext.Current.GetCurrentUser();
            // if (currentUser == null || request.UserType <= currentUser.UserType)
            // {
            //     await _bus.RaiseEvent(new DomainNotification(nameof(request.UserType), "只能创建下级用户，不能创建相同等级或上级用户"),
            //         cancellationToken);
            //     return false;
            // }

            var user = new User
            {
                ContactNumber = request.ContactNumber,
                State = request.State,
                UserAccount = request.UserAccount,
                UserName = request.UserName,
                UserPassword = request.UserPassword,
                UserType = request.UserType,
                OtherId = request.OtherId
            };

            user.TenantId = request.TenantId ?? user.TenantId;

            await _userRepository.AddAsync(user);

            if (!await Commit()) return false;

            var key = GirvsEntityCacheDefaults<User>.ByIdCacheKey.Create(user.Id.ToString());
            await _bus.RaiseEvent(new RemoveCacheEvent(key), cancellationToken);
            await _bus.RaiseEvent(new RemoveCacheListEvent(GirvsEntityCacheDefaults<User>.ListCacheKey.Create()),
                cancellationToken);
            request.Id = user.Id;
            return true;
        }

        public async Task<bool> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.Id);
            if (user == null)
            {
                await _bus.RaiseEvent(
                    new DomainNotification(request.Id.ToString(), "未找到对应的数据", StatusCodes.Status404NotFound),
                    cancellationToken);
                return false;
            }

            if (user.UserType is UserType.AdminUser or UserType.TenantAdminUser)
            {
                await _bus.RaiseEvent(
                    new DomainNotification(request.Id.ToString(), "当前用户禁止修改", 568),
                    cancellationToken);
                return false;
            }

            user.ContactNumber = request.ContactNumber;
            user.State = request.State;
            user.UserName = request.UserName;
            user.UserType = request.UserType;

            if (user.UserPassword != request.UserPassword)
            {
                user.UserPassword = request.UserPassword;
            }

            await _userRepository.UpdateAsync(user);

            if (await Commit())
            {
                var key = GirvsEntityCacheDefaults<User>.ByIdCacheKey.Create(user.Id.ToString());
                await _bus.RaiseEvent(new RemoveCacheEvent(key), cancellationToken);
                await _bus.RaiseEvent(new RemoveCacheListEvent(GirvsEntityCacheDefaults<User>.ListCacheKey.Create()),
                    cancellationToken);
            }

            return true;
        }

        public async Task<bool> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetUserByIdIncludeRoleAndDataRule(request.Id);
            if (user == null)
            {
                await _bus.RaiseEvent(
                    new DomainNotification(request.Id.ToString(), "未找到对应的数据", StatusCodes.Status404NotFound),
                    cancellationToken);
                return false;
            }

            if (user.IsInitData)
            {
                await _bus.RaiseEvent(new DomainNotification(request.Id.ToString(), "系统初始化数据，无法进行操作"),
                    cancellationToken);
                return false;
            }

            if (user.UserType is UserType.AdminUser or UserType.TenantAdminUser)
            {
                await _bus.RaiseEvent(
                    new DomainNotification(request.Id.ToString(), "当前用户禁止修改", 568),
                    cancellationToken);
                return false;
            }

            await _userRepository.DeleteAsync(user);

            if (await Commit())
            {
                var key = GirvsEntityCacheDefaults<User>.ByIdCacheKey.Create(user.Id.ToString());
                await _bus.RaiseEvent(new RemoveCacheEvent(key), cancellationToken);
                await _bus.RaiseEvent(new RemoveCacheListEvent(GirvsEntityCacheDefaults<User>.ListCacheKey.Create()),
                    cancellationToken);
            }

            return true;
        }

        public async Task<bool> Handle(ChangeUserStateCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.Id);
            if (user == null)
            {
                await _bus.RaiseEvent(
                    new DomainNotification(request.Id.ToString(), "未找到对应的数据", StatusCodes.Status404NotFound),
                    cancellationToken);
                return false;
            }

            if (user.UserType is UserType.AdminUser or UserType.TenantAdminUser)
            {
                await _bus.RaiseEvent(
                    new DomainNotification(request.Id.ToString(), "当前用户禁止修改", 568),
                    cancellationToken);
                return false;
            }

            user.State = request.State;

            await _userRepository.UpdateAsync(user, nameof(User.State));

            if (await Commit())
            {
                var key = GirvsEntityCacheDefaults<User>.ByIdCacheKey.Create(user.Id.ToString());
                await _bus.RaiseEvent(new RemoveCacheEvent(key), cancellationToken);
                await _bus.RaiseEvent(new RemoveCacheListEvent(GirvsEntityCacheDefaults<User>.ListCacheKey.Create()),
                    cancellationToken);
            }

            return true;
        }

        public async Task<bool> Handle(UpdateUserRoleCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetUserByIdIncludeRolesAsync(request.Id);

            if (user == null)
            {
                await _bus.RaiseEvent(
                    new DomainNotification(request.Id.ToString(), "未找到对应的数据", StatusCodes.Status404NotFound),
                    cancellationToken);
                return false;
            }

            var roles = await _roleRepository.GetWhereAsync(x => request.RoleIds.Contains(x.Id));
            user.Roles = roles;

            if (await Commit())
            {
                var key = GirvsEntityCacheDefaults<User>.ByIdCacheKey.Create(user.Id.ToString());
                await _bus.RaiseEvent(new RemoveCacheEvent(key), cancellationToken);
                await _bus.RaiseEvent(new RemoveCacheListEvent(GirvsEntityCacheDefaults<User>.ListCacheKey.Create()),
                    cancellationToken);
            }

            return true;
        }

        public async Task<bool> Handle(ChangeUserPassworkCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.Id);

            if (user == null)
            {
                await _bus.RaiseEvent(new DomainNotification(request.Id.ToString(), "用户不存在"), cancellationToken);
                return false;
            }

            user.UserPassword = request.NewPassword.ToMd5();

            await _userRepository.UpdateAsync(user);

            if (await Commit())
            {
                var key = GirvsEntityCacheDefaults<User>.ByIdCacheKey.Create(user.Id.ToString());
                await _bus.RaiseEvent(new RemoveCacheEvent(key), cancellationToken);
                await _bus.RaiseEvent(new RemoveCacheListEvent(GirvsEntityCacheDefaults<User>.ListCacheKey.Create()),
                    cancellationToken);
            }

            return true;
        }

        public Task<bool> Handle(UpdateUserEventCommand request, CancellationToken cancellationToken)
        {
            // var user = await _userRepository.GetUserByOtherIdAsync(request.OtherId);
            // if (user == null)
            // {
            //     await _bus.RaiseEvent(
            //         new DomainNotification(request.Id.ToString(), "未找到对应的数据", StatusCodes.Status404NotFound),
            //         cancellationToken);
            //     return false;
            // }
            //
            // user.ContactNumber = request.ContactNumber;
            // user.UserName = request.UserName;
            //
            // await _userRepository.UpdateAsync(user);
            //
            // if (await Commit())
            // {
            //     // await _bus.RaiseEvent(new SetCacheEvent(user, _cacheKeyManager.BuildCacheEntityKey(user.Id),
            //     //     _cacheKeyManager.CacheTime), cancellationToken);
            //     // await _bus.RaiseEvent(new RemoveCacheListEvent(_cacheKeyManager.CacheKeyListPrefix), cancellationToken);
            // }
            //
            // return true;

            return Task.FromResult(true);
        }

        public async Task<bool> Handle(AddUserRoleCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetUserByIdIncludeRolesAsync(request.UserId);
            if (user == null)
            {
                await _bus.RaiseEvent(
                    new DomainNotification(request.UserId.ToString(), "未找到对应的数据", StatusCodes.Status404NotFound),
                    cancellationToken);
                return false;
            }

            var roles = await _roleRepository.GetWhereAsync(x => request.RoleIds.Contains(x.Id));
            user.Roles.AddRange(roles);

            if (await Commit())
            {
                var key = GirvsEntityCacheDefaults<User>.ByIdCacheKey.Create(user.Id.ToString());
                _bus.RaiseEvent(new RemoveCacheEvent(key), cancellationToken);
                _bus.RaiseEvent(new RemoveCacheListEvent(GirvsEntityCacheDefaults<User>.ListCacheKey.Create()),
                    cancellationToken);
                _bus.RaiseEvent(new RemoveServiceCacheEvent(), cancellationToken);
            }

            return true;
        }

        public async Task<bool> Handle(DeleteUserRoleCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetUserByIdIncludeRolesAsync(request.UserId);
            if (user == null)
            {
                await _bus.RaiseEvent(
                    new DomainNotification(request.UserId.ToString(), "未找到对应的数据", StatusCodes.Status404NotFound),
                    cancellationToken);
                return false;
            }

            user.Roles.RemoveAll(x => request.RoleIds.Contains(x.Id));

            if (await Commit())
            {
                var key = GirvsEntityCacheDefaults<User>.ByIdCacheKey.Create(user.Id.ToString());
                _bus.RaiseEvent(new RemoveCacheEvent(key), cancellationToken);
                _bus.RaiseEvent(new RemoveCacheListEvent(GirvsEntityCacheDefaults<User>.ListCacheKey.Create()),
                    cancellationToken);
                _bus.RaiseEvent(new RemoveServiceCacheEvent(), cancellationToken);
            }

            return true;
        }

        public async Task<bool> Handle(UpdateUserRuleCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetUserByIdIncludeRoleAndDataRule(request.UserId);
            if (user == null)
            {
                await _bus.RaiseEvent(
                    new DomainNotification(request.UserId.ToString(), "未找到对应的数据", StatusCodes.Status404NotFound),
                    cancellationToken);
                return false;
            }

            await _userRuleRepository.DeleteRangeAsync(user.RulesList);
            user.RulesList.Clear();
            user.RulesList.AddRange(request.UserRules);

            await _userRepository.UpdateAsync(user);

            if (await Commit())
            {
                var key = GirvsEntityCacheDefaults<User>.ByIdCacheKey.Create(user.Id.ToString());
                _bus.RaiseEvent(new RemoveCacheEvent(key), cancellationToken);
                _bus.RaiseEvent(new RemoveCacheListEvent(GirvsEntityCacheDefaults<User>.ListCacheKey.Create()),
                    cancellationToken);
                _bus.RaiseEvent(new RemoveServiceCacheEvent(), cancellationToken);
            }

            return true;
        }

        public async Task<bool> Handle(UserEditPasswordCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.Id);

            if (user == null || user.UserPassword != request.OldPassword.ToMd5())
            {
                await _bus.RaiseEvent(new DomainNotification(request.Id.ToString(), "用户不存在或原密码不正确"), cancellationToken);
                return false;
            }

            user.UserPassword = request.NewPassword.ToMd5();

            await _userRepository.UpdateAsync(user);

            if (await Commit())
            {
                var key = GirvsEntityCacheDefaults<User>.ByIdCacheKey.Create(user.Id.ToString());
                _bus.RaiseEvent(new RemoveCacheEvent(key), cancellationToken);
                _bus.RaiseEvent(new RemoveCacheListEvent(GirvsEntityCacheDefaults<User>.ListCacheKey.Create()),
                    cancellationToken);
            }

            return true;
        }

        public async Task<bool> Handle(EventCreateUserCommand request, CancellationToken cancellationToken)
        {
            var existUser = await _userRepository.GetUserByLoginNameAsync(request.UserAccount);

            if (existUser != null)
            {
                await _bus.RaiseEvent(new DomainNotification(nameof(request.UserAccount), "登陆名称已存在"),
                    cancellationToken);
                return false;
            }

            var user = new User
            {
                ContactNumber = request.ContactNumber,
                State = request.State,
                UserAccount = request.UserAccount,
                UserName = request.UserName,
                UserPassword = request.UserPassword,
                UserType = request.UserType,
                OtherId = request.OtherId
            };

            user.TenantId = request.TenantId ?? user.TenantId;

            await _userRepository.CreateTenantIdAdmin(user);

            if (!await Commit()) return false;

            var key = GirvsEntityCacheDefaults<User>.ByIdCacheKey.Create(user.Id.ToString());
            _bus.RaiseEvent(new RemoveCacheEvent(key), cancellationToken);
            _bus.RaiseEvent(new RemoveCacheListEvent(GirvsEntityCacheDefaults<User>.ListCacheKey.Create()),
                cancellationToken);
            request.Id = user.Id;
            return true;
        }

        public async Task<bool> Handle(BatchDeleteUserCommand request, CancellationToken cancellationToken)
        {
            //批量删除不允许删除超级管理员和租户管理员。
            var users = await _userRepository.GetUsersIncludeRolesAndDataRule(x =>
                request.Ids.Contains(x.Id) &&
                (x.UserType != UserType.AdminUser && x.UserType != UserType.TenantAdminUser));

            await _userRepository.DeleteRangeAsync(users);
            if (await Commit())
            {
                foreach (var id in request.Ids)
                {
                    var key = GirvsEntityCacheDefaults<Role>.ByIdCacheKey.Create(id.ToString());
                    _bus.RaiseEvent(new RemoveCacheEvent(key), cancellationToken);
                }

                _bus.RaiseEvent(new RemoveCacheListEvent(GirvsEntityCacheDefaults<Role>.ListCacheKey.Create()),
                    cancellationToken);
            }

            return true;
        }

        public async Task<bool> Handle(BatchChangeUserPasswordCommand request, CancellationToken cancellationToken)
        {
            var users = await _userRepository.GetWhereAsync(x =>
                request.Ids.Contains(x.Id) &&
                (x.UserType != UserType.AdminUser && x.UserType != UserType.TenantAdminUser));

            users.ForEach(x=>x.UserPassword = request.NewPassword.ToMd5());

            await _userRepository.UpdateRangeAsync(users);
            
            if (await Commit())
            {
                foreach (var id in request.Ids)
                {
                    var key = GirvsEntityCacheDefaults<Role>.ByIdCacheKey.Create(id.ToString());
                    await _bus.RaiseEvent(new RemoveCacheEvent(key), cancellationToken);
                }

                _bus.RaiseEvent(new RemoveCacheListEvent(GirvsEntityCacheDefaults<Role>.ListCacheKey.Create()),
                    cancellationToken);
            }

            return true;
        }

        public async Task<bool> Handle(BatchChangeUserStateCommand request, CancellationToken cancellationToken)
        {
            var users = await _userRepository.GetWhereAsync(x =>
                request.Ids.Contains(x.Id) &&
                (x.UserType != UserType.AdminUser && x.UserType != UserType.TenantAdminUser));

            users.ForEach(x=>x.State = request.DataState);

            await _userRepository.UpdateRangeAsync(users);
            
            if (await Commit())
            {
                foreach (var id in request.Ids)
                {
                    var key = GirvsEntityCacheDefaults<Role>.ByIdCacheKey.Create(id.ToString());
                    await _bus.RaiseEvent(new RemoveCacheEvent(key), cancellationToken);
                }

                _bus.RaiseEvent(new RemoveCacheListEvent(GirvsEntityCacheDefaults<Role>.ListCacheKey.Create()),
                    cancellationToken);
            }

            return true;
        }
    }
}
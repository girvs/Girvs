using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Girvs.BusinessBasis.UoW;
using Girvs.Driven.Bus;
using Girvs.Driven.Commands;
using Girvs.Driven.Notifications;
using Girvs.Extensions;
using MediatR;
using Microsoft.AspNetCore.Http;
using ZhuoFan.Wb.BasicService.Domain.Commands.User;
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
        IRequestHandler<UpdateUserEventCommand, bool>
    {
        private readonly IMediatorHandler _bus;
        private readonly IUserRepository _userRepository;

        public UserCommandHandler(
            IMediatorHandler bus,
            IUserRepository userRepository,
            IUnitOfWork<User> unitOfWork
            ) : base(unitOfWork,
            bus)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
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

            var user = new User()
            {
                ContactNumber = request.ContactNumber,
                State = request.State,
                UserAccount = request.UserAccount,
                UserName = request.UserName,
                UserPassword = request.UserPassword,
                UserType = request.UserType,
                OtherId = request.OtherId
            };

            await _userRepository.AddAsync(user);
            if (await Commit())
            {
                // await _bus.RaiseEvent(new SetCacheEvent(user, _cacheKeyManager.BuildCacheEntityKey(user.Id),
                //     _cacheKeyManager.CacheTime), cancellationToken);
                // await _bus.RaiseEvent(new RemoveCacheListEvent(_cacheKeyManager.CacheKeyListPrefix), cancellationToken);
                request.Id = user.Id;
            }

            return true;
        }

        public async Task<bool> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.Id);
            if (user == null)
            {
                await _bus.RaiseEvent(new DomainNotification(request.Id.ToString(), "未找到对应的数据",StatusCodes.Status404NotFound), cancellationToken);
                return false;
            }

            user.ContactNumber = request.ContactNumber;
            user.State = request.State;
            user.UserName = request.UserName;
            user.UserType = request.UserType;

            if (user.UserPassword != request.UserPassword)
            {
                user.UserPassword = request.UserPassword.ToMd5();
            }

            await _userRepository.UpdateAsync(user);

            if (await Commit())
            {
                // await _bus.RaiseEvent(new SetCacheEvent(user, _cacheKeyManager.BuildCacheEntityKey(user.Id),
                //     _cacheKeyManager.CacheTime), cancellationToken);
                // await _bus.RaiseEvent(new RemoveCacheListEvent(_cacheKeyManager.CacheKeyListPrefix), cancellationToken);
            }

            return true;
        }

        public async Task<bool> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.Id);
            if (user == null)
            {
                await _bus.RaiseEvent(new DomainNotification(request.Id.ToString(), "未找到对应的数据",StatusCodes.Status404NotFound), cancellationToken);
                return false;
            }

            if (user.IsInitData)
            {
                await _bus.RaiseEvent(new DomainNotification(request.Id.ToString(), "系统初始化数据，无法进行操作"), cancellationToken);
                return false;
            }

            await _userRepository.DeleteAsync(user);

            if (await Commit())
            {
                // await _bus.RaiseEvent(new RemoveCacheEvent(_cacheKeyManager.BuildCacheEntityKey(user.Id)),
                //     cancellationToken);
                // await _bus.RaiseEvent(new RemoveCacheListEvent(_cacheKeyManager.CacheKeyListPrefix), cancellationToken);
            }

            return true;
        }

        public async Task<bool> Handle(ChangeUserStateCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.Id);
            if (user == null)
            {
                await _bus.RaiseEvent(new DomainNotification(request.Id.ToString(), "未找到对应的数据",StatusCodes.Status404NotFound), cancellationToken);
                return false;
            }

            user.State = request.State;

            await _userRepository.UpdateAsync(user, nameof(User.State));

            if (await Commit())
            {
                // await _bus.RaiseEvent(new SetCacheEvent(user, _cacheKeyManager.BuildCacheEntityKey(user.Id),
                //     _cacheKeyManager.CacheTime), cancellationToken);
                // await _bus.RaiseEvent(new RemoveCacheListEvent(_cacheKeyManager.CacheKeyListPrefix), cancellationToken);
            }

            return true;
        }

        public async Task<bool> Handle(UpdateUserRoleCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetUserByIdIncludeRolesAsync(request.Id);

            user.Roles.RemoveAll(x => !request.RoleIds.Contains(x.Id));

            //foreach (var roleId in request.RoleIds)
            //{
            //    if (!user.UserRoles.Select(r=>r.RoleId).Contains(roleId))
            //    {
            //        user.UserRoles.Add(new UserRole()
            //        {
            //            UserId = request.UserId,
            //            RoleId = roleId
            //        });
            //    }
            //}

            user.Roles.AddRange(
                request.RoleIds.Where(x => !user.Roles
                        .Select(r => r.Id).Contains(x))
                    .Select(ur => new Role()
                    {
                        Id = request.Id
                    }));

            if (await Commit())
            {
            }

            return true;
        }

        public async Task<bool> Handle(ChangeUserPassworkCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.Id);
            if (user != null && user.UserPassword == request.OldPassword.ToMd5())
            {
                user.UserPassword = request.NewPassword.ToMd5();
                if (await Commit())
                {
                    return true;
                }
            }
            else
            {
                await _bus.RaiseEvent(new DomainNotification(user.UserAccount, "旧的密码错误"), cancellationToken);
                // await _bus.RaiseEvent(new RemoveCacheEvent(_cacheKeyManager.BuildCacheEntityKey(user.Id)),
                //     cancellationToken);
                // await _bus.RaiseEvent(new RemoveCacheListEvent(_cacheKeyManager.CacheKeyListPrefix), cancellationToken);
            }

            return false;
        }

        public async Task<bool> Handle(UpdateUserEventCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetUserByOtherIdAsync(request.OtherId);
            if (user == null)
            {
                await _bus.RaiseEvent(new DomainNotification(request.Id.ToString(), "未找到对应的数据",StatusCodes.Status404NotFound), cancellationToken);
                return false;
            }

            user.ContactNumber = request.ContactNumber;
            user.UserName = request.UserName;

            await _userRepository.UpdateAsync(user);

            if (await Commit())
            {
                // await _bus.RaiseEvent(new SetCacheEvent(user, _cacheKeyManager.BuildCacheEntityKey(user.Id),
                //     _cacheKeyManager.CacheTime), cancellationToken);
                // await _bus.RaiseEvent(new RemoveCacheListEvent(_cacheKeyManager.CacheKeyListPrefix), cancellationToken);
            }

            return true;
        }
    }
}
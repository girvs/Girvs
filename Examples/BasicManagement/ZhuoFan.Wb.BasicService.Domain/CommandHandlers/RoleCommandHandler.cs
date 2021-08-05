using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Girvs.BusinessBasis.UoW;
using Girvs.Driven.Bus;
using Girvs.Driven.Commands;
using Girvs.Driven.Notifications;
using MediatR;
using ZhuoFan.Wb.BasicService.Domain.Commands.Role;
using ZhuoFan.Wb.BasicService.Domain.Models;
using ZhuoFan.Wb.BasicService.Domain.Repositories;

namespace ZhuoFan.Wb.BasicService.Domain.CommandHandlers
{
    public class RoleCommandHandler : CommandHandler,
        IRequestHandler<CreateRoleCommand, bool>,
        IRequestHandler<UpdateRoleCommand, bool>,
        IRequestHandler<DeleteRoleCommand, bool>,
        IRequestHandler<UpdateRoleUserCommand, bool>
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IMediatorHandler _bus;

        public RoleCommandHandler(
            IRoleRepository roleRepository,
            IMediatorHandler bus,
            IUnitOfWork<Role> unitOfWork
        ) : base(unitOfWork, bus)
        {
            _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        public async Task<bool> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
        {
            var role = new Role()
            {
                Name = request.Name,
                Desc = request.Desc,
                Users = request.UserIds.Select(uid => new User() {Id = uid}).ToList()
            };


            await _roleRepository.AddAsync(role);

            if (await Commit())
            {
                request.Id = role.Id;
                // await _bus.RaiseEvent(new RemoveCacheListEvent(_cacheKeyManager.CacheKeyListPrefix));
            }

            return true;
        }

        public async Task<bool> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
        {
            var role = await _roleRepository.GetByIdAsync(request.Id);
            if (role == null)
            {
                await _bus.RaiseEvent(new DomainNotification(request.Id.ToString(), "未找到对应的数据"));
                return false;
            }

            role.Name = request.Name;
            role.Desc = request.Desc;

            // 移除不存在的用户
            role.Users.RemoveAll(user => !request.UserIds.Contains(user.Id));

            // 添加新的用户
            var newUserRoles = request.UserIds.Select(uId => new User {Id = uId})
                .ToList();
            newUserRoles.RemoveAll(userRole => role.Users.Select(oldur => oldur.Id).Contains(userRole.Id));
            role.Users.AddRange(newUserRoles);


            await _roleRepository.UpdateAsync(role);

            if (await Commit())
            {
                // await _bus.RaiseEvent(new RemoveCacheEvent(_cacheKeyManager.BuildCacheEntityKey(role.Id)));
                // await _bus.RaiseEvent(new RemoveCacheListEvent(_cacheKeyManager.CacheKeyListPrefix));
            }

            return true;
        }

        public async Task<bool> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
        {
            var role = await _roleRepository.GetByIdAsync(request.Id);
            if (role == null)
            {
                await _bus.RaiseEvent(new DomainNotification(request.Id.ToString(), "未找到对应的数据"));
                return false;
            }

            if (role.IsInitData)
            {
                await _bus.RaiseEvent(new DomainNotification(request.Id.ToString(), "系统初始化数据，无法进行操作"));
                return false;
            }

            await _roleRepository.DeleteAsync(role);

            if (await Commit())
            {
                // await _bus.RaiseEvent(new RemoveCacheEvent(_cacheKeyManager.BuildCacheEntityKey(role.Id)));
                // await _bus.RaiseEvent(new RemoveCacheListEvent(_cacheKeyManager.CacheKeyListPrefix));
            }

            return true;
        }

        public async Task<bool> Handle(UpdateRoleUserCommand request, CancellationToken cancellationToken)
        {
            var role = await _roleRepository.GetRoleByIdIncludeUsersAsync(request.Id);
            role.Users.RemoveAll(x => !request.UserIds.Contains(x.Id));

            var userRoles = request.UserIds
                .Where(userId => !role.Users.Select(userRole => userRole.Id).Contains(userId))
                .Select(ur => new User()
                {
                    Id = request.Id
                }).ToList();

            if (userRoles.Any())
            {
                role.Users.AddRange(userRoles);
            }

            if (await Commit())
            {
                // await _bus.RaiseEvent(new RemoveCacheEvent(_cacheKeyManager.BuildCacheEntityKey(role.Id)));
                // await _bus.RaiseEvent(new RemoveCacheListEvent(_cacheKeyManager.CacheKeyListPrefix));
            }

            return true;
        }
    }
}
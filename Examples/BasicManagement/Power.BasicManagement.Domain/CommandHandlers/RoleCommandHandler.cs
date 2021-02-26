using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Girvs.Domain.Caching.Events;
using Girvs.Domain.Caching.Interface;
using Girvs.Domain.Driven.Bus;
using Girvs.Domain.Driven.Commands;
using Girvs.Domain.Driven.Notifications;
using Girvs.Domain.Managers;
using MediatR;
using Power.BasicManagement.Domain.Commands.Role;
using Power.BasicManagement.Domain.Models;
using Power.BasicManagement.Domain.Repositories;

namespace Power.BasicManagement.Domain.CommandHandlers
{
    public class RoleCommandHandler : CommandHandler,
        IRequestHandler<CreateRoleCommand, bool>,
        IRequestHandler<UpdateRoleCommand, bool>,
        IRequestHandler<DeleteRoleCommand, bool>,
        IRequestHandler<UpdateRoleUserCommand, bool>
    {
        private readonly IRoleRepository _roleRepository;
        private readonly ICacheKeyManager<Role> _cacheKeyManager;
        private readonly IMediatorHandler _bus;

        public RoleCommandHandler(
            IRoleRepository roleRepository,
            ICacheKeyManager<Role> cacheKeyManager,
            IMediatorHandler bus,
            IUnitOfWork<Role> unitOfWork
        ) : base(unitOfWork, bus)
        {
            _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
            _cacheKeyManager = cacheKeyManager ?? throw new ArgumentNullException(nameof(cacheKeyManager));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        public async Task<bool> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
        {
            var role = new Role()
            {
                Name = request.Name,
                Desc = request.Desc,
                UserRoles = request.UserIds.Select(uid => new UserRole() {UserId = uid}).ToList()
            };

            await _roleRepository.AddAsync(role);

            if (await Commit())
            {
                request.Id = role.Id;
                await _bus.RaiseEvent(new RemoveCacheListEvent(_cacheKeyManager.CacheKeyListPrefix));
            }

            return true;
        }

        public async Task<bool> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
        {
            var role = await _roleRepository.GetByIdAsync(request.Id);
            if (role == null)
            {
                await _bus.RaiseEvent(new DomainNotification("", "未找到对应的数据"));
                return false;
            }

            role.Name = request.Name;
            role.Desc = request.Desc;

            // 移除不存在的用户
            role.UserRoles.RemoveAll(userRole => !request.UserIds.Contains(userRole.UserId));

            // 添加新的用户
            var newUserRoles = request.UserIds.Select(uId => new UserRole() {UserId = uId, RoleId = request.Id})
                .ToList();
            newUserRoles.RemoveAll(userRole => role.UserRoles.Select(oldur => oldur.UserId).Contains(userRole.UserId));
            role.UserRoles.AddRange(newUserRoles);


            await _roleRepository.UpdateAsync(role);

            if (await Commit())
            {
                await _bus.RaiseEvent(new RemoveCacheEvent(_cacheKeyManager.BuildCacheEntityKey(role.Id)));
                await _bus.RaiseEvent(new RemoveCacheListEvent(_cacheKeyManager.CacheKeyListPrefix));
            }

            return true;
        }

        public async Task<bool> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
        {
            var role = await _roleRepository.GetByIdAsync(request.Id);
            if (role == null)
            {
                await _bus.RaiseEvent(new DomainNotification("", "未找到对应的数据"));
                return false;
            }

            if (role.IsInitData)
            {
                await _bus.RaiseEvent(new DomainNotification("", "系统初始化数据，无法进行操作"));
                return false;
            }

            await _roleRepository.DeleteAsync(role);

            if (await Commit())
            {
                await _bus.RaiseEvent(new RemoveCacheEvent(_cacheKeyManager.BuildCacheEntityKey(role.Id)));
                await _bus.RaiseEvent(new RemoveCacheListEvent(_cacheKeyManager.CacheKeyListPrefix));
            }

            return true;
        }

        public async Task<bool> Handle(UpdateRoleUserCommand request, CancellationToken cancellationToken)
        {
            var role = await _roleRepository.GetRoleByIdIncludeUsersAsync(request.Id);
            role.UserRoles.RemoveAll(x => !request.UserIds.Contains(x.UserId));

            var userRoles = request.UserIds
                .Where(userId => !role.UserRoles.Select(userRole => userRole.UserId).Contains(userId))
                .Select(ur => new UserRole()
                {
                    RoleId = request.Id,
                    UserId = ur
                }).ToList();

            if (userRoles.Any())
            {
                role.UserRoles.AddRange(userRoles);
            }

            if (await Commit())
            {
                await _bus.RaiseEvent(new RemoveCacheEvent(_cacheKeyManager.BuildCacheEntityKey(role.Id)));
                await _bus.RaiseEvent(new RemoveCacheListEvent(_cacheKeyManager.CacheKeyListPrefix));
            }

            return true;
        }
    }
}
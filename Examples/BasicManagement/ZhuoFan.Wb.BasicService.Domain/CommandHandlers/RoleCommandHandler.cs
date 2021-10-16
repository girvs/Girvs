using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Girvs.BusinessBasis.UoW;
using Girvs.Cache.Caching;
using Girvs.Driven.Bus;
using Girvs.Driven.CacheDriven.Events;
using Girvs.Driven.Commands;
using Girvs.Driven.Notifications;
using JetBrains.Annotations;
using MediatR;
using ZhuoFan.Wb.BasicService.Domain.Commands.Role;
using ZhuoFan.Wb.BasicService.Domain.Events;
using ZhuoFan.Wb.BasicService.Domain.Models;
using ZhuoFan.Wb.BasicService.Domain.Repositories;

namespace ZhuoFan.Wb.BasicService.Domain.CommandHandlers
{
    public class RoleCommandHandler : CommandHandler,
        IRequestHandler<CreateRoleCommand, bool>,
        IRequestHandler<UpdateRoleCommand, bool>,
        IRequestHandler<DeleteRoleCommand, bool>,
        IRequestHandler<UpdateRoleUserCommand, bool>,
        IRequestHandler<AddRoleUserCommand, bool>,
        IRequestHandler<DeleteRoleUserCommand, bool>,
        IRequestHandler<BatchDeleteRoleCommand, bool>
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IMediatorHandler _bus;
        private readonly IUserRepository _userRepository;

        public RoleCommandHandler(
            IRoleRepository roleRepository,
            IMediatorHandler bus,
            [NotNull] IUserRepository userRepository,
            IUnitOfWork<Role> unitOfWork
        ) : base(unitOfWork, bus)
        {
            _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        }

        public async Task<bool> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
        {
            var role = new Role()
            {
                Name = request.Name,
                Desc = request.Desc,
                Users = request.UserIds.Select(uid => new User() { Id = uid }).ToList()
            };


            await _roleRepository.AddAsync(role);

            if (await Commit())
            {
                request.Id = role.Id;
                var key = GirvsEntityCacheDefaults<Role>.ByIdCacheKey.Create(role.Id.ToString());
                _bus.RaiseEvent(new RemoveCacheEvent(key), cancellationToken);
                _bus.RaiseEvent(new RemoveCacheListEvent(GirvsEntityCacheDefaults<Role>.ListCacheKey.Create()),
                    cancellationToken);
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
            var newUserRoles = request.UserIds.Select(uId => new User { Id = uId })
                .ToList();
            newUserRoles.RemoveAll(userRole => role.Users.Select(oldur => oldur.Id).Contains(userRole.Id));
            role.Users.AddRange(newUserRoles);


            await _roleRepository.UpdateAsync(role);

            if (await Commit())
            {
                var key = GirvsEntityCacheDefaults<Role>.ByIdCacheKey.Create(role.Id.ToString());
                _bus.RaiseEvent(new RemoveCacheEvent(key), cancellationToken);
                _bus.RaiseEvent(new RemoveCacheListEvent(GirvsEntityCacheDefaults<Role>.ListCacheKey.Create()),
                    cancellationToken);
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
                var key = GirvsEntityCacheDefaults<Role>.ByIdCacheKey.Create(role.Id.ToString());
                _bus.RaiseEvent(new RemoveCacheEvent(key), cancellationToken);
                _bus.RaiseEvent(new RemoveCacheListEvent(GirvsEntityCacheDefaults<Role>.ListCacheKey.Create()),
                    cancellationToken);
                _bus.RaiseEvent(new RemoveServiceCacheEvent(), cancellationToken);
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
                var key = GirvsEntityCacheDefaults<Role>.ByIdCacheKey.Create(role.Id.ToString());
                _bus.RaiseEvent(new RemoveCacheEvent(key), cancellationToken);
                _bus.RaiseEvent(new RemoveCacheListEvent(GirvsEntityCacheDefaults<Role>.ListCacheKey.Create()),
                    cancellationToken);
            }

            return true;
        }

        public async Task<bool> Handle(AddRoleUserCommand request, CancellationToken cancellationToken)
        {
            var role = await _roleRepository.GetRoleByIdIncludeUsersAsync(request.RoleId);
            if (role == null)
            {
                await _bus.RaiseEvent(new DomainNotification(request.UserIds.ToString(), "未找到对应的数据"));
                return false;
            }

            var users = await _userRepository.GetWhereAsync(x => request.UserIds.Contains(x.Id));
            role.Users.AddRange(users);

            if (await Commit())
            {
                var key = GirvsEntityCacheDefaults<Role>.ByIdCacheKey.Create(role.Id.ToString());
                _bus.RaiseEvent(new RemoveCacheEvent(key), cancellationToken);
                _bus.RaiseEvent(new RemoveCacheListEvent(GirvsEntityCacheDefaults<Role>.ListCacheKey.Create()),
                    cancellationToken);
                _bus.RaiseEvent(new RemoveServiceCacheEvent(), cancellationToken);
            }

            return true;
        }

        public async Task<bool> Handle(DeleteRoleUserCommand request, CancellationToken cancellationToken)
        {
            var role = await _roleRepository.GetRoleByIdIncludeUsersAsync(request.RoleId);
            if (role == null)
            {
                await _bus.RaiseEvent(new DomainNotification(request.UserIds.ToString(), "未找到对应的数据"));
                return false;
            }

            role.Users.RemoveAll(x => request.UserIds.Contains(x.Id));

            if (await Commit())
            {
                var key = GirvsEntityCacheDefaults<Role>.ByIdCacheKey.Create(role.Id.ToString());
                _bus.RaiseEvent(new RemoveCacheEvent(key), cancellationToken);
                _bus.RaiseEvent(new RemoveCacheListEvent(GirvsEntityCacheDefaults<Role>.ListCacheKey.Create()),
                    cancellationToken);
                _bus.RaiseEvent(new RemoveServiceCacheEvent(), cancellationToken);
            }

            return true;
        }

        public async Task<bool> Handle(BatchDeleteRoleCommand request, CancellationToken cancellationToken)
        {
            var role = await _roleRepository.GetWhereAsync(x => request.Ids.Contains(x.Id));
            await _roleRepository.DeleteRangeAsync(role);
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
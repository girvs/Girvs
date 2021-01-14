using System;
using System.Threading;
using System.Threading.Tasks;
using Girvs.Domain.Caching.Events;
using Girvs.Domain.Caching.Interface;
using Girvs.Domain.Driven.Bus;
using Girvs.Domain.Driven.Commands;
using Girvs.Domain.Driven.Notifications;
using Girvs.Domain.IRepositories;
using Girvs.Domain.Managers;
using MediatR;
using Test.Domain.Commands.Role;
using Test.Domain.Models;

namespace Test.Domain.CommandHandlers
{
    public class RoleCommandHandler : CommandHandler,
        IRequestHandler<CreateRoleCommand, bool>,
        IRequestHandler<UpdateRoleCommand, bool>,
        IRequestHandler<DeleteRoleCommand, bool>
    {
        private readonly IRepository<Role> _roleRepository;
        private readonly ICacheKeyManager<Role> _cacheKeyManager;
        private readonly IMediatorHandler _bus;

        public RoleCommandHandler(
            IRepository<Role> roleRepository,
            ICacheKeyManager<Role> cacheKeyManager,
            IMediatorHandler bus,
            IUnitOfWork uow
        ) : base(uow, bus)
        {
            _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
            _cacheKeyManager = cacheKeyManager ?? throw new ArgumentNullException(nameof(cacheKeyManager));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        public async Task<bool> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
        {
            if (!request.IsValid())
            {
                NotifyValidationErrors(request);
                return false;
            }

            var role = new Role()
            {
                Name = request.Name,
                Desc = request.Desc
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
            if (!request.IsValid())
            {
                NotifyValidationErrors(request);
                return false;
            }

            var role = await _roleRepository.GetByIdAsync(request.Id);
            if (role == null)
            {
                await _bus.RaiseEvent(new DomainNotification("", "not find user entity"));
                return false;
            }

            role.Name = request.Name;
            role.Desc = request.Desc;

            var fields = new[]
            {
                nameof(Role.Name),
                nameof(Role.Desc),
            };

            await _roleRepository.UpdateAsync(role, fields);

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
                await _bus.RaiseEvent(new DomainNotification("", "not find user entity"));
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
    }
}
using System;
using System.Threading;
using System.Threading.Tasks;
using Girvs.Domain.Caching.Interface;
using Girvs.Domain.Driven.Bus;
using Girvs.Domain.Driven.Commands;
using Girvs.Domain.Driven.Notifications;
using Girvs.Domain.IRepositories;
using MediatR;
using Test.Domain.Commands.Role;
using Test.Domain.Models;

namespace Test.Domain.CommandHandlers
{
    public class RoleCommandHandler : CommandHandler,
        IRequestHandler<CreateRoleCommand, bool>,
        IRequestHandler<UpdateRoleCommand, bool>,
        IRequestHandler<DeleteRoleCommand,bool>
    {
        private readonly IRepository<Role> _roleRepository;
        private readonly IStaticCacheManager _staticCacheManager;
        private readonly ICacheKeyManager<Role> _cacheKeyManager;
        private readonly IMediatorHandler _mediator;

        public RoleCommandHandler(IRepository<Role> roleRepository,
            IStaticCacheManager staticCacheManager,
            ICacheKeyManager<Role> cacheKeyManager,
            IMediatorHandler mediator) : base(roleRepository.UnitOfWork,
            mediator)
        {
            _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
            _staticCacheManager = staticCacheManager ?? throw new ArgumentNullException(nameof(staticCacheManager));
            _cacheKeyManager = cacheKeyManager ?? throw new ArgumentNullException(nameof(cacheKeyManager));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
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
                _staticCacheManager.RemoveByPrefix(_cacheKeyManager.CacheKeyListPrefix);
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
                await _mediator.RaiseEvent(new DomainNotification("", "not find user entity"));
                return false;
            }
            
            role.Name = request.Name;
            role.Desc = request.Desc;

            var fields = new []
            {
                nameof(Role.Name),
                nameof(Role.Desc),
            };
            
            await _roleRepository.UpdateAsync(role, fields);
            
            if (await Commit())
            {
                _staticCacheManager.Remove(_cacheKeyManager.BuildCacheEntityKey(role.Id));
                _staticCacheManager.RemoveByPrefix(_cacheKeyManager.CacheKeyListPrefix);
            }

            return true;
        }

        public async Task<bool> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
        {
            var role = await _roleRepository.GetByIdAsync(request.Id);
            if (role == null)
            {
                await _mediator.RaiseEvent(new DomainNotification("", "not find user entity"));
                return false; 
            }

            await _roleRepository.DeleteAsync(role);

            if (await Commit())
            {
                _staticCacheManager.Remove(_cacheKeyManager.BuildCacheEntityKey(request.Id));
                _staticCacheManager.RemoveByPrefix(_cacheKeyManager.CacheKeyListPrefix);
            }

            return true;
        }
    }
}
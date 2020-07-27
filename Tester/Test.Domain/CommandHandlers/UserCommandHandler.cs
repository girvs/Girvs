using System;
using System.Threading;
using System.Threading.Tasks;
using Girvs.Domain.Caching.Interface;
using Girvs.Domain.Driven.Bus;
using Girvs.Domain.Driven.Commands;
using Girvs.Domain.Driven.Notifications;
using Girvs.Domain.Managers;
using MediatR;
using Test.Domain.Commands.User;
using Test.Domain.Models;
using Test.Domain.Repositories;

namespace Test.Domain.CommandHandlers
{
    public class UserCommandHandler : CommandHandler, IRequestHandler<CreateUserCommand, bool>,
        IRequestHandler<UpdateUserCommand, bool>, IRequestHandler<DeleteUserCommand, bool>,IRequestHandler<ChangeUserStateCommand,bool>
    {
        private readonly IMediatorHandler _bus;
        private readonly IUserRepository _userRepository;
        private readonly IStaticCacheManager _staticCacheManager;
        private readonly ICacheKeyManager<User> _cacheKeyManager;

        public UserCommandHandler(IUnitOfWork uow,
            IMediatorHandler bus,
            IUserRepository userRepository,
            IStaticCacheManager staticCacheManager,
            ICacheKeyManager<User> cacheKeyManager) : base(uow,
            bus)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _staticCacheManager = staticCacheManager ?? throw new ArgumentNullException(nameof(staticCacheManager));
            _cacheKeyManager = cacheKeyManager ?? throw new ArgumentNullException(nameof(cacheKeyManager));
        }

        public async Task<bool> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            if (!request.IsValid())
            {
                NotifyValidationErrors(request);
                return false;
            }

            var user = new User()
            {
                ContactNumber = request.ContactNumber,
                State = request.State,
                UserAccount = request.UserAccount,
                UserName = request.UserName,
                UserPassword = request.UserPassword,
                UserType = request.UserType
            };

            await _userRepository.AddAsync(user);
            if (await Commit())
            {
                _staticCacheManager.Set(_cacheKeyManager.BuildCacheEntityKey(user.Id), user, 60);
                _staticCacheManager.RemoveByPrefix(_cacheKeyManager.CacheKeyListPrefix);
                request.Id = user.Id;
            }

            return true;
        }

        public async Task<bool> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            if (!request.IsValid())
            {
                NotifyValidationErrors(request);
                return false;
            }

            var user = await _userRepository.GetByIdAsync(request.Id);
            if (user == null)
            {
                await _bus.RaiseEvent(new DomainNotification("", "not find user entity"));
                return false;
            }
            
            user.ContactNumber = request.ContactNumber;
            user.State = request.State;
            user.UserAccount = request.UserAccount;
            user.UserName = request.UserName;
            user.UserPassword = request.UserPassword;
            user.UserType = request.UserType;

            var fields = new []
            {
                nameof(User.ContactNumber),
                nameof(User.State),
                nameof(User.UserAccount),
                nameof(User.UserName),
                nameof(User.UserPassword),
                nameof(User.UserType)
            };
            
            await _userRepository.UpdateAsync(user, fields);
            
            if (await Commit())
            {
                _staticCacheManager.Remove(_cacheKeyManager.BuildCacheEntityKey(user.Id));
                _staticCacheManager.RemoveByPrefix(_cacheKeyManager.CacheKeyListPrefix);
            }

            return true;
        }

        public async Task<bool> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.Id);
            if (user == null)
            {
                await _bus.RaiseEvent(new DomainNotification("", "not find user entity"));
                return false;
            }

            await _userRepository.DeleteAsync(user);

            if (await  Commit())
            {
                _staticCacheManager.Remove(_cacheKeyManager.BuildCacheEntityKey(request.Id));
                _staticCacheManager.RemoveByPrefix(_cacheKeyManager.CacheKeyListPrefix);
            }

            return true;
        }

        public async Task<bool> Handle(ChangeUserStateCommand request, CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(request.Id);
            if (user == null)
            {
                await _bus.RaiseEvent(new DomainNotification("", "not find user entity"));
                return false;
            }

            user.State = request.State;

            await _userRepository.UpdateAsync(user, nameof(User.State));

            if (await Commit())
            {
                _staticCacheManager.Remove(_cacheKeyManager.BuildCacheEntityKey(request.Id));
                _staticCacheManager.RemoveByPrefix(_cacheKeyManager.CacheKeyListPrefix);
            }

            return true;
        }
    }
}
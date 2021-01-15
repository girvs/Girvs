using System;
using System.Threading;
using System.Threading.Tasks;
using Girvs.Domain;
using Girvs.Domain.Caching.Events;
using Girvs.Domain.Caching.Interface;
using Girvs.Domain.Driven.Bus;
using Girvs.Domain.Driven.Commands;
using Girvs.Domain.Driven.Notifications;
using Girvs.Domain.Managers;
using MediatR;
using Microsoft.AspNetCore.Http;
using Test.Domain.Commands.User;
using Test.Domain.Models;
using Test.Domain.Repositories;

namespace Test.Domain.CommandHandlers
{
    public class UserCommandHandler : CommandHandler, IRequestHandler<CreateUserCommand, bool>,
        IRequestHandler<UpdateUserCommand, bool>, IRequestHandler<DeleteUserCommand, bool>,
        IRequestHandler<ChangeUserStateCommand, bool>
    {
        private readonly IMediatorHandler _bus;
        private readonly IUserRepository _userRepository;
        private readonly ICacheKeyManager<User> _cacheKeyManager;

        public UserCommandHandler(
            IMediatorHandler bus,
            IUserRepository userRepository,
            ICacheKeyManager<User> cacheKeyManager,
            IUnitOfWork unitOfWork
            ) : base(unitOfWork,
            bus)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _cacheKeyManager = cacheKeyManager ?? throw new ArgumentNullException(nameof(cacheKeyManager));
        }

        public async Task<bool> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            var existUser = await _userRepository.GetUserByLoginNameAsync(request.UserAccount);

            if (existUser != null)
            {
                throw new GirvsException($"{request.UserAccount}登陆名称已存在", StatusCodes.Status422UnprocessableEntity);
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
                await _bus.RaiseEvent(new SetCacheEvent(user, _cacheKeyManager.BuildCacheEntityKey(user.Id),
                    _cacheKeyManager.CacheTime), cancellationToken);
                await _bus.RaiseEvent(new RemoveCacheListEvent(_cacheKeyManager.CacheKeyListPrefix), cancellationToken);
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

            var fields = new[]
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
                await _bus.RaiseEvent(new SetCacheEvent(user, _cacheKeyManager.BuildCacheEntityKey(user.Id),
                    _cacheKeyManager.CacheTime));
                await _bus.RaiseEvent(new RemoveCacheListEvent(_cacheKeyManager.CacheKeyListPrefix));
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

            if (await Commit())
            {
                await _bus.RaiseEvent(new RemoveCacheEvent(_cacheKeyManager.BuildCacheEntityKey(user.Id)));
                await _bus.RaiseEvent(new RemoveCacheListEvent(_cacheKeyManager.CacheKeyListPrefix));
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
                await _bus.RaiseEvent(new SetCacheEvent(user, _cacheKeyManager.BuildCacheEntityKey(user.Id),
                    _cacheKeyManager.CacheTime));
                await _bus.RaiseEvent(new RemoveCacheListEvent(_cacheKeyManager.CacheKeyListPrefix));
            }

            return true;
        }
    }
}
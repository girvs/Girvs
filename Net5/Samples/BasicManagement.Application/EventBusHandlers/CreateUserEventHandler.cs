using System;
using System.Threading.Tasks;
using BasicManagement.Domain.Commands.User;
using BasicManagement.Domain.Enumerations;
using BasicManagement.Domain.Models;
using BasicManagement.Domain.Repositories;
using Girvs.Driven.Bus;
using Girvs.Driven.Notifications;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BasicManagement.Application.EventBusHandlers
{
    public class CreateUserEventHandler : IIntegrationEventHandler<CreateUserEvent>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILocker _locker;
        private readonly IMediatorHandler _bus;
        private readonly ICacheKeyManager<User> _cacheKeyManager;
        private readonly ILogger<CreateUserEventHandler> _logger;
        private DomainNotificationHandler _notifications;

        public CreateUserEventHandler(
            IUserRepository userRepository,
            ILocker locker,
            IMediatorHandler bus,
            INotificationHandler<DomainNotification> notifications,
            ICacheKeyManager<User> cacheKeyManager,
            ILogger<CreateUserEventHandler> logger
        )
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _locker = locker ?? throw new ArgumentNullException(nameof(locker));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _cacheKeyManager = cacheKeyManager ?? throw new ArgumentNullException(nameof(cacheKeyManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _notifications = notifications as DomainNotificationHandler ??
                             throw new ArgumentNullException(nameof(notifications));
        }

        [CapSubscribe(nameof(CreateUserEvent))]
        public Task Handle(CreateUserEvent @event)
        {
            _logger.LogInformation("Handling 'CreateUserEvent' event", @event.Id, AppDomain.CurrentDomain.FriendlyName,
                @event);
            TimeSpan timeSpan = TimeSpan.FromMinutes(_cacheKeyManager.CacheTime);
            _locker.PerformActionWithLock(_cacheKeyManager.BuilerRedisEventLockKey(@event),
                timeSpan,  () =>
                {
                    var existUser = _userRepository.ExistEntityAsync(x =>
                        x.OtherId == @event.OtherId || x.UserAccount == @event.UserAccount).Result;

                    if (existUser)
                    {
                        _logger.LogError($"已存在OtherId:{@event.OtherId},或者UserAccount:{@event.UserAccount} 对象", @event);
                    }
                    else
                    {
                        if (Guid.Empty.Equals(@event.OtherId))
                        {
                            _logger.LogError($"OtherId:{@event.OtherId} 错误，不能为空！", @event);
                            return;
                        }
                        var command = new CreateUserCommand(
                            @event.UserAccount,
                            @event.UserPassword.ToMd5(),
                            @event.UserName,
                            @event.ContactNumber,
                            DataState.Enable,
                            UserType.UnitPersion,
                            @event.OtherId
                        );

                        _bus.SendCommand(command).Wait();
                        if (_notifications.HasNotifications())
                        {
                            var (errorCode, errorMessage) = _notifications.GetNotificationMessage();
                            _logger.LogError(
                                $"Handling 'CreateUserEvent' event Error Code:{errorCode},Message:{errorMessage}",
                                @event);
                        }
                    }
                });
            return Task.CompletedTask;
        }
    }
}
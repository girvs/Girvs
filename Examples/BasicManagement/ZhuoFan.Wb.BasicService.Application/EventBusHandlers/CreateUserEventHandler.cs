using System;
using System.Threading.Tasks;
using DotNetCore.CAP;
using Girvs.Cache.Caching;
using Girvs.Cache.Configuration;
using Girvs.Configuration;
using Girvs.Driven.Bus;
using Girvs.Driven.Notifications;
using Girvs.EventBus;
using Girvs.Extensions;
using Girvs.Infrastructure;
using MediatR;
using Microsoft.Extensions.Logging;
using Users;
using ZhuoFan.Wb.BasicService.Domain.Commands.User;
using ZhuoFan.Wb.BasicService.Domain.Enumerations;
using ZhuoFan.Wb.BasicService.Domain.Repositories;
using UserType = ZhuoFan.Wb.BasicService.Domain.Enumerations.UserType;

namespace ZhuoFan.Wb.BasicService.Application.EventBusHandlers
{
    public class CreateUserEventHandler : IIntegrationEventHandler<CreateUserEvent>
    {
        private readonly IUserRepository _userRepository;
        private readonly ILocker _locker;
        private readonly IMediatorHandler _bus;
        private readonly ILogger<CreateUserEventHandler> _logger;
        private DomainNotificationHandler _notifications;

        public CreateUserEventHandler(
            IUserRepository userRepository,
            ILocker locker,
            IMediatorHandler bus,
            INotificationHandler<DomainNotification> notifications,
            ILogger<CreateUserEventHandler> logger
        )
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _locker = locker ?? throw new ArgumentNullException(nameof(locker));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _notifications = notifications as DomainNotificationHandler ??
                             throw new ArgumentNullException(nameof(notifications));
        }

        [CapSubscribe(nameof(CreateUserEvent))]
        public Task Handle(CreateUserEvent @event ,[FromCap]CapHeader header)
        {
            _logger.LogInformation("Handling 'CreateUserEvent' event", @event.Id, AppDomain.CurrentDomain.FriendlyName,
                @event);

            var cacheConfig = Singleton<AppSettings>.Instance[nameof(CacheConfig)] as CacheConfig;
            TimeSpan timeSpan = TimeSpan.FromMinutes(cacheConfig.CacheBaseConfig.DefaultCacheTime);
            _locker.PerformActionWithLock(@event.Id.ToString(),
                timeSpan, () =>
                {
                    var existUser = _userRepository.ExistEntityAsync(x => x.UserAccount == @event.UserAccount).Result;

                    if (existUser)
                    {
                        _logger.LogError($"已存在UserAccount:{@event.UserAccount} 对象", @event);
                    }
                    else
                    {
                        var command = new CreateUserCommand(
                            @event.UserAccount,
                            @event.UserPassword.ToMd5(),
                            @event.UserName,
                            @event.ContactNumber,
                            DataState.Enable,
                            (UserType) (int) @event.UserType
                        );

                        _bus.SendCommand(command).Wait();
                        if (_notifications.HasNotifications())
                        {
                            var errorMessage = _notifications.GetNotificationMessage();
                            _logger.LogError(
                                $"Handling 'CreateUserEvent' event Error Code:{400},Message:{errorMessage}",
                                @event);
                        }
                    }
                });
            return Task.CompletedTask;
        }
    }
}
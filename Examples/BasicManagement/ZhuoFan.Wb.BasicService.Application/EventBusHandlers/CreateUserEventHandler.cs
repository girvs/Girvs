using System;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP;
using Girvs.AuthorizePermission.Enumerations;
using Girvs.Cache.Caching;
using Girvs.Cache.Configuration;
using Girvs.Driven.Bus;
using Girvs.Driven.Notifications;
using Girvs.EventBus;
using Girvs.EventBus.Extensions;
using Girvs.Extensions;
using Girvs.Infrastructure;
using MediatR;
using Microsoft.Extensions.Logging;
using ZhuoFan.Wb.BasicService.Domain.Commands.User;
using ZhuoFan.Wb.BasicService.Domain.Enumerations;
using ZhuoFan.Wb.BasicService.Domain.Repositories;
using ZhuoFan.Wb.Common.Events.Users;

namespace ZhuoFan.Wb.BasicService.Application.EventBusHandlers
{
    public class CreateUserEventHandler : IIntegrationEventHandler<CreateUserEvent>
    {
        private readonly ILocker _locker;
        private readonly IMediatorHandler _bus;
        private readonly ILogger<CreateUserEventHandler> _logger;
        private DomainNotificationHandler _notifications;

        public CreateUserEventHandler(
            ILocker locker,
            IMediatorHandler bus,
            INotificationHandler<DomainNotification> notifications,
            ILogger<CreateUserEventHandler> logger
        )
        {
            _locker = locker ?? throw new ArgumentNullException(nameof(locker));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _notifications = notifications as DomainNotificationHandler ??
                             throw new ArgumentNullException(nameof(notifications));
        }

        [CapSubscribe(nameof(CreateUserEvent))]
        public Task Handle(CreateUserEvent @event, [FromCap] CapHeader header, CancellationToken cancellationToken)
        {
            //需要重新设置身份认证头
            EngineContext.Current.ClaimManager.CapEventBusReSetClaim(header);

            _logger.LogInformation($"Handling 'CreateUserEvent' eventId:{@event.Id}");

            var cacheConfig = EngineContext.Current.GetAppModuleConfig<CacheConfig>();
            var timeSpan = TimeSpan.FromSeconds(cacheConfig.CacheBaseConfig.DefaultCacheTime);

            _locker.PerformActionWithLock(@event.Id.ToString(),
                timeSpan, () =>
                {
                    var command = new EventCreateUserCommand(
                        @event.UserAccount,
                        @event.UserPassword.ToMd5(),
                        @event.UserName,
                        @event.ContactNumber,
                        DataState.Enable,
                        UserType.TenantAdminUser, //此处创建的为租户管理员
                        @event.TenantId
                    );
                    _bus.SendCommand(command, cancellationToken).Wait(cancellationToken);
                    if (_notifications.HasNotifications())
                    {
                        var errorMessage = _notifications.GetNotificationMessage();
                        _logger.LogError(
                            $"Handling 'CreateUserEvent' event Error Code:{400},Message:{errorMessage}",
                            @event);
                    }
                });
            return Task.CompletedTask;
        }
    }
}
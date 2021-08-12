using System;
using System.Threading.Tasks;
using DotNetCore.CAP;
using Girvs.Cache.Caching;
using Girvs.Cache.Configuration;
using Girvs.Configuration;
using Girvs.Driven.Bus;
using Girvs.Driven.Notifications;
using Girvs.EventBus;
using Girvs.EventBus.Extensions;
using Girvs.Infrastructure;
using JetBrains.Annotations;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Power.EventBus.Permission;
using ZhuoFan.Wb.BasicService.Domain.Commands.ServiceDataRule;
using ZhuoFan.Wb.BasicService.Domain.Commands.ServicePermission;
using ZhuoFan.Wb.BasicService.Domain.Enumerations;

namespace ZhuoFan.Wb.BasicService.Application.EventBusHandlers
{
    public class PermissionAuthorizeEventHandler : IIntegrationEventHandler<PermissionAuthorizeEvent>
    {
        private readonly ILocker _locker;
        private readonly ILogger<PermissionAuthorizeEventHandler> _logger;
        private readonly IMediatorHandler _bus;
        private DomainNotificationHandler _notifications;

        public PermissionAuthorizeEventHandler(
            ILocker locker,
            ILogger<PermissionAuthorizeEventHandler> logger,
            [NotNull] IMediatorHandler bus,
            INotificationHandler<DomainNotification> notifications
        )
        {
            _locker = locker ?? throw new ArgumentNullException(nameof(locker));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _notifications = notifications as DomainNotificationHandler ??
                             throw new ArgumentNullException(nameof(notifications));
        }


        [CapSubscribe(nameof(PermissionAuthorizeEvent))]
        public Task Handle(PermissionAuthorizeEvent @event, [FromCap] CapHeader header)
        {
            //需要重新设置身份认证头
            EngineContext.Current.ClaimManager.CapEventBusReSetClaim(header);

            _logger.LogInformation("Handling 'PermissionAuthorizeEvent' event", @event.Id,
                AppDomain.CurrentDomain.FriendlyName,
                @event);
            var cacheConfig = Singleton<AppSettings>.Instance[nameof(CacheConfig)] as CacheConfig;
            var timeSpan = TimeSpan.FromMinutes(cacheConfig.CacheBaseConfig.DefaultCacheTime);

            _locker.PerformActionWithLock(@event.Id.ToString(),
                timeSpan, async () =>
                {
                    foreach (var permissionAuthoriz in @event.PermissionAuthorizes)
                    {
                        var servicePermissionCommand = new CreateOrUpdateServicePermissionCommand(
                            permissionAuthoriz.ServiceName, permissionAuthoriz.ServiceId,
                            permissionAuthoriz.Permissions);

                        await _bus.SendCommand(servicePermissionCommand);
                    }


                    foreach (var authorizeUserRule in @event.AuthorizeUserRules)
                    {
                        var createOrUpdateServiceDataRuleCommand = new CreateOrUpdateServiceDataRuleCommand(
                            authorizeUserRule.ServiceName,
                            authorizeUserRule.ModuleName,
                            (UserType)(int)authorizeUserRule.UserType,
                            authorizeUserRule.DataSource,
                            authorizeUserRule.FieldName,
                            authorizeUserRule.FieldDesc
                        );
                        await _bus.SendCommand(createOrUpdateServiceDataRuleCommand);
                    }


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
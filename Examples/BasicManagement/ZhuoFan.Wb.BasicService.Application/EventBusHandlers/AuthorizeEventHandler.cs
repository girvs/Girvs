using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP;
using Girvs.Cache.Caching;
using Girvs.Cache.Configuration;
using Girvs.Driven.Bus;
using Girvs.Driven.Notifications;
using Girvs.EventBus;
using Girvs.EventBus.Extensions;
using Girvs.Infrastructure;
using JetBrains.Annotations;
using MediatR;
using Microsoft.Extensions.Logging;
using ZhuoFan.Wb.BasicService.Domain.Commands.Authorize;
using ZhuoFan.Wb.Common.Events.Authorize;

namespace ZhuoFan.Wb.BasicService.Application.EventBusHandlers
{
    public class AuthorizeEventHandler : IIntegrationEventHandler<AuthorizeEvent>
    {
        private readonly ILocker _locker;
        private readonly ILogger<AuthorizeEventHandler> _logger;
        private readonly IMediatorHandler _bus;
        private DomainNotificationHandler _notifications;

        public AuthorizeEventHandler(
            ILocker locker,
            ILogger<AuthorizeEventHandler> logger,
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

        [CapSubscribe(nameof(AuthorizeEvent))]
        public Task Handle(AuthorizeEvent @event, [FromCap] CapHeader header, CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Handling 'AuthorizeEvent' eventId:{@event.Id}");
            //需要重新设置身份认证头
            EngineContext.Current.ClaimManager.CapEventBusReSetClaim(header);

            var cacheConfig = EngineContext.Current.GetAppModuleConfig<CacheConfig>();

            var timeSpan = TimeSpan.FromSeconds(cacheConfig.CacheBaseConfig.DefaultCacheTime);

            var isLock = _locker.PerformActionWithLock(@event.Id.ToString(),
                timeSpan, () =>
                {
                    var servicePermissionCommandModels = new List<ServicePermissionCommandModel>();
                    foreach (var permissionAuthoriz in @event.AuthorizePermissions)
                    {
                        servicePermissionCommandModels.Add(new ServicePermissionCommandModel()
                        {
                            ServiceName = permissionAuthoriz.ServiceName,
                            ServiceId = permissionAuthoriz.ServiceId,
                            Permissions = permissionAuthoriz.Permissions,
                            OperationPermissionModels = permissionAuthoriz.OperationPermissionModels
                        });
                    }

                    var serviceDataRuleCommandModels = new List<ServiceDataRuleCommandModel>();

                    foreach (var authorizeDataRule in @event.AuthorizeDataRules)
                    {
                        foreach (var dataRuleFieldModel in authorizeDataRule.AuthorizeDataRuleFieldModels)
                        {
                            serviceDataRuleCommandModels.Add(new ServiceDataRuleCommandModel()
                            {
                                EntityTypeName = authorizeDataRule.EntityTypeName,
                                EntityDesc = authorizeDataRule.EntityDesc,
                                FieldName = dataRuleFieldModel.FieldName,
                                FieldType = dataRuleFieldModel.FieldType,
                                FieldValue = dataRuleFieldModel.FieldValue,
                                ExpressionType = dataRuleFieldModel.ExpressionType,
                                FieldDesc = dataRuleFieldModel.FieldDesc,
                                UserType = dataRuleFieldModel.UserType
                            });
                        }
                    }

                    var command =
                        new NeedAuthorizeListCommand(servicePermissionCommandModels, serviceDataRuleCommandModels);

                    _bus.SendCommand(command).Wait(cancellationToken);

                    if (_notifications.HasNotifications())
                    {
                        var errorMessage = _notifications.GetNotificationMessage();
                        _logger.LogError(
                            $"Handling 'AuthorizeEvent' event Error Code:{400},Message:{errorMessage}",
                            @event);
                    }
                });

            _logger.LogInformation($"本次事件处理过程中，Reids锁的情况为：{isLock} 事件ID为：{@event.Id}");


            return Task.CompletedTask;
        }
    }
}
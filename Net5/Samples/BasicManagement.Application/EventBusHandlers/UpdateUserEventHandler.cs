using System;
using System.Threading.Tasks;

namespace BasicManagement.Application.EventBusHandlers
{
    public class UpdateUserEventHandler : IIntegrationEventHandler<UpdateUserEvent>
    {
        private readonly ILocker _locker;
        private readonly IMediatorHandler _bus;
        private readonly ICacheKeyManager<User> _cacheKeyManager;
        private readonly ILogger<CreateUserEventHandler> _logger;
        private DomainNotificationHandler _notifications;

        public UpdateUserEventHandler(
            ILocker locker,
            IMediatorHandler bus,
            INotificationHandler<DomainNotification> notifications,
            ICacheKeyManager<User> cacheKeyManager,
            ILogger<CreateUserEventHandler> logger
        )
        {
            _locker = locker ?? throw new ArgumentNullException(nameof(locker));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _cacheKeyManager = cacheKeyManager ?? throw new ArgumentNullException(nameof(cacheKeyManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _notifications = notifications as DomainNotificationHandler ??
                             throw new ArgumentNullException(nameof(notifications));
        }

        [CapSubscribe(nameof(UpdateUserEvent))]
        public Task Handle(UpdateUserEvent @event)
        {
            _logger.LogInformation("Handling 'UpdateUserEvent' event", @event.Id, AppDomain.CurrentDomain.FriendlyName,
                @event);
            TimeSpan timeSpan = TimeSpan.FromMinutes(_cacheKeyManager.CacheTime);
            _locker.PerformActionWithLock(_cacheKeyManager.BuilerRedisEventLockKey(@event),
                timeSpan, () =>
                {
                    var command = new UpdateUserEventCommand(
                        
                        @event.OtherId,
                        @event.UserName,
                        @event.ContactNumber
                    );

                    _bus.SendCommand(command).Wait();
                    if (_notifications.HasNotifications())
                    {
                        var (errorCode, errorMessage) = _notifications.GetNotificationMessage();
                        _logger.LogError(
                            $"Handling 'UpdateUserEvent' event Error Code:{errorCode},Message:{errorMessage}",
                            @event);
                    }
                });
            return Task.CompletedTask;
        }
    }
}
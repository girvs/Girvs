using System;
using System.Threading.Tasks;

namespace BasicManagement.Application.EventBusHandlers
{
    public class PermissionAuthorizeEventHandler : IIntegrationEventHandler<PermissionAuthorizeEvent>
    {
        private readonly IServicePermissionAuthorizeStore _sotre;
        private readonly ILocker _locker;
        private readonly ICacheKeyManager<AuthorizePermissionModel> _cacheKeyManager;
        private readonly ILogger<PermissionAuthorizeEventHandler> _logger;

        public PermissionAuthorizeEventHandler(
            IServicePermissionAuthorizeStore sotre,
            ILocker locker,
            ICacheKeyManager<AuthorizePermissionModel> cacheKeyManager,
            ILogger<PermissionAuthorizeEventHandler> logger
        )
        {
            _sotre = sotre ?? throw new ArgumentNullException(nameof(sotre));
            _locker = locker ?? throw new ArgumentNullException(nameof(locker));
            _cacheKeyManager = cacheKeyManager ?? throw new ArgumentNullException(nameof(cacheKeyManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }


        [CapSubscribe(nameof(PermissionAuthorizeEvent))]
        public Task Handle(PermissionAuthorizeEvent @event)
        {
            _logger.LogInformation("Handling 'PermissionAuthorizeEvent' event", @event.Id,
                AppDomain.CurrentDomain.FriendlyName,
                @event);
            TimeSpan timeSpan = TimeSpan.FromMinutes(_cacheKeyManager.CacheTime);
            _locker.PerformActionWithLock(_cacheKeyManager.BuilerRedisEventLockKey(@event),
                timeSpan, () =>
                {
                    _sotre.CreateOrUpdate(@event.PermissionAuthorizes.Select(p => new AuthorizePermissionModel()
                    {
                        ServiceId = p.ServiceId,
                        ServiceName = p.ServiceName,
                        Permissions = p.Permissions
                    }).ToList());
                });
            return Task.CompletedTask;
        }
    }
}
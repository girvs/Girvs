using System;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP;
using Girvs.AuthorizePermission;
using Girvs.Cache.Caching;
using Girvs.EventBus;
using Girvs.EventBus.Extensions;
using Girvs.Infrastructure;
using Microsoft.Extensions.Logging;
using ZhuoFan.Wb.Common.Events.Authorize;

namespace ZhuoFan.Wb.Common.CommHanlders
{
    public class RemoveAuthorizeCacheEventHanlder : IIntegrationEventHandler<RemoveAuthorizeCacheEvent>
    {
        private readonly IStaticCacheManager _staticCacheManager;
        private readonly ILogger<RemoveAuthorizeCacheEventHanlder> _logger;
        private readonly ILocker _locker;

        public RemoveAuthorizeCacheEventHanlder(
            IStaticCacheManager staticCacheManager,
            ILogger<RemoveAuthorizeCacheEventHanlder> logger,
            ILocker locker
        )
        {
            _staticCacheManager = staticCacheManager ?? throw new ArgumentNullException(nameof(staticCacheManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _locker = locker ?? throw new ArgumentNullException(nameof(locker));
        }

        public Task Handle(RemoveAuthorizeCacheEvent @event, [FromCap] CapHeader header,
            CancellationToken cancellationToken)
        {
            EngineContext.Current.ClaimManager.CapEventBusReSetClaim(header);
            _locker.PerformActionWithLock(@event.Id.ToString(),
                TimeSpan.FromMinutes(30),
                () =>
                {
                    try
                    {
                        _staticCacheManager.RemoveByPrefix(GirvsAuthorizePermissionCacheKeyManager
                            .CurrentUserAuthorizeCacheKeyPrefix);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e.Message, e);
                    }
                });
            return Task.CompletedTask;
        }
    }
}
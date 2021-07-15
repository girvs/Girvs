using System;
using System.Threading;
using System.Threading.Tasks;
using Girvs.Cache.Caching;
using Girvs.Driven.Bus;
using Girvs.Driven.CacheDriven.Commands;
using Girvs.Driven.Commands;
using MediatR;

namespace Girvs.Driven.CacheDriven.CommandHandlers
{
    public class RemoveByPrefixCommandHandler : CommandHandler, IRequestHandler<RemoveByPrefixCommand, bool>
    {
        private readonly IStaticCacheManager _staticCacheManager;

        public RemoveByPrefixCommandHandler(IStaticCacheManager staticCacheManager, IMediatorHandler bus) : base(null, bus)
        {
            _staticCacheManager = staticCacheManager ?? throw new ArgumentNullException(nameof(staticCacheManager));
        }

        public Task<bool> Handle(RemoveByPrefixCommand request, CancellationToken cancellationToken)
        {
            _staticCacheManager.RemoveByPrefix(request.Prefix);
            return Task.FromResult(true);
        }
    }
}
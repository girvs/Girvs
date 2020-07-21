using System;
using System.Threading;
using System.Threading.Tasks;
using Girvs.Domain.Caching.Commands;
using Girvs.Domain.Caching.Interface;
using MediatR;

namespace Girvs.Domain.Caching.CommandHandlers
{
    public class RemoveByKeyCommandHandler : IRequestHandler<RemoveByKeyCommand, bool>
    {
        private readonly IStaticCacheManager _staticCacheManager;

        public RemoveByKeyCommandHandler(IStaticCacheManager staticCacheManager)
        {
            _staticCacheManager = staticCacheManager ?? throw new ArgumentNullException(nameof(staticCacheManager));
        }

        public Task<bool> Handle(RemoveByKeyCommand request, CancellationToken cancellationToken)
        {
            _staticCacheManager.Remove(request.Key);
            return Task.FromResult(true);
        }
    }
}
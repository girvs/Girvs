using System;
using System.Threading;
using System.Threading.Tasks;
using Girvs.Domain.Caching.Commands;
using Girvs.Domain.Caching.Interface;
using Girvs.Domain.Driven.Bus;
using Girvs.Domain.Driven.Commands;
using Girvs.Domain.Managers;
using MediatR;

namespace Girvs.Domain.Caching.CommandHandlers
{
    public class RemoveByPrefixCommandHandler : CommandHandler, IRequestHandler<RemoveByPrefixCommand, bool>
    {
        private readonly IStaticCacheManager _staticCacheManager;

        public RemoveByPrefixCommandHandler(IStaticCacheManager staticCacheManager, IUnitOfWork uow, IMediatorHandler bus) : base(uow, bus)
        {
            _staticCacheManager = staticCacheManager ?? throw new ArgumentNullException(nameof(staticCacheManager));
        }

        public Task<bool> Handle(RemoveByPrefixCommand request, CancellationToken cancellationToken)
        {
            _staticCacheManager.Remove(request.Prefix);
            return Task.FromResult(true);
        }
    }
}
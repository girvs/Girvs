namespace Girvs.Driven.CacheDriven.CommandHandlers;

public class RemoveByKeyCommandHandler(IStaticCacheManager staticCacheManager, IMediatorHandler bus)
    : CommandHandler(null, bus),
        IRequestHandler<RemoveByKeyCommand, bool>
{
    public async Task<bool> Handle(RemoveByKeyCommand request, CancellationToken cancellationToken)
    {
        await staticCacheManager.RemoveAsync(new CacheKey(request.Key));
        return true;
    }
}

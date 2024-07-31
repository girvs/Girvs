namespace Girvs.Driven.CacheDriven.CommandHandlers;

public class RemoveByKeyCommandHandler(IStaticCacheManager staticCacheManager, IMediatorHandler bus)
    : CommandHandler(null, bus),
        IRequestHandler<RemoveByKeyCommand, bool>
{
    private readonly IStaticCacheManager _staticCacheManager =
        staticCacheManager ?? throw new ArgumentNullException(nameof(staticCacheManager));

    public Task<bool> Handle(RemoveByKeyCommand request, CancellationToken cancellationToken)
    {
        _staticCacheManager.Remove(new CacheKey(request.Key));
        return Task.FromResult(true);
    }
}

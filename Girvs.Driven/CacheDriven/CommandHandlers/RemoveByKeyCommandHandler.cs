namespace Girvs.Driven.CacheDriven.CommandHandlers;

public class RemoveByKeyCommandHandler : CommandHandler, IRequestHandler<RemoveByKeyCommand, bool>
{
    private readonly IStaticCacheManager _staticCacheManager;

    public RemoveByKeyCommandHandler(IStaticCacheManager staticCacheManager, IMediatorHandler bus) : base(null, bus)
    {
        _staticCacheManager = staticCacheManager ?? throw new ArgumentNullException(nameof(staticCacheManager));
    }

    public Task<bool> Handle(RemoveByKeyCommand request, CancellationToken cancellationToken)
    {
        _staticCacheManager.Remove(new CacheKey(request.Key));
        return Task.FromResult(true);
    }

}
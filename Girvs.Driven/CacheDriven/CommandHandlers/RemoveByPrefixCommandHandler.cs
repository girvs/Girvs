namespace Girvs.Driven.CacheDriven.CommandHandlers;

public class RemoveByPrefixCommandHandler(
    IStaticCacheManager staticCacheManager,
    IMediatorHandler bus
) : CommandHandler(null, bus), IRequestHandler<RemoveByPrefixCommand, bool>
{
    private readonly IStaticCacheManager _staticCacheManager =
        staticCacheManager ?? throw new ArgumentNullException(nameof(staticCacheManager));

    public Task<bool> Handle(RemoveByPrefixCommand request, CancellationToken cancellationToken)
    {
        _staticCacheManager.RemoveByPrefix(request.Prefix);
        return Task.FromResult(true);
    }
}

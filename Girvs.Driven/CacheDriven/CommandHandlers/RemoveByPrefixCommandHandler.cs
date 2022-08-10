namespace Girvs.Driven.CacheDriven.CommandHandlers;

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
namespace Girvs.Driven.CacheDriven.CommandHandlers;

public class RemoveByPrefixCommandHandler(
    IStaticCacheManager staticCacheManager,
    IMediatorHandler bus
) : CommandHandler(null, bus), IRequestHandler<RemoveByPrefixCommand, bool>
{
    public Task<bool> Handle(RemoveByPrefixCommand request, CancellationToken cancellationToken)
    {
        staticCacheManager.RemoveByPrefix(request.Prefix);
        return Task.FromResult(true);
    }
}

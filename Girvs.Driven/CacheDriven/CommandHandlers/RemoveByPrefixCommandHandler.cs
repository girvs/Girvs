namespace Girvs.Driven.CacheDriven.CommandHandlers;

public class RemoveByPrefixCommandHandler(
    IStaticCacheManager staticCacheManager,
    IMediatorHandler bus
) : CommandHandler(null, bus), IRequestHandler<RemoveByPrefixCommand, bool>
{
    public async Task<bool> Handle(RemoveByPrefixCommand request, CancellationToken cancellationToken)
    {
        // 改用异步缓存接口，消除同步阻塞
        await staticCacheManager.RemoveByPrefixAsync(request.Prefix);
        return true;
    }
}

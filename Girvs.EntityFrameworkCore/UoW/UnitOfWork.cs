namespace Girvs.EntityFrameworkCore.UoW;

public class UnitOfWork<TEntity> : IUnitOfWork<TEntity> where TEntity : Entity
{
    private readonly DbContext _context;
    private readonly ILogger<UnitOfWork<TEntity>> _logger;

    //构造函数注入
    public UnitOfWork()
    {
        var related = EngineContext.Current.GetShardingTableRelatedByEntity<TEntity>();
        _context = related.GetInstant() ??
                   throw new ArgumentNullException(nameof(DbContext));
        _context.SwitchReadWriteDataBase(DataBaseWriteAndRead.Write);
        _logger = EngineContext.Current.Resolve<ILogger<UnitOfWork<TEntity>>>();
    }

    //手动回收
    public void Dispose()
    {
        _context.Dispose();
    }

    public async Task<bool> Commit(CancellationToken cancellationToken = new CancellationToken())
    {
        var changeRowCount = await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            $"提交保存时的DBContextId为：{(_context as DbContext)?.ContextId.InstanceId.ToString()},保存时影响的行数为：{changeRowCount}");

        return changeRowCount > 0;
    }
}
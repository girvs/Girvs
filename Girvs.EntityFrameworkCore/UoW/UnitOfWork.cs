namespace Girvs.EntityFrameworkCore.UoW;

public class UnitOfWork<TEntity> : IUnitOfWork<TEntity> where TEntity : Entity
{
    private readonly DbContext _context;
    private readonly ILogger<UnitOfWork<TEntity>> _logger;

    //构造函数注入
    public UnitOfWork()
    {
        var related = EngineContext.Current.GetShardingTableRelatedByEntity<TEntity>();
        _context = related.GetInstance() ??
                   throw new ArgumentNullException(nameof(DbContext));
        _context.SwitchReadWriteDataBase(DataBaseWriteAndRead.Write);
        _logger = EngineContext.Current.Resolve<ILogger<UnitOfWork<TEntity>>>();
    }

    //手动回收
    public void Dispose()
    {
        // _context.Dispose();
    }

    public async Task<bool> Commit(CancellationToken cancellationToken = new CancellationToken())
    {
        var changeRowCount = await _context.SaveChangesAsync(cancellationToken);
        // 使用结构化模板，日志级别关闭时不产生插值字符串与装箱。
        _logger.LogInformation(
            "提交保存时的 DbContextId 为 {DbContextId}，保存时影响的行数为 {ChangeRowCount}",
            _context.ContextId.InstanceId,
            changeRowCount
        );

        return changeRowCount > 0;
    }
}
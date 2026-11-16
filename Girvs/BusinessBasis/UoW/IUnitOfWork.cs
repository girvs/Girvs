namespace Girvs.BusinessBasis.UoW;

/// <summary>
/// 工作单元，方便多操作事务至业务层
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// 保存当前上下文的跟踪变更；返回值仅表示本次保存是否产生变更。
    /// 批量更新/删除已立即执行，不计入返回值；显式事务须由其调用方提交或回滚。
    /// </summary>
    Task<bool> Commit(CancellationToken cancellationToken = default(CancellationToken));
}

/// <summary>
/// 工作单元，方便多操作事务至业务层
/// </summary>
public interface IUnitOfWork<TEntity> : IUnitOfWork where TEntity : Entity
{
}

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Girvs.Domain.Managers
{
    /// <summary>
    /// 工作单元，方便多操作事务至业务层
    /// </summary>
    public interface IUnitOfWork : IManager, IDisposable
    {
        //是否提交成功
        Task<bool> Commit(CancellationToken cancellationToken = default(CancellationToken));
    }
}
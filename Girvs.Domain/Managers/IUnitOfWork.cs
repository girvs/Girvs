using System;
using System.Threading.Tasks;

namespace Girvs.Domain.Managers
{
    public interface IUnitOfWork : IManager,IDisposable
    {
        //是否提交成功
        Task<bool> Commit();
    }
}
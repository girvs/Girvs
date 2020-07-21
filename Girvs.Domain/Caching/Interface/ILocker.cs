using System;

namespace Girvs.Domain.Caching.Interface
{
    public interface ILocker
    {
        /// <summary>
        /// 使用独占锁执行某些操作
        /// </summary>
        /// <param name="resource">我们锁定的关键</param>
        /// <param name="expirationTime">锁定将自动过期的时间</param>
        /// <param name="action">要执行锁定的操作</param>
        /// <returns>如果获得锁定并执行操作，则为True;否则是假的</returns>
        bool PerformActionWithLock(string resource, TimeSpan expirationTime, Action action);
    }
}

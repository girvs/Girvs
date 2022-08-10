namespace Girvs.Cache.Caching;

public interface ILocker
{
    /// <summary>
    /// Perform some action with exclusive lock
    /// </summary>
    /// <param name="key">The key we are locking on</param>
    /// <param name="expirationTime">The time after which the lock will automatically be expired</param>
    /// <param name="action">Action to be performed with locking</param>
    /// <returns>True if lock was acquired and action was performed; otherwise false</returns>
    Task<bool> PerformActionWithLock(string key, TimeSpan expirationTime, Func<Task> action);
}
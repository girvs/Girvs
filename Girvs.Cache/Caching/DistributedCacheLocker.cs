using Girvs.Cache.CacheImps;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Newtonsoft.Json;

namespace Girvs.Cache.Caching;

public partial class DistributedCacheLocker(IDistributedCache distributedCache) : ILocker
{
    protected static readonly string Running = JsonConvert.SerializeObject(TaskStatus.Running);

    #region Methods

    /// <summary>
    /// Performs some asynchronous task with exclusive lock
    /// </summary>
    /// <param name="resource">The key we are locking on</param>
    /// <param name="expirationTime">The time after which the lock will automatically be expired</param>
    /// <param name="action">Asynchronous task to be performed with locking</param>
    /// <param name="immediateLockDispose"></param>
    /// <returns>A task that resolves true if lock was acquired and action was performed; otherwise false</returns>
    public async Task<bool> PerformActionWithLockAsync(
        string resource,
        TimeSpan expirationTime,
        Func<Task> action,
        bool immediateLockDispose = true
    )
    {
        //ensure that lock is acquired
        if (!string.IsNullOrEmpty(await distributedCache.GetStringAsync(resource)))
            return false;

        try
        {
            var isAcquired = await Acquired(resource, expirationTime);
            //如果没有获取到锁，直接返回
            if (!isAcquired)
                return false;

            await action();

            if (immediateLockDispose)
                //release lock even if action fails
                await distributedCache.RemoveAsync(resource);

            return true;
        }
        catch
        {
            //release lock even if action fails
            await distributedCache.RemoveAsync(resource);
            throw;
        }
    }

    private async Task<bool> Acquired(string resource, TimeSpan expirationTime)
    {
        var isAcquired = false;
        if (distributedCache is RedisCache redisCache)
        {
            var redis = EngineContext.Current.Resolve<RedisConnectionWrapper>();
            isAcquired = await (await redis.GetDatabaseAsync()).StringSetAsync(
                resource, resource, expirationTime, When.NotExists);
        }
        else
        {
            await distributedCache.SetStringAsync(
                resource,
                resource,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expirationTime
                }
            );
            isAcquired = true;
        }

        return isAcquired;
    }

    /// <summary>
    /// Starts a background task with "heartbeat": a status flag that will be periodically updated to signal to
    /// others that the task is running and stop them from starting the same task.
    /// </summary>
    /// <param name="key">The key of the background task</param>
    /// <param name="expirationTime">The time after which the heartbeat key will automatically be expired. Should be longer than <paramref name="heartbeatInterval"/></param>
    /// <param name="heartbeatInterval">The interval at which to update the heartbeat, if required by the implementation</param>
    /// <param name="action">Asynchronous background task to be performed</param>
    /// <param name="cancellationTokenSource">A CancellationTokenSource for manually canceling the task</param>
    /// <returns>A task that resolves true if lock was acquired and action was performed; otherwise false</returns>
    public async Task RunWithHeartbeatAsync(
        string key,
        TimeSpan expirationTime,
        TimeSpan heartbeatInterval,
        Func<CancellationToken, Task> action,
        CancellationTokenSource cancellationTokenSource = default
    )
    {
        if (!string.IsNullOrEmpty(await distributedCache.GetStringAsync(key)))
            return;

        var tokenSource = cancellationTokenSource ?? new CancellationTokenSource();

        try
        {
            // run heartbeat early to minimize risk of multiple execution
            await distributedCache.SetStringAsync(
                key,
                Running,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expirationTime
                },
                token: tokenSource.Token
            );

            await using var timer = new Timer(
                callback: _ =>
                {
                    try
                    {
                        tokenSource.Token.ThrowIfCancellationRequested();
                        var status = distributedCache.GetString(key);
                        if (
                            !string.IsNullOrEmpty(status)
                            && JsonConvert.DeserializeObject<TaskStatus>(status)
                            == TaskStatus.Canceled
                        )
                        {
                            tokenSource.Cancel();
                            return;
                        }

                        distributedCache.SetString(
                            key,
                            Running,
                            new DistributedCacheEntryOptions
                            {
                                AbsoluteExpirationRelativeToNow = expirationTime
                            }
                        );
                    }
                    catch (OperationCanceledException)
                    {
                    }
                },
                state: null,
                dueTime: 0,
                period: (int) heartbeatInterval.TotalMilliseconds
            );

            await action(tokenSource.Token);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            await distributedCache.RemoveAsync(key);
        }
    }

    /// <summary>
    /// Tries to cancel a background task by flagging it for cancellation on the next heartbeat.
    /// </summary>
    /// <param name="key">The task's key</param>
    /// <param name="expirationTime">The time after which the task will be considered stopped due to system shutdown or other causes,
    /// even if not explicitly canceled.</param>
    /// <returns>A task that represents requesting cancellation of the task. Note that the completion of this task does not
    /// necessarily imply that the task has been canceled, only that cancellation has been requested.</returns>
    public async Task CancelTaskAsync(string key, TimeSpan expirationTime)
    {
        var status = await distributedCache.GetStringAsync(key);
        if (
            !string.IsNullOrEmpty(status)
            && JsonConvert.DeserializeObject<TaskStatus>(status) != TaskStatus.Canceled
        )
            await distributedCache.SetStringAsync(
                key,
                JsonConvert.SerializeObject(TaskStatus.Canceled),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = expirationTime
                }
            );
    }

    /// <summary>
    /// Check if a background task is running.
    /// </summary>
    /// <param name="key">The task's key</param>
    /// <returns>A task that resolves to true if the background task is running; otherwise false</returns>
    public async Task<bool> IsTaskRunningAsync(string key)
    {
        return !string.IsNullOrEmpty(await distributedCache.GetStringAsync(key));
    }

    #endregion
}
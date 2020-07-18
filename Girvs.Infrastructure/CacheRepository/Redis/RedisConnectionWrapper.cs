using System;
using System.Linq;
using System.Net;
using Girvs.Domain.Caching.Interface;
using Girvs.Domain.Caching.Interface.Redis;
using Girvs.Domain.Configuration;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;

namespace Girvs.Infrastructure.CacheRepository.Redis
{
    /// <summary>
    /// 表示Redis连接包装器实现
    /// </summary>
    public class RedisConnectionWrapper : IRedisConnectionWrapper, ILocker
    {
        private readonly GirvsConfig _config;
        private readonly object _lock = new object();
        private volatile ConnectionMultiplexer _connection;
        private readonly Lazy<string> _connectionString;
        private volatile RedLockFactory _redisLockFactory;

        public RedisConnectionWrapper(GirvsConfig config)
        {
            this._config = config;
            _connectionString = new Lazy<string>(GetConnectionString);
            _redisLockFactory = CreateRedisLockFactory();
        }

        protected string GetConnectionString()
        {
            return _config.RedisConnectionString;
        }

        protected ConnectionMultiplexer GetConnection()
        {
            if (_connection != null && _connection.IsConnected) return _connection;

            lock (_lock)
            {
                if (_connection != null && _connection.IsConnected) return _connection;

                //连接断开。处理连接......
                _connection?.Dispose();

                //创建Redis Connection的新实例
                _connection = ConnectionMultiplexer.Connect(_connectionString.Value);
            }

            return _connection;
        }

        protected RedLockFactory CreateRedisLockFactory()
        {
            //get RedLock endpoints
            var configurationOptions = ConfigurationOptions.Parse(_connectionString.Value);
            var redLockEndPoints = GetEndPoints().Select(endPoint => new RedLockEndPoint
            {
                EndPoint = endPoint,
                Password = configurationOptions.Password,
                Ssl = configurationOptions.Ssl,
                RedisDatabase = configurationOptions.DefaultDatabase,
                ConfigCheckSeconds = configurationOptions.ConfigCheckSeconds,
                ConnectionTimeout = configurationOptions.ConnectTimeout,
                SyncTimeout = configurationOptions.SyncTimeout
            }).ToList();

            //create RedLock factory to use RedLock distributed lock algorithm
            return RedLockFactory.Create(redLockEndPoints);
        }

        public IDatabase GetDatabase(int db)
        {
            return GetConnection().GetDatabase(db);
        }

        public IServer GetServer(EndPoint endPoint)
        {
            return GetConnection().GetServer(endPoint);
        }

        public EndPoint[] GetEndPoints()
        {
            return GetConnection().GetEndPoints();
        }

        public void FlushDatabase(RedisDatabaseNumber db)
        {
            var endPoints = GetEndPoints();

            foreach (var endPoint in endPoints)
            {
                GetServer(endPoint).FlushDatabase((int)db);
            }
        }

        public bool PerformActionWithLock(string resource, TimeSpan expirationTime, Action action)
        {
            //使用RedLock库
            using (var redisLock = _redisLockFactory.CreateLock(resource, expirationTime))
            {
                //确保获得锁
                if (!redisLock.IsAcquired)
                    return false;
                //执行动作
                action();
                return true;
            }
        }

        public void Dispose()
        {
            //释放连接
            _connection?.Dispose();

            //释放锁工厂
            _redisLockFactory?.Dispose();
        }
    }
}
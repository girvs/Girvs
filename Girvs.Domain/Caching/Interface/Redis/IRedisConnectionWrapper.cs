using System;
using System.Net;
using StackExchange.Redis;

namespace Girvs.Domain.Caching.Interface.Redis
{
    /// <summary>
    /// 表示Redis连接器
    /// </summary>
    public interface IRedisConnectionWrapper : IDisposable
    {
        /// <summary>
        /// 获取与Redis内部数据库的交互式连接
        /// </summary>
        /// <param name="db">数据库编号</param>
        /// <returns>Redis缓存数据库</returns>
        IDatabase GetDatabase(int db);

        /// <summary>
        /// 获取单个服务器的配置API
        /// </summary>
        /// <param name="endPoint">网络端点</param>
        /// <returns>Redis服务器</returns>
        IServer GetServer(EndPoint endPoint);

        /// <summary>
        /// 获取服务器上定义的所有端点
        /// </summary>
        /// <returns>端点数组</returns>
        EndPoint[] GetEndPoints();

        ///// <summary>
        ///// 刷新数据库的所有键
        ///// </summary>
        ///// <param name="db">数据库编号</param>
        //void FlushDatabase(RedisDatabaseNumber db);
    }
}
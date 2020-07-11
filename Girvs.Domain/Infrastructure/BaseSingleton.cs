using System;
using System.Collections.Generic;

namespace Girvs.Domain.Infrastructure
{
    /// <summary>
    /// 基本的单例存储器
    /// </summary>
    public class BaseSingleton
    {
        static BaseSingleton()
        {
            AllSingletons = new Dictionary<Type, object>();
        }

        /// <summary>
        /// 所有字典单例实例
        /// </summary>
        public static IDictionary<Type, object> AllSingletons { get; }
    }
}
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Girvs.Domain.Caching
{
    public interface ICacheUsingManager
    {
        Task SetAsync(Action<ActionCacheUsingParameter> action, object data);
        Task<T> GetAsync<T>(Action<ActionCacheUsingParameter> action, Func<Task<T>> acquire);
        Task ReMoveAsync(string keyPrefix);
        Task ReMoveByPrefixAsync(string keyPrefix);
        Task<IList<string>> GetAllKeysAsync();

        Task<string> GetToString(string key);
    }

    public class ActionCacheUsingParameter
    {
        public bool UseCache { get; set; } = false;
        public int CacheTime { get; set; } = 30;
        public string CacheKey { get; set; }
        public string AllListKeyPrefix { get; set; }
        public string QueryListKeyPrefix { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Girvs.Domain.Caching.Interface;
using Girvs.Infrastructure.CacheRepository.ComponentModel;
using Microsoft.AspNetCore.Http;

namespace Girvs.Infrastructure.CacheRepository
{
    /// <summary>
    /// 表示HTTP请求期间缓存的管理器（短期缓存）
    /// </summary>
    public partial class PerRequestCacheManager : ICacheManager
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ReaderWriterLockSlim locker;

        public PerRequestCacheManager(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            locker = new ReaderWriterLockSlim();
        }

        protected virtual IDictionary<object, object> GetItems()
        {
            return httpContextAccessor.HttpContext?.Items;
        }

        public virtual T Get<T>(string key, Func<T> acquire, int? cacheTime = null)
        {
            IDictionary<object, object> items;

            using (new ReaderWriteLockDisposable(locker, ReaderWriteLockType.Read))
            {
                items = GetItems();
                if (items == null)
                    return acquire();

                if (items[key] != null)
                    return (T)items[key];
            }

            var result = acquire();

            if (result == null || (cacheTime ?? GirvsCachingDefaults.CacheTime) <= 0)
                return result;

            using (new ReaderWriteLockDisposable(locker))
            {
                items[key] = result;
            }

            return result;
        }

        public virtual string GetToString(string key)
        {
            using (new ReaderWriteLockDisposable(locker, ReaderWriteLockType.Read))
            {
                IDictionary<object, object> items = GetItems();
                if (items[key] != null)
                    return items[key].ToString();
            }
            return string.Empty;
        }

        public virtual void Set(string key, object data, int? cacheTime = null)
        {
            if (data == null)
                return;

            using (new ReaderWriteLockDisposable(locker))
            {
                var items = GetItems();
                if (items == null)
                    return;

                items[key] = data;
            }
        }

        public virtual bool IsSet(string key)
        {
            using (new ReaderWriteLockDisposable(locker, ReaderWriteLockType.Read))
            {
                var items = GetItems();
                return items?[key] != null;
            }
        }

        public virtual void Remove(string key)
        {
            using (new ReaderWriteLockDisposable(locker))
            {
                var items = GetItems();
                items?.Remove(key);
            }
        }

        public virtual void RemoveByPrefix(string prefix)
        {
            using (new ReaderWriteLockDisposable(locker, ReaderWriteLockType.UpgradeableRead))
            {
                var items = GetItems();
                if (items == null)
                    return;
                var regex = new Regex(prefix,
                    RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.IgnoreCase);
                var matchesKeys = items.Keys.Select(p => p.ToString()).Where(key => regex.IsMatch(key)).ToList();

                if (!matchesKeys.Any())
                    return;

                using (new ReaderWriteLockDisposable(locker))
                {
                    //remove matching values
                    foreach (var key in matchesKeys)
                    {
                        items.Remove(key);
                    }
                }
            }
        }

        public virtual void Clear()
        {
            using (new ReaderWriteLockDisposable(locker))
            {
                var items = GetItems();
                items?.Clear();
            }
        }

        public List<string> GetCacheKeys()
        {
            List<string> requestKeys = new List<string>();
            var items = GetItems();
            foreach (var key in items.Keys)
            {
                requestKeys.Add(key.ToString());
            }

            return requestKeys;
        }

        public virtual void Dispose()
        {
            //nothing special
        }
    }
}
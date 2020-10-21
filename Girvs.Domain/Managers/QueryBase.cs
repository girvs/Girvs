using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json.Serialization;
using Girvs.Domain.Caching.Interface;
using Girvs.Domain.Models;

namespace Girvs.Domain.Managers
{
    public abstract class QueryBase<T> : IQuery<T> where T : BaseEntity, new()
    {
        protected QueryBase()
        {
            OrderBy = x => nameof(BaseEntity.CreateTime);
            PageSize = 20;
            PageIndex = 0;
        }

        public int PageStart => PageIndex * PageSize;

        /// <summary>
        /// 当前页，从0开始
        /// </summary>
        [QueryCacheKey]
        public int PageIndex { get; set; }

        [QueryCacheKey] public int PageSize { get; set; }
        public int RecordCount { get; set; }
        public List<T> Result { get; set; }

        [JsonIgnore] [QueryCacheKey] public Expression<Func<T, string>> OrderBy { get; set; }

        public int PageCount => (int) Math.Ceiling(RecordCount / (decimal) PageSize);

        public string[] QueryFields { get; set; }

        public virtual string GetCacheKey(ICacheKeyManager<T> cacheKeyManager)
        {
            return GetCacheKey(cacheKeyManager.CacheKeyListQueryPrefix);
        }

        public virtual string GetCacheKey(string cacheKeyListPrefix)
        {
            //此处字符串为约定，请不要随意修改
            StringBuilder sb = new StringBuilder(cacheKeyListPrefix);
            var ps = this.GetType().GetProperties();
            foreach (var propertyInfo in ps)
            {
                if (propertyInfo.GetCustomAttributes(typeof(QueryCacheKeyAttribute), true).Length > 0)
                {
                    sb.Append($":{propertyInfo.Name}={propertyInfo.GetValue(this)}");
                }
            }

            if (QueryFields != null && QueryFields.Length > 0)
            {
                var queryFieldStr = string.Join(',', QueryFields);
                sb.Append($"QueryFields:{queryFieldStr}");
            }

            return sb.ToString();
        }

        public abstract Expression<Func<T, bool>> GetQueryWhere();
    }
}
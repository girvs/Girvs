using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json.Serialization;

namespace Girvs.BusinessBasis.Queries
{
    public abstract class QueryBase<TEntity> : IQuery<TEntity>
    {
        protected QueryBase()
        {
            OrderBy = x => "Id";
            PageSize = 20;
            PageIndex = 1;
        }

        public int PageStart => (PageIndex - 1) * PageSize;

        /// <summary>
        /// 当前页，从1开始
        /// </summary>
        [QueryCacheKey]
        public int PageIndex { get; set; }

        [QueryCacheKey] public int PageSize { get; set; }
        public int RecordCount { get; set; }
        public List<TEntity> Result { get; set; }

        [JsonIgnore] [QueryCacheKey] public Expression<Func<TEntity, string>> OrderBy { get; set; }

        public int PageCount => (int) Math.Ceiling(RecordCount / (decimal) PageSize);

        public string[] QueryFields { get; set; }

        public virtual string GetCacheKey()
        {
            //此处字符串为约定，请不要随意修改
            var sb = new StringBuilder();

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

            return HashHelper.CreateHash(Encoding.UTF8.GetBytes(sb.ToString()));
        }

        public abstract Expression<Func<TEntity, bool>> GetQueryWhere();
    }
}
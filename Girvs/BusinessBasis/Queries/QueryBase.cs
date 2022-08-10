using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Girvs.BusinessBasis.Queries
{
    public abstract class QueryBase<TEntity> : IQuery<TEntity>
    {
        protected QueryBase()
        {
            OrderBy = string.Empty;
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

        [QueryCacheKey] public string OrderBy { get; set; }

        public int PageCount => (int) Math.Ceiling(RecordCount / (decimal) PageSize);

        public string[] QueryFields { get; set; }

        public virtual string GetCacheKey()
        {
            //此处字符串为约定，请不要随意修改
            var sb = new StringBuilder(GetType().FullName);

            var ps = GetType().GetProperties();
            foreach (var propertyInfo in ps)
            {
                if (propertyInfo.GetCustomAttributes(typeof(QueryCacheKeyAttribute), true).Length > 0)
                {
                    sb.Append($":{propertyInfo.Name}={propertyInfo.GetValue(this)}");
                }
            }

            if (QueryFields != null && QueryFields.Any())
            {
                var queryFieldStr = string.Join(',', QueryFields);
                sb.Append($"QueryFields:{queryFieldStr}");
            }

            if (!string.IsNullOrEmpty(OrderBy))
            {
                sb.Append($"OrderBy:{OrderBy}");
            }

            return sb.ToString().ToMd5();
        }

        public abstract Expression<Func<TEntity, bool>> GetQueryWhere();
    }
}
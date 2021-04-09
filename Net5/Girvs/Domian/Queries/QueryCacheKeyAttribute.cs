using System;

namespace Girvs.Domian.Queries
{
    /// <summary>
    /// 主要作为查询Key的关键字
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class QueryCacheKeyAttribute : Attribute
    {

    }
}
using Girvs.Domain.Extensions;
using Girvs.Domain.Managers;
using Power.BasicManagement.Domain.Models;
using System;
using System.Linq.Expressions;

namespace Power.BasicManagement.Domain.Queries
{
    /// <summary>
    /// 
    /// </summary>
    public class SysDictQuery : QueryBase<SysDict>
    {
        /// <summary>
        /// 
        /// </summary>
        [QueryCacheKey]
        public string CodeType { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override Expression<Func<SysDict, bool>> GetQueryWhere()
        {
            Expression<Func<SysDict, bool>> expression = dict => true;

            if (!string.IsNullOrEmpty(CodeType))
            {
                expression = expression.And(dict => dict.CodeType == CodeType);
            }

            return expression;
        }
    }
}
using System;
using System.Linq.Expressions;
using BasicManagement.Domain.Models;
using Girvs.BusinessBasis.Queries;
using Girvs.Extensions;

namespace BasicManagement.Domain.Queries
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
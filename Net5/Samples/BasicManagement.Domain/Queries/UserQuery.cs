using System;
using System.Linq.Expressions;
using BasicManagement.Domain.Enumerations;
using BasicManagement.Domain.Models;
using Girvs.BusinessBasis.Queries;
using Girvs.Extensions;

namespace BasicManagement.Domain.Queries
{
    public class UserQuery : QueryBase<User>
    {
        [QueryCacheKey]
        public string UserName { get; set; }

        [QueryCacheKey]
        public string UserAccount { get; set; }

        [QueryCacheKey]
        public DataState? DataState { get; set; }


        public override Expression<Func<User, bool>> GetQueryWhere()
        {
            Expression<Func<User, bool>> expression = user => true;
            if (DataState.HasValue)
            {
                expression = expression.And(user => user.State == DataState.Value);
            }

            if (!string.IsNullOrEmpty(UserName))
            {
                expression = expression.And(user => user.UserName.Contains(UserName));
            }

            if (!string.IsNullOrEmpty(UserAccount))
            {
                expression = expression.And(user => user.UserAccount.Contains(UserAccount));
            }

            return expression;
        }
    }
}
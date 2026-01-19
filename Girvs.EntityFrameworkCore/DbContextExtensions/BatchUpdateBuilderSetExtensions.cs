using Humanizer;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore.Query;

namespace Girvs.EntityFrameworkCore.DbContextExtensions;

public static class BatchUpdateBuilderSetExtensions
{
    // public static BatchUpdateBuilder<TEntity> Set<TEntity>(this BatchUpdateBuilder<TEntity> batchUpdateBuilder,
    //     Expression<Func<TEntity>> name,
    //     Expression<Func<TEntity>> value) where TEntity : class
    // {
    //     
    //     return batchUpdateBuilder;
    // }
#if NET10_0_OR_GREATER

#else
        public static Expression<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>> Append<TEntity>(
        this Expression<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>> left,
        Expression<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>> right)
    {
        var replace = new ReplacingExpressionVisitor(right.Parameters, new[] {left.Body});
        var combined = replace.Visit(right.Body);
        return Expression.Lambda<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>>(combined, left.Parameters);
    }
#endif
}
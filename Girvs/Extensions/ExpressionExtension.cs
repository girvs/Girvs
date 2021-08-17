using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Girvs.Extensions
{
    public static class ExpressionExtension
    {
        // public static Expression<Func<T, bool>> And<T>(
        //    this Expression<Func<T, bool>> first,
        //    Expression<Func<T, bool>> second)
        // {
        //     if (first == null) throw new ArgumentNullException(nameof(first));
        //     if (second == null) throw new ArgumentNullException(nameof(second));
        //     return first.AndAlso<T>(second, Expression.AndAlso);
        // }
        //
        // public static Expression<Func<T, bool>> Or<T>(
        //     this Expression<Func<T, bool>> first,
        //     Expression<Func<T, bool>> second)
        // {
        //     if (first == null) throw new ArgumentNullException(nameof(first));
        //     if (second == null) throw new ArgumentNullException(nameof(second));
        //     return first.AndAlso<T>(second, Expression.OrElse);
        // }
        //
        // private static Expression<Func<T, bool>> AndAlso<T>(
        // this Expression<Func<T, bool>> expr1,
        // Expression<Func<T, bool>> expr2,
        // Func<Expression, Expression, BinaryExpression> func)
        // {
        //     var parameter = Expression.Parameter(typeof(T));
        //
        //     var leftVisitor = new ReplaceExpressionVisitor(expr1.Parameters[0], parameter);
        //     var left = leftVisitor.Visit(expr1.Body);
        //
        //     var rightVisitor = new ReplaceExpressionVisitor(expr2.Parameters[0], parameter);
        //     var right = rightVisitor.Visit(expr2.Body);
        //
        //     var result = Expression.Lambda<Func<T, bool>>(func(left, right), parameter);
        //     return result;
        // }
        //
        // private class ReplaceExpressionVisitor
        //     : ExpressionVisitor
        // {
        //     private readonly Expression _oldValue;
        //     private readonly Expression _newValue;
        //
        //     public ReplaceExpressionVisitor(Expression oldValue, Expression newValue)
        //     {
        //         _oldValue = oldValue;
        //         _newValue = newValue;
        //     }
        //
        //     public override Expression Visit(Expression node)
        //     {
        //         if (node == _oldValue)
        //             return _newValue;
        //         return base.Visit(node);
        //     }
        // }

        /// <summary>
        ///     以特定的条件运行组合两个Expression表达式
        /// </summary>
        /// <typeparam name="T">表达式的主实体类型</typeparam>
        /// <param name="first">第一个Expression表达式</param>
        /// <param name="second">要组合的Expression表达式</param>
        /// <param name="merge">组合条件运算方式</param>
        /// <returns>组合后的表达式</returns>
        public static Expression<T> Compose<T>(this Expression<T> first, Expression<T> second,
            Func<Expression, Expression, Expression> merge)
        {
            var map = first.Parameters.Select((f, i) => new { f, s = second.Parameters[i] })
                .ToDictionary(p => p.s, p => p.f);
            var secondBody = ParameterRebinder.ReplaceParameters(map, second.Body);
            return Expression.Lambda<T>(merge(first.Body, secondBody), first.Parameters);
        }

        /// <summary>
        ///     以 Expression.AndAlso 组合两个Expression表达式
        /// </summary>
        /// <typeparam name="T">表达式的主实体类型</typeparam>
        /// <param name="first">第一个Expression表达式</param>
        /// <param name="second">要组合的Expression表达式</param>
        /// <returns>组合后的表达式</returns>
        public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> first,
            Expression<Func<T, bool>> second)
        {
            return first.Compose(second, Expression.AndAlso);
        }

        /// <summary>
        ///     以 Expression.OrElse 组合两个Expression表达式
        /// </summary>
        /// <typeparam name="T">表达式的主实体类型</typeparam>
        /// <param name="first">第一个Expression表达式</param>
        /// <param name="second">要组合的Expression表达式</param>
        /// <returns>组合后的表达式</returns>
        public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> first,
            Expression<Func<T, bool>> second)
        {
            return first.Compose(second, Expression.OrElse);
        }

        private class ParameterRebinder : ExpressionVisitor
        {
            private readonly Dictionary<ParameterExpression, ParameterExpression> _map;

            private ParameterRebinder(Dictionary<ParameterExpression, ParameterExpression> map)
            {
                _map = map ?? new Dictionary<ParameterExpression, ParameterExpression>();
            }

            public static Expression ReplaceParameters(Dictionary<ParameterExpression, ParameterExpression> map,
                Expression exp)
            {
                return new ParameterRebinder(map).Visit(exp);
            }

            protected override Expression VisitParameter(ParameterExpression node)
            {
                ParameterExpression replacement;
                if (_map.TryGetValue(node, out replacement))
                    node = replacement;
                return base.VisitParameter(node);
            }
        }
    }
}
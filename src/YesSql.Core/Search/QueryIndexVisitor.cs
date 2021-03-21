using Parlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using YesSql.Indexes;
using YesSql.Services;

namespace YesSql.Search
{
    public interface IQueryIndexVisitor<TDocument, TIndex> where TDocument : class where TIndex : class, IIndex
    {
        IQuery<TDocument, TIndex> Query(QueryIndexContext<TDocument, TIndex> context);
    }


    /// <summary>
    /// Takes a list of statements and applies them to a query.
    /// <summary>
    public class QueryIndexVisitor<TDocument, TIndex> :
        IStatementVisitor<QueryIndexContext<TDocument, TIndex>, QueryIndexContext<TDocument, TIndex>>,
        IFilterExpressionVisitor<QueryIndexExpressionArgument<TDocument, TIndex>, Expression>,
        ISortExpressionVisitor<QueryIndexExpressionArgument<TDocument, TIndex>, QueryIndexContext<TDocument, TIndex>>,
        IOperatorVisitor<QueryOperationArgument<TDocument, TIndex>, Expression>,
        IValueVisitor<ConstantExpression>,
        IQueryIndexVisitor<TDocument, TIndex>
        where TDocument : class where TIndex : class, IIndex
    {
        private static readonly MethodInfo _containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) })!;
        private static readonly MethodInfo _notContainsMethod = typeof(DefaultQueryExtensions).GetMethod("NotContains", new[] { typeof(string), typeof(string) })!;

        private static readonly Func<IQuery<TDocument, TIndex>, Expression<Func<TIndex, bool>>, IQuery<TDocument, TIndex>> _defaultWhere = (query, e) => query.Where(e);
        private static readonly Func<IQuery<TDocument, TIndex>, Expression<Func<TIndex, object>>, IQuery<TDocument, TIndex>> _defaultOrderBy = (query, e) => query.OrderBy(e);
        private static readonly Func<IQuery<TDocument, TIndex>, Expression<Func<TIndex, object>>, IQuery<TDocument, TIndex>> _defaultOrderByDescending = (query, e) => query.OrderByDescending(e);
        private static readonly Func<IQuery<TDocument, TIndex>, Expression<Func<TIndex, object>>, IQuery<TDocument, TIndex>> _defaultThenBy = (query, e) => query.ThenBy(e);
        private static readonly Func<IQuery<TDocument, TIndex>, Expression<Func<TIndex, object>>, IQuery<TDocument, TIndex>> _defaultThenByDescending = (query, e) => query.ThenByDescending(e);


        private readonly ParameterExpression _parameter;

        public QueryIndexVisitor()
        {
            _parameter = Expression.Parameter(typeof(TIndex));
        }

        public IQuery<TDocument, TIndex> Query(QueryIndexContext<TDocument, TIndex> context)
        {
            foreach (var statement in context.StatementList.Statements)
            {
                context = statement.Accept(this, context);
            }

            return context.Query;

        }

        public QueryIndexContext<TDocument, TIndex> VisitDefaultFilterStatement(DefaultFilterStatement statement, QueryIndexContext<TDocument, TIndex> context)
        {
            if (context.DefaultFilter == null)
            {
                return context;
            }

            var propertyInfo = context.DefaultFilter.PropertyInfo;

            var propertyExpression = Expression.Property(_parameter, propertyInfo);
            var expressionArgument = new QueryIndexExpressionArgument<TDocument, TIndex>
            {
                MemberExpression = propertyExpression,
                Context = context
            };

            var body = statement.Expression.Accept(this, expressionArgument);
            var expression = Expression.Lambda<Func<TIndex, bool>>(body, _parameter);

            if (context.DefaultFilter.Filter != null)
            {
                context.Query = context.DefaultFilter.Filter.Invoke(context.Query, expression);
            }
            else
            {
                context.Query = _defaultWhere.Invoke(context.Query, expression);
            }

            return context;
        }

        public QueryIndexContext<TDocument, TIndex> VisitFieldFilterStatement(FieldFilterStatement statement, QueryIndexContext<TDocument, TIndex> context)
        {
            if (!context.Filters.TryGetValue(statement.Name, out var filterMap))
            {
                return context;
            }

            // TODO could be cached.
            var propertyExpression = Expression.Property(_parameter, filterMap.PropertyInfo);
            var expressionArgument = new QueryIndexExpressionArgument<TDocument, TIndex>
            {
                MemberExpression = propertyExpression,
                Context = context
            };

            var body = statement.Expression.Accept(this, expressionArgument);
            var expression = Expression.Lambda<Func<TIndex, bool>>(body, _parameter);

            if (filterMap.Filter != null)
            {
                context.Query = filterMap.Filter.Invoke(context.Query, expression);
            }
            else
            {
                context.Query = _defaultWhere.Invoke(context.Query, expression);
            }

            return context;
        }

        public QueryIndexContext<TDocument, TIndex> VisitSortStatement(SortStatement statement, QueryIndexContext<TDocument, TIndex> context)
        {
            if (!context.Sorts.TryGetValue(statement.PropertyName.Value, out var sortMap))
            {
                return context;
            }

            var propertyExpression = Expression.Property(_parameter, sortMap.PropertyInfo);

            var expressionArgument = new QueryIndexExpressionArgument<TDocument, TIndex>
            {
                MemberExpression = propertyExpression,
                Context = context
            };

            context = statement.Sort.Accept(this, expressionArgument);

            return context;
        }

        public QueryIndexContext<TDocument, TIndex> VisitDefaultSortStatement(DefaultSortStatement statement, QueryIndexContext<TDocument, TIndex> context)
        {
            if (context.StatementList.HasOrder)
            {
                return context;
            }

            return VisitSortStatement(statement, context);
        }

        Expression IFilterExpressionVisitor<QueryIndexExpressionArgument<TDocument, TIndex>, Expression>.VisitUnaryFilterExpression(UnaryFilterExpression expression, QueryIndexExpressionArgument<TDocument, TIndex> argument)
        {
            var operationArgument = new QueryOperationArgument<TDocument, TIndex>
            {
                FilterExpression = expression,
                QueryIndexArgument = argument

            };

            return expression.Operation.Accept(this, operationArgument);
        }

        Expression IFilterExpressionVisitor<QueryIndexExpressionArgument<TDocument, TIndex>, Expression>.VisitAndFilterExpression(AndFilterExpression expression, QueryIndexExpressionArgument<TDocument, TIndex> argument)
            => Expression.AndAlso(
                expression.Left.Accept(this, argument),
                expression.Right.Accept(this, argument)
            );

        Expression IFilterExpressionVisitor<QueryIndexExpressionArgument<TDocument, TIndex>, Expression>.VisitOrFilterExpression(OrFilterExpression expression, QueryIndexExpressionArgument<TDocument, TIndex> argument)
            => Expression.Or(
                expression.Left.Accept(this, argument),
                expression.Right.Accept(this, argument)
            );

        public Expression VisitMatchOperator(MatchOperator op, QueryOperationArgument<TDocument, TIndex> argument)
        {
            if (argument.QueryIndexArgument.MemberExpression.Type == typeof(string))
            {
                return Expression.Call(
                    argument.QueryIndexArgument.MemberExpression,
                    _containsMethod,
                    argument.FilterExpression.Value.Accept(this)
                );
            }
            else
            {
                return Expression.Equal(argument.QueryIndexArgument.MemberExpression, Expression.Constant(true));
            }
        }

        public Expression VisitNotMatchOperator(NotMatchOperator op, QueryOperationArgument<TDocument, TIndex> argument)
        {
            // TODO dates etc. But one would expect them to be > < operators.
            if (argument.QueryIndexArgument.MemberExpression.Type == typeof(string))
            {
                return Expression.Call(
                    argument.QueryIndexArgument.MemberExpression,
                    _notContainsMethod,
                    argument.FilterExpression.Value.Accept(this)
                );
            }
            else
            {
                return Expression.Equal(argument.QueryIndexArgument.MemberExpression, Expression.Constant(false));
            }
        }

        ConstantExpression IValueVisitor<ConstantExpression>.VisitValue(SearchValue value)
            => Expression.Constant(value.Value);

        public QueryIndexContext<TDocument, TIndex> VisitSortAscending(SortAscending expression, QueryIndexExpressionArgument<TDocument, TIndex> argument)
        {
            var orderByExpression = Expression.Lambda<Func<TIndex, object>>(
                Expression.Convert(argument.MemberExpression, typeof(object)),
                _parameter
            );

            if (argument.Context.StatementList.HasOrder)
            {
                argument.Context.Query = _defaultThenBy.Invoke(argument.Context.Query, orderByExpression);

                return argument.Context;
            }

            argument.Context.Query = _defaultOrderBy.Invoke(argument.Context.Query, orderByExpression);
            argument.Context.StatementList.HasOrder = true;

            return argument.Context;
        }

        public QueryIndexContext<TDocument, TIndex> VisitSortDescending(SortDescending expression, QueryIndexExpressionArgument<TDocument, TIndex> argument)
        {
            var orderByExpression = Expression.Lambda<Func<TIndex, object>>(
                Expression.Convert(argument.MemberExpression, typeof(object)),
                _parameter
            );

            if (argument.Context.StatementList.HasOrder)
            {
                argument.Context.Query = _defaultThenByDescending.Invoke(argument.Context.Query, orderByExpression);

                return argument.Context;
            }

            argument.Context.Query = _defaultOrderByDescending.Invoke(argument.Context.Query, orderByExpression);
            argument.Context.StatementList.HasOrder = true;

            return argument.Context;
        }
    }

    public struct QueryIndexExpressionArgument<TDocument, TIndex> where TDocument : class where TIndex : class, IIndex
    {
        public MemberExpression MemberExpression { get; set; }
        public QueryIndexContext<TDocument, TIndex> Context { get; set; }
    }

    public struct QueryOperationArgument<TDocument, TIndex> where TDocument : class where TIndex : class, IIndex
    {
        public UnaryFilterExpression FilterExpression { get; set; }
        public QueryIndexExpressionArgument<TDocument, TIndex> QueryIndexArgument { get; set; }
    }
}

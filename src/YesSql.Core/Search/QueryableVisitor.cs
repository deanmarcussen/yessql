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

    /// <summary>
    /// Takes a list of statements and applies them to a query.
    /// <summary>
    public class QueryableVisitor<TSource> :
        IStatementVisitor<QueryableContext<TSource>, QueryableContext<TSource>>,
        IFilterExpressionVisitor<QueryableExpressionArgument<TSource>, Expression>,
        ISortExpressionVisitor<QueryableExpressionArgument<TSource>, QueryableContext<TSource>>,
        IOperatorVisitor<QueryableOperationArgument<TSource>, Expression>,
        IValueVisitor<ConstantExpression>
    {
        private static readonly MethodInfo _containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) })!;
        private static readonly MethodInfo _notContainsMethod = typeof(DefaultQueryExtensions).GetMethod("NotContains", new[] { typeof(string), typeof(string) })!;

        private static readonly Func<IQueryable<TSource>, Expression<Func<TSource, bool>>, IQueryable<TSource>> _defaultWhere = (query, e) => query.Where(e);
        // TODO these are probably going to either have to be casted to IOrderedQueryable. That would work.
        private static readonly Func<IQueryable<TSource>, Expression<Func<TSource, object>>, IQueryable<TSource>> _defaultOrderBy = (query, e) => query.OrderBy(e);
        private static readonly Func<IQueryable<TSource>, Expression<Func<TSource, object>>, IQueryable<TSource>> _defaultOrderByDescending = (query, e) => query.OrderByDescending(e);
        private static readonly Func<IOrderedQueryable<TSource>, Expression<Func<TSource, object>>, IQueryable<TSource>> _defaultThenBy = (query, e) => query.ThenBy(e);
        private static readonly Func<IOrderedQueryable<TSource>, Expression<Func<TSource, object>>, IQueryable<TSource>> _defaultThenByDescending = (query, e) => query.ThenByDescending(e);

        private readonly ParameterExpression _parameter;

        public QueryableVisitor()
        {
            _parameter = Expression.Parameter(typeof(TSource));
        }

        public IQueryable<TSource> Visit(QueryableContext<TSource> context)
        {
            foreach (var statement in context.StatementList.Statements)
            {
                context = statement.Accept(this, context);
            }

            return context.Query;

        }

        public QueryableContext<TSource> VisitDefaultFilterStatement(DefaultFilterStatement statement, QueryableContext<TSource> context)
        {
            if (context.DefaultFilter == null)
            {
                return context;
            }

            var propertyInfo = context.DefaultFilter.PropertyInfo;

            var propertyExpression = Expression.Property(_parameter, propertyInfo);
            var expressionArgument = new QueryableExpressionArgument<TSource>
            {
                MemberExpression = propertyExpression,
                Context = context
            };

            var body = statement.Expression.Accept(this, expressionArgument);
            var expression = Expression.Lambda<Func<TSource, bool>>(body, _parameter);

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

        public QueryableContext<TSource> VisitFieldFilterStatement(FieldFilterStatement statement, QueryableContext<TSource> context)
        {
            if (!context.Filters.TryGetValue(statement.Name, out var filterMap))
            {
                return context;
            }

            // TODO could be cached.
            var propertyExpression = Expression.Property(_parameter, filterMap.PropertyInfo);
            var expressionArgument = new QueryableExpressionArgument<TSource>
            {
                MemberExpression = propertyExpression,
                Context = context
            };

            var body = statement.Expression.Accept(this, expressionArgument);
            var expression = Expression.Lambda<Func<TSource, bool>>(body, _parameter);

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

        public QueryableContext<TSource> VisitSortStatement(SortStatement statement, QueryableContext<TSource> context)
        {
            if (!context.Sorts.TryGetValue(statement.FieldName.Value, out var sortMap))
            {
                return context;
            }

            var propertyExpression = Expression.Property(_parameter, sortMap.PropertyInfo);

            var expressionArgument = new QueryableExpressionArgument<TSource>
            {
                MemberExpression = propertyExpression,
                Context = context
            };

            context = statement.Sort.Accept(this, expressionArgument);

            return context;
        }

        public QueryableContext<TSource> VisitDefaultSortStatement(DefaultSortStatement statement, QueryableContext<TSource> context)
        {
            if (context.StatementList.HasOrder)
            {
                return context;
            }

            return VisitSortStatement(statement, context);
        }

        Expression IFilterExpressionVisitor<QueryableExpressionArgument<TSource>, Expression>.VisitUnaryFilterExpression(UnaryFilterExpression expression, QueryableExpressionArgument<TSource> argument)
        {
            var operationArgument = new QueryableOperationArgument<TSource>
            {
                FilterExpression = expression,
                QueryableArgument = argument
            };

            return expression.Operation.Accept(this, operationArgument);
        }

        Expression IFilterExpressionVisitor<QueryableExpressionArgument<TSource>, Expression>.VisitAndFilterExpression(AndFilterExpression expression, QueryableExpressionArgument<TSource> argument)
            => Expression.AndAlso(
                expression.Left.Accept(this, argument),
                expression.Right.Accept(this, argument)
            );

        Expression IFilterExpressionVisitor<QueryableExpressionArgument<TSource>, Expression>.VisitOrFilterExpression(OrFilterExpression expression, QueryableExpressionArgument<TSource> argument)
            => Expression.Or(
                expression.Left.Accept(this, argument),
                expression.Right.Accept(this, argument)
            );

        public Expression VisitMatchOperator(MatchOperator op, QueryableOperationArgument<TSource> argument)
        {
            if (argument.QueryableArgument.MemberExpression.Type == typeof(string))
            {
                return Expression.Call(
                    argument.QueryableArgument.MemberExpression,
                    _containsMethod,
                    argument.FilterExpression.Value.Accept(this)
                );
            }
            else
            {
                return Expression.Equal(argument.QueryableArgument.MemberExpression, Expression.Constant(true));
            }
        }

        public Expression VisitNotMatchOperator(NotMatchOperator op, QueryableOperationArgument<TSource> argument)
        {
            if (argument.QueryableArgument.MemberExpression.Type == typeof(string))
            {
                return Expression.Call(
                    argument.QueryableArgument.MemberExpression,
                    _notContainsMethod,
                    argument.FilterExpression.Value.Accept(this)
                );
            }
            else
            {
                return Expression.Equal(argument.QueryableArgument.MemberExpression, Expression.Constant(false));
            }
        }        

        ConstantExpression IValueVisitor<ConstantExpression>.VisitValue(SearchValue value)
            => Expression.Constant(value.Value.ToString());


        public QueryableContext<TSource> VisitSortAscending(SortAscending expression, QueryableExpressionArgument<TSource> argument)
        {
            var orderByExpression = Expression.Lambda<Func<TSource, object>>(argument.MemberExpression, _parameter);

            // TODO with IQueryable we can infer this from typing.
            if (argument.Context.StatementList.HasOrder)
            {
                argument.Context.Query = _defaultThenBy.Invoke((IOrderedQueryable<TSource>)argument.Context.Query, orderByExpression);

                return argument.Context;
            }

            argument.Context.Query = _defaultOrderBy.Invoke(argument.Context.Query, orderByExpression);
            argument.Context.StatementList.HasOrder = true;

            return argument.Context;
        }

        public QueryableContext<TSource> VisitSortDescending(SortDescending expression, QueryableExpressionArgument<TSource> argument)
        {
            var orderByExpression = Expression.Lambda<Func<TSource, object>>(argument.MemberExpression, _parameter);

            if (argument.Context.StatementList.HasOrder)
            {
                argument.Context.Query = _defaultThenByDescending.Invoke((IOrderedQueryable<TSource>)argument.Context.Query, orderByExpression);

                return argument.Context;
            }

            argument.Context.Query = _defaultOrderByDescending.Invoke(argument.Context.Query, orderByExpression);
            argument.Context.StatementList.HasOrder = true;

            return argument.Context;
        }
    }

    public struct QueryableExpressionArgument<TSource>
    {
        public MemberExpression MemberExpression { get; set; }
        public QueryableContext<TSource> Context { get; set; }
    }

    public struct QueryableOperationArgument<TSource>
    {
        public UnaryFilterExpression FilterExpression { get; set; }
        public QueryableExpressionArgument<TSource> QueryableArgument { get; set; }
    }    
}

using Parlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using YesSql.Indexes;
using YesSql.Services;

namespace YesSql.Tests.Search
{
    internal struct ExpressionArgument<TDocument, TIndex> where TDocument : class where TIndex : class, IIndex
    {
        public MemberExpression MemberExpression { get; set; }
        // public PropertyInfo PropertyInfo { get; set; }
        public StatementContext<TDocument, TIndex> Context { get; set; }

    }
    /// <summary>
    /// Takes a list of statements and applies them to a query.
    /// <summary>

    public class YesSqlStatementVisitor<TDocument, TIndex> :
        IStatementVisitor,
        IExpressionVisitor<ExpressionArgument<TDocument, TIndex>, Expression>,
        IOperatorVisitor<MethodInfo>,
        IValueVisitor<ConstantExpression>
        where TDocument : class where TIndex : class, IIndex
    {
        private static readonly MethodInfo _containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) })!;
        private static readonly MethodInfo _notContainsMethod = typeof(DefaultQueryExtensions).GetMethod("NotContains", new[] { typeof(string), typeof(string) })!;
        private readonly StatementContext<TDocument, TIndex> _context;
        private readonly ParameterExpression _parameter;

        public YesSqlStatementVisitor(StatementContext<TDocument, TIndex> context)
        {
            _context = context;
            _parameter = Expression.Parameter(typeof(TIndex));
        }

        public IQuery<TDocument, TIndex> Visit()
        {
            foreach (var statement in _context.StatementList.Statements)
            {
                statement.Accept(this);
            }

            return _context.Query;

        }

        void IStatementVisitor.VisitDefaultFilterStatement(DefaultFilterStatement statement)
        {
            var propertyInfo = _context.DefaultPropertyInfo;

            var propertyExpression = Expression.Property(_parameter, propertyInfo);
            var argument = new ExpressionArgument<TDocument, TIndex>
            {
                MemberExpression = propertyExpression,
                Context = _context
            };

            var body = statement.Expression.Accept(this, argument);
            var expression = Expression.Lambda<Func<TIndex, bool>>(body, _parameter);

            // TODO this could actually produce an expression to invoke
            // the Where clause on the query.
            // Like an IQueryable does.
            // Which could mean this method returns an expression,
            // which then gets invoked on the query.
            // When compiled it could be.
            // Func<IQuery<TDocument, TIndex>, IQuery<TDocument, TIndex>, Expression>
            // i.e. a func that takes a query and an expression, and returns a query.

            _context.Query.Where(expression);
        }

        void IStatementVisitor.VisitPropertyFilterStatement(PropertyFilterStatement statement)
        {
            var propertyInfo = _context.PropertyInfo[statement.Name];
            if (propertyInfo == null)
            {
                return; // fail silently when the incorrect property name is passed.
            }

            // TODO could be cached.
            var propertyExpression = Expression.Property(_parameter, propertyInfo);
            var argument = new ExpressionArgument<TDocument, TIndex>
            {
                MemberExpression = propertyExpression,
                Context = _context
            };

            var body = statement.Expression.Accept(this, argument);
            var expression = Expression.Lambda<Func<TIndex, bool>>(body, _parameter);

            _context.Query.Where(expression);
        }

        void IStatementVisitor.VisitSortStatement(SortStatement statement)
        {
            var propertyInfo = _context.PropertyInfo[statement.PropertyName.Value];
            if (propertyInfo == null)
            {
                return; // fail silently when an unmapped property name is passed.
            }

            var propertyExpression = Expression.Property(_parameter, propertyInfo);

            var argument = new ExpressionArgument<TDocument, TIndex>
            {
                MemberExpression = propertyExpression,
                Context = _context
            };

            statement.Sort.Accept(this, argument);
        }

        Expression IExpressionVisitor<ExpressionArgument<TDocument, TIndex>, Expression>.VisitUnaryFilterExpression(UnaryFilterExpression expression, ExpressionArgument<TDocument, TIndex> argument)
            => Expression.Call(
                argument.MemberExpression,
                expression.Operation.Accept(this), // This is method info here. can it be method info expression? // apparently not!
                expression.Value.Accept(this)
            );

        Expression IExpressionVisitor<ExpressionArgument<TDocument, TIndex>, Expression>.VisitAndFilterExpression(AndFilterExpression expression, ExpressionArgument<TDocument, TIndex> argument)
            => Expression.AndAlso(
                expression.Left.Accept(this, argument),
                expression.Right.Accept(this, argument)
            );

        Expression IExpressionVisitor<ExpressionArgument<TDocument, TIndex>, Expression>.VisitOrFilterExpression(OrFilterExpression expression, ExpressionArgument<TDocument, TIndex> argument)
            => Expression.Or(
                expression.Left.Accept(this, argument),
                expression.Right.Accept(this, argument)
            );

        MethodInfo IOperatorVisitor<MethodInfo>.VisitContainsOperator(ContainsOperator op)
            => _containsMethod;

        MethodInfo IOperatorVisitor<MethodInfo>.VisitNotContainsOperator(NotContainsOperator op)
            => _notContainsMethod;

        ConstantExpression IValueVisitor<ConstantExpression>.VisitValue(SearchValue value)
            => Expression.Constant(value.Value.ToString());

        Expression IExpressionVisitor<ExpressionArgument<TDocument, TIndex>, Expression>.VisitSortAscending(SortAscending expression, ExpressionArgument<TDocument, TIndex> argument)
        {
            var orderByExpression = Expression.Lambda<Func<TIndex, object>>(argument.MemberExpression, _parameter);
            if (argument.Context.HasOrder)
            {
                argument.Context.Query.ThenBy(orderByExpression);

                return null;
            }

            argument.Context.Query.OrderBy(orderByExpression);
            argument.Context.HasOrder = true;

            return null;
        }

        Expression IExpressionVisitor<ExpressionArgument<TDocument, TIndex>, Expression>.VisitSortDescending(SortDescending expression, ExpressionArgument<TDocument, TIndex> argument)
        {
            var orderByExpression = Expression.Lambda<Func<TIndex, object>>(argument.MemberExpression, _parameter);

            if (argument.Context.HasOrder)
            {
                argument.Context.Query.ThenByDescending(orderByExpression);

                return null;
            }

            argument.Context.Query.OrderByDescending(orderByExpression);
            argument.Context.HasOrder = true;

            return null;
        }
    }

    public class StatementContext<TDocument, TIndex> where TDocument : class where TIndex : class, IIndex
    {
        public Dictionary<TextSpan, PropertyInfo> PropertyInfo { get; set; } = new();
        public PropertyInfo DefaultPropertyInfo { get; set; }
        public StatementList StatementList { get; set; }
        public IQuery<TDocument, TIndex> Query { get; set; }

        public bool HasOrder { get; set; }
    }
}

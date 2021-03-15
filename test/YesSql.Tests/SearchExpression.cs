using Parlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using YesSql.Indexes;
using YesSql.Services;

namespace YesSql.Tests
{
    public class SearchValue
    {
        public SearchValue(TextSpan value)
        {
            Value = value;
        }

        public TextSpan Value { get; }

        public ConstantExpression Build()
            => Expression.Constant(Value.ToString());
    }

    public abstract class SearchExpression
    {
        public abstract Expression Build(Expression propertyExpression, PropertyInfo propertyInfo = null);
    }

    public abstract class SearchUnaryExpression : SearchExpression
    {
        protected SearchUnaryExpression(SearchValue value)
        {
            Value = value;
        }

        public SearchValue Value { get; }
    }


    public abstract class SearchBinaryExpression : SearchExpression
    {
        public SearchBinaryExpression(SearchExpression left, SearchExpression right)
        {
            Left = left;
            Right = right;
        }

        public SearchExpression Left { get; }
        public SearchExpression Right { get; }
    }


    public class SearchContainsExpression : SearchUnaryExpression
    {
        private static readonly MethodInfo _containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) })!;

        public SearchContainsExpression(SearchValue value) : base(value)
        {
        }

        public override Expression Build(Expression propertyExpression, PropertyInfo propertyInfo = null)
            => Expression.Call(propertyExpression, _containsMethod, Value.Build()); // This is the constant
    }

    public class SearchNotContainsExpression : SearchUnaryExpression
    {
        private static readonly MethodInfo _notContainsMethod = typeof(DefaultQueryExtensions).GetMethod("NotContains", new[] { typeof(string), typeof(string) })!;

        public SearchNotContainsExpression(SearchValue value) : base(value)
        {
        }

        public override Expression Build(Expression propertyExpression, PropertyInfo propertyInfo = null)
            => Expression.Call(_notContainsMethod, propertyExpression, Value.Build());             
    }

    public class SearchEqualExpression : SearchUnaryExpression
    {
        public SearchEqualExpression(SearchValue value) : base(value)
        {
        }

        public override Expression Build(Expression propertyExpression, PropertyInfo propertyInfo = null)
            => Expression.Equal(propertyExpression, Value.Build());
    }

    public class SearchAndExpression : SearchBinaryExpression
    {
        public SearchAndExpression(SearchExpression left, SearchExpression right) : base(left, right)
        {
        }

        public override Expression Build(Expression propertyExpression, PropertyInfo propertyInfo = null)
            => Expression.AndAlso(Left.Build(propertyExpression), Right.Build(propertyExpression));
    }

    public class SearchOrExpression : SearchBinaryExpression
    {
        public SearchOrExpression(SearchExpression left, SearchExpression right) : base(left, right)
        {
        }

        public override Expression Build(Expression propertyExpression, PropertyInfo propertyInfo = null)
            => Expression.Or(Left.Build(propertyExpression), Right.Build(propertyExpression));
    }

    public abstract class StatementExpression
    {
        public StatementExpression(SearchExpression qslExpression)
        {
            QslExpression = qslExpression;
        }

        public SearchExpression QslExpression { get; }

        public abstract IQuery<TDocument, TIndex> Query<TDocument, TIndex>(IQuery<TDocument, TIndex> query, PropertyInfo propertyInfo)
           where TDocument : class
           where TIndex : class, IIndex;

        public IQuery<TDocument, TIndex> Query<TDocument, TIndex>(IQuery<TDocument, TIndex> query, Expression<Func<TIndex, object>> predicate)
           where TDocument : class
           where TIndex : class, IIndex
        {
            var propName = ((MemberExpression)predicate.Body).Member.Name;

            return Query<TDocument, TIndex>(query, propName);
        }

        public IQuery<TDocument, TIndex> Query<TDocument, TIndex>(IQuery<TDocument, TIndex> query, string property)
           where TDocument : class
           where TIndex : class, IIndex
        {
            var propertyInfo = typeof(TIndex).GetProperties().FirstOrDefault(x => x.Name == property);

            return Query<TDocument, TIndex>(query, propertyInfo);
        }
    }

    public class SearchWhereStatement : StatementExpression
    {
        public SearchWhereStatement(SearchExpression qslExpression) : base(qslExpression)
        {
        }

        public Expression<Func<TIndex, bool>> Where<TIndex>(PropertyInfo propertyInfo) where TIndex : class, IIndex
        {
            var parameter = System.Linq.Expressions.Expression.Parameter(typeof(TIndex));

            var propertyExpression = System.Linq.Expressions.Expression.Property(parameter, propertyInfo);

            var body = QslExpression.Build(propertyExpression);

            var expression = System.Linq.Expressions.Expression.Lambda<Func<TIndex, bool>>(body, parameter);

            return expression;
        }

        public override IQuery<TDocument, TIndex> Query<TDocument, TIndex>(IQuery<TDocument, TIndex> query, PropertyInfo propertyInfo)
        {
            var expression = Where<TIndex>(propertyInfo);
            return query.Where(expression);
        }
    }


    public class SearchOrderExpression : SearchExpression
    {

        public override Expression Build(Expression propertyExpression, PropertyInfo propertyInfo)
            => Expression.Property(propertyExpression, propertyInfo);
        

    }

    public class SearchOrderDescendingStatement : StatementExpression
    {
        public SearchOrderDescendingStatement(SearchExpression qslExpression) : base(qslExpression)
        {
        }

        public override IQuery<TDocument, TIndex> Query<TDocument, TIndex>(IQuery<TDocument, TIndex> query, PropertyInfo propertyInfo)
        {
            var expression = OrderBy<TIndex>(propertyInfo);
            // We need a context property, and once an order property has been set on the context
            // everything else is then by.
            query.OrderByDescending(expression);

            return query;
        }

        private Expression<Func<TIndex, object>> OrderBy<TIndex>(PropertyInfo propertyInfo) where TIndex : class, IIndex
        {
            var parameter = Expression.Parameter(typeof(TIndex));

            var orderByProperty = QslExpression.Build(parameter, propertyInfo);

            var orderByExpression = Expression.Lambda<Func<TIndex, object>>(orderByProperty, parameter);

            return orderByExpression;
        }
    }

    // much more of a parse result.

    public class SearchStatement
    {
        public SearchStatement(TextSpan name, List<StatementExpression> statements)
        {
            Name = name;
            Statements = statements;
        }

        public TextSpan Name { get; }

        // This needs to be more of a grouping, but also the visitor should include a context
        // with mappings, and manage the whole arrangement.
        // To a large extent this is basically the visitor.
        public List<StatementExpression> Statements { get; }
        public IQuery<TDocument, TIndex> Query<TDocument, TIndex>(IQuery<TDocument, TIndex> query, Expression<Func<TIndex, object>> predicate)
           where TDocument : class
           where TIndex : class, IIndex
        {
            var propName = ((MemberExpression)predicate.Body).Member.Name;

            return Query(query, propName);
        }

        public IQuery<TDocument, TIndex> Query<TDocument, TIndex>(IQuery<TDocument, TIndex> query, string property)
              where TDocument : class
              where TIndex : class, IIndex
        {
            var propertyInfo = typeof(TIndex).GetProperties().FirstOrDefault(x => x.Name == property);

            return Query(query, propertyInfo);
        }

        public IQuery<TDocument, TIndex> Query<TDocument, TIndex>(IQuery<TDocument, TIndex> query, PropertyInfo propertyInfo)
              where TDocument : class
              where TIndex : class, IIndex
        {
            foreach (var statement in Statements)
            {
                statement.Query(query, propertyInfo);
            }

            return query;
        }        
    }

    public static class StatementExtensions
    {
        public static IQuery<TDocument, TIndex> WithStatement<TDocument, TIndex>(this IQuery<TDocument, TIndex>  query, SearchStatement statement, Expression<Func<TIndex, object>> predicate)
            where TDocument : class
            where TIndex : class, IIndex        
        {
            var propName = ((MemberExpression)predicate.Body).Member.Name;

            return WithStatement(query, statement, propName);
        }

        public static IQuery<TDocument, TIndex> WithStatement<TDocument, TIndex>(this IQuery<TDocument, TIndex>  query, SearchStatement statement, string property)
            where TDocument : class
            where TIndex : class, IIndex        
        {
            var propertyInfo = typeof(TIndex).GetProperties().FirstOrDefault(x => x.Name == property);

            return WithStatement(query, statement, propertyInfo);
        }  

        public static IQuery<TDocument, TIndex> WithStatement<TDocument, TIndex>(this IQuery<TDocument, TIndex>  query, SearchStatement statement, PropertyInfo propertyInfo)
            where TDocument : class
            where TIndex : class, IIndex        
        {
            statement.Query(query, propertyInfo);

            return query;
        }                
    }
}

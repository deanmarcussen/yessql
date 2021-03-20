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
    // The idea is Statement applies something. Expression returns something.

    public abstract class SearchStatement
    {

        public abstract TResult Accept<TArgument, TResult>(IStatementVisitor<TArgument, TResult> visitor, TArgument argument);
    }

    public class DefaultFilterStatement : SearchStatement
    {
        public DefaultFilterStatement(FilterExpression expression)
        {
            Expression = expression;
        }

        public FilterExpression Expression { get; }

        public override TResult Accept<TArgument, TResult>(IStatementVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitDefaultFilterStatement(this, argument);

        public override string ToString()
            => Expression.ToString();
    }

    public class PropertyFilterStatement : SearchStatement
    {
        public PropertyFilterStatement(string name, FilterExpression expression)
        {
            Name = name;
            Expression = expression;
        }

        public string Name { get; }
        public FilterExpression Expression { get; }

        public override TResult Accept<TArgument, TResult>(IStatementVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitPropertyFilterStatement(this, argument);

        public override string ToString()
            => $"{Name}: {Expression.ToString()}";
    }

    public class SortStatement : SearchStatement
    {
        public SortStatement(SearchValue propertyName, SortExpression sort)
        {
            PropertyName = propertyName;
            Sort = sort;
        }

        public SearchValue PropertyName { get; }
        public SortExpression Sort { get; }

        public override TResult Accept<TArgument, TResult>(IStatementVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitSortStatement(this, argument);

        public override string ToString()
            => $"sort:{PropertyName.ToString()}{Sort.ToString()}";
    }

    public class DefaultSortStatement : SortStatement
    {
        public DefaultSortStatement(SearchValue propertyName, SortExpression sort) : base(propertyName, sort)
        { }

        public override TResult Accept<TArgument, TResult>(IStatementVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitDefaultSortStatement(this, argument);
    }
}

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

    public class FieldFilterStatement : SearchStatement
    {
        public FieldFilterStatement(string name, FilterExpression expression)
        {
            Name = name;
            Expression = expression;
        }

        public string Name { get; }
        public FilterExpression Expression { get; }

        public override TResult Accept<TArgument, TResult>(IStatementVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitFieldFilterStatement(this, argument);

        public override string ToString()
            => $"{Name}:{Expression.ToString()}";
    }

    public class SortStatement : SearchStatement
    {
        public SortStatement(SearchValue fieldName, SortExpression sort)
        {
            FieldName = fieldName;
            Sort = sort;
        }

        public SearchValue FieldName { get; }
        public SortExpression Sort { get; }

        public override TResult Accept<TArgument, TResult>(IStatementVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitSortStatement(this, argument);

        public override string ToString()
            => $"sort:{FieldName.ToString()}{Sort.ToString()}";
    }

    public class DefaultSortStatement : SortStatement
    {
        public DefaultSortStatement(SearchValue propertyName, SortExpression sort) : base(propertyName, sort)
        { }

        public override TResult Accept<TArgument, TResult>(IStatementVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitDefaultSortStatement(this, argument);

        // TODO this gets put in twice sometimes, when it's also typed in.
        // that doesn't matter much it never runs twice, or if something else has run.
        // but we don't serialize it by default.

        public override string ToString()
            => String.Empty;          
    }
}

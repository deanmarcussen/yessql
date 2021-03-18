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

        public abstract void Accept(IStatementVisitor visitor);
    }

    public class DefaultFilterStatement : SearchStatement
    {
        public DefaultFilterStatement(FilterExpression expression)
        {
            Expression = expression;
        }

        public FilterExpression Expression { get; }

        public override void Accept(IStatementVisitor visitor)
            => visitor.VisitDefaultFilterStatement(this);

        public override string ToString()
            => Expression.ToString();
    }

    public class PropertyFilterStatement : SearchStatement
    {
        public PropertyFilterStatement(TextSpan name, FilterExpression expression)
        {
            Name = name;
            Expression = expression;
        }

        public TextSpan Name { get; }
        public FilterExpression Expression { get; }

        public override void Accept(IStatementVisitor visitor)
            => visitor.VisitPropertyFilterStatement(this);

        public override string ToString()
            => $"{Name.ToString()}: {Expression.ToString()}";
    }

    public class SortStatement : SearchStatement
    {
        public SortStatement(SearchValue propertyName, SortOperator sort)
        {
            PropertyName = propertyName;
            Sort = sort;
        }

        public SearchValue PropertyName { get; }
        public SortOperator Sort { get; }

        public override void Accept(IStatementVisitor visitor)
            => visitor.VisitSortStatement(this);

        public override string ToString()
            => $"sort:{PropertyName.ToString()}{Sort.ToString()}";
    }
}

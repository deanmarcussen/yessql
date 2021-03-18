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
    public class SearchValue
    {
        public SearchValue(TextSpan value)
        {
            Value = value;
        }

        public TextSpan Value { get; }

        public TResult Accept<TResult>(IValueVisitor<TResult> visitor)
            => visitor.VisitValue(this);

        public override string ToString()
            => Value.ToString();
    }

    public abstract class SearchOperator
    {
        public abstract TResult Accept<TResult>(IOperatorVisitor<TResult> visitor);
    }

    public class ContainsOperator : SearchOperator
    {
        private static readonly MethodInfo _containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) })!;

        public override TResult Accept<TResult>(IOperatorVisitor<TResult> visitor)
            => visitor.VisitContainsOperator(this);

        public override string ToString()
            => String.Empty;
    }

    public class NotContainsOperator : SearchOperator
    {
        private static readonly MethodInfo _notContainsMethod = typeof(DefaultQueryExtensions).GetMethod("NotContains", new[] { typeof(string), typeof(string) })!;

        public NotContainsOperator(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public override TResult Accept<TResult>(IOperatorVisitor<TResult> visitor)
            => visitor.VisitNotContainsOperator(this);

        public override string ToString()
            => Value;
    }


    public abstract class FilterExpression 
    {
        public abstract TResult Accept<TArgument, TResult>(IExpressionVisitor<TArgument, TResult> visitor, TArgument argument);
    }

    public class UnaryFilterExpression : FilterExpression
    {
        public UnaryFilterExpression(SearchOperator operation, SearchValue value)
        {
            Operation = operation;
            Value = value;
        }

        public SearchOperator Operation { get; }
        public SearchValue Value { get; set; }

        public override TResult Accept<TArgument, TResult>(IExpressionVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitUnaryFilterExpression(this, argument);

        public override string ToString()
            => $"{Operation.ToString()}{Value.ToString()}";
    }

    public class AndFilterExpression : FilterExpression
    {
        public AndFilterExpression(FilterExpression left, FilterExpression right)
        {
            Left = left;
            Right = right;
        }

        public FilterExpression Left { get; }
        public FilterExpression Right { get; }

        public override TResult Accept<TArgument, TResult>(IExpressionVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitAndFilterExpression(this, argument);

        public override string ToString()
            => $"{Left.ToString()} AND {Right.ToString()}";
    }

    public class OrFilterExpression : FilterExpression
    {
        public OrFilterExpression(FilterExpression left, FilterExpression right, string value)
        {
            Left = left;
            Right = right;
            Value = value;
        }

        public string Value { get; }
        public FilterExpression Left { get; }
        public FilterExpression Right { get; }

        public override TResult Accept<TArgument, TResult>(IExpressionVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitOrFilterExpression(this, argument);

        public override string ToString()
            => $"{Left.ToString()} {Value} {Right.ToString()}";
    }

    public abstract class SortOperator : FilterExpression { }
    public class SortAscending : SortOperator
    {
        public SortAscending(string value)
        {
            HasValue = !String.IsNullOrEmpty(value);
        }

        public bool HasValue { get; }

        public override TResult Accept<TArgument, TResult>(IExpressionVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitSortAscending(this, argument);

        public override string ToString()
            => HasValue ? "-asc" : String.Empty;
    }

    public class SortDescending : SortOperator
    {
        public override TResult Accept<TArgument, TResult>(IExpressionVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitSortDescending(this, argument);

        public override string ToString()
            => "-desc";
    }

    // The idea is Statement applies something. Expression returns something.

 


    // Final output.

    public class StatementList // Maybe? maybe not.
    {
        public StatementList(List<SearchStatement> statements)
        {
            Statements = statements;
        }

        public List<SearchStatement> Statements { get; }

        public override string ToString()
            => $"{String.Join("+", Statements.Select(s => s.ToString()))}";
    }
}

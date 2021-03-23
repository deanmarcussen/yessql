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
    public class SearchValue
    {
        public SearchValue(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public TResult Accept<TResult, TArgument>(IValueVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitValue(this, argument);

        public override string ToString()
            => Value;
    }

    public abstract class SearchOperator
    {
        public abstract TResult Accept<TArgument, TResult>(IOperatorVisitor<TArgument, TResult> visitor, TArgument argument);
    }

    public class MatchOperator : SearchOperator
    {
        public override TResult Accept<TArgument, TResult>(IOperatorVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitMatchOperator(this, argument);

        public override string ToString()
            => String.Empty;
    }

    public class NotMatchOperator : SearchOperator
    {
        public NotMatchOperator(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public override TResult Accept<TArgument, TResult>(IOperatorVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitNotMatchOperator(this, argument);

        public override string ToString()
            => Value;
    }


    public abstract class FilterExpression 
    {
        public abstract TResult Accept<TArgument, TResult>(IFilterExpressionVisitor<TArgument, TResult> visitor, TArgument argument);
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

        public override TResult Accept<TArgument, TResult>(IFilterExpressionVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitUnaryFilterExpression(this, argument);

        public override string ToString()
            => $"{Operation.ToString()}{Value.ToString()}";
    }

    public class AndFilterExpression : FilterExpression
    {
        public AndFilterExpression(FilterExpression left, FilterExpression right, string value)
        {
            Left = left;
            Right = right;
            HasValue = !String.IsNullOrWhiteSpace(value);
        }

        public FilterExpression Left { get; }
        public FilterExpression Right { get; }        
        public bool HasValue { get; }

        public override TResult Accept<TArgument, TResult>(IFilterExpressionVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitAndFilterExpression(this, argument);

        public override string ToString()
            => HasValue ? $"{Left.ToString()} AND {Right.ToString()}" : $"{Left.ToString()} {Right.ToString()}";
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

        public override TResult Accept<TArgument, TResult>(IFilterExpressionVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitOrFilterExpression(this, argument);

        public override string ToString()
            => $"{Left.ToString()} {Value} {Right.ToString()}";
    }

    public abstract class SortExpression 
    {
        public abstract TResult Accept<TArgument, TResult>(ISortExpressionVisitor<TArgument, TResult> visitor, TArgument argument);
    }

    public class SortAscending : SortExpression
    {
        public SortAscending(string value = null)
        {
            HasValue = !String.IsNullOrEmpty(value);
        }

        public bool HasValue { get; }

        public override TResult Accept<TArgument, TResult>(ISortExpressionVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitSortAscending(this, argument);

        public override string ToString()
            => HasValue ? "-asc" : String.Empty;
    }

    public class SortDescending : SortExpression
    {
        public override TResult Accept<TArgument, TResult>(ISortExpressionVisitor<TArgument, TResult> visitor, TArgument argument)
            => visitor.VisitSortDescending(this, argument);

        public override string ToString()
            => "-desc";
    }

    public class StatementList
    {        
        public StatementList()
        {
            Statements = new();
        }

        public StatementList(List<SearchStatement> statements)
        {
            Statements = statements;
        }

        public List<SearchStatement> Statements { get; set; }

        /// <summary>
        /// Indicates to the search processing context that a order statement has already been applied
        /// and that it should use ThenBy for the next statement.
        /// <summary>
        public bool HasOrder { get; set; }

        public override string ToString()
            => $"{String.Join(" ", Statements.Select(s => s.ToString()))}";
    }
}

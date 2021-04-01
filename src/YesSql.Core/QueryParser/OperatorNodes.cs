using System;
using System.Collections.Generic;
using System.Linq;

namespace YesSql.Core.QueryParser
{
    public abstract class OperatorNode<T> : QueryNode<T> where T : class
    {
    }

    public class UnaryNode<T> : OperatorNode<T> where T : class
    {
        public UnaryNode(string value, Func<IQuery<T>, string, IQuery<T>> query)
        {
            Value = value;
            Query = query;
        }

        public string Value { get; }
        public bool HasValue => !String.IsNullOrEmpty(Value);
        public Func<IQuery<T>, string, IQuery<T>> Query { get; }

        public override Func<IQuery<T>, IQuery<T>> Build(IQuery<T> query)
        {
            return result => Query(query, Value);
        }

        public override string ToNormalizedString()
            => ToString();

        public override string ToString()
            => $"{Value.ToString()}";
    }

    public class NotUnaryNode<T> : OperatorNode<T> where T : class
    {
        public NotUnaryNode(string operatorValue, UnaryNode<T> operation)
        {
            OperatorValue = operatorValue;
            Operation = operation;
        }

        public string OperatorValue { get; }
        public UnaryNode<T> Operation { get; }

        public override Func<IQuery<T>, IQuery<T>> Build(IQuery<T> query)
        {
            return result => query.All(
                Operation.Build(query)
            );
        }

        public override string ToNormalizedString()
            => ToString();

        public override string ToString()
            => $"{OperatorValue} {Operation.ToString()}";
    }

    public class OrNode<T> : OperatorNode<T> where T : class
    {
        public OrNode(OperatorNode<T> left, OperatorNode<T> right, string value)
        {
            Left = left;
            Right = right;
            Value = value;
        }

        public OperatorNode<T> Left { get; }
        public OperatorNode<T> Right { get; }
        public string Value { get; }

        public override Func<IQuery<T>, IQuery<T>> Build(IQuery<T> query)
        {
            return result => query.Any(
                Left.Build(query),
                Right.Build(query)
            );
        }

        public override string ToNormalizedString()
            => $"({Left.ToNormalizedString()} OR {Right.ToNormalizedString()})";

        public override string ToString()
            => $"{Left.ToString()} {Value} {Right.ToString()}";
    }

    public class AndNode<T> : OperatorNode<T> where T : class
    {
        public AndNode(OperatorNode<T> left, OperatorNode<T> right, string value)
        {
            Left = left;
            Right = right;
            Value = value;
        }

        public OperatorNode<T> Left { get; }
        public OperatorNode<T> Right { get; }
        public string Value { get; }

        public override Func<IQuery<T>, IQuery<T>> Build(IQuery<T> query)
        {
            return result => query.All(
                Left.Build(query),
                Right.Build(query)
            );
        }

        public override string ToNormalizedString()
            => $"({Left.ToNormalizedString()} AND {Right.ToNormalizedString()})";

        public override string ToString()
            => $"{Left.ToString()} {Value} {Right.ToString()}";
    }

    public class NotNode<T> : AndNode<T> where T : class
    {
        public NotNode(OperatorNode<T> left, OperatorNode<T> right, string value) : base(left, right, value)
        {
        }

        public override string ToNormalizedString()
            => $"({Left.ToNormalizedString()} NOT {Right.ToNormalizedString()})";

        public override string ToString()
            => $"{Left.ToString()} {Value} {Right.ToString()}";
    }

    /// <summary>
    /// Marks a node as being produced by a group request, i.e. () were specified
    /// </summary>

    public class GroupNode<T> : OperatorNode<T> where T : class
    {
        public GroupNode(OperatorNode<T> operation)
        {
            Operation = operation;
        }

        public OperatorNode<T> Operation { get; }

        public override Func<IQuery<T>, IQuery<T>> Build(IQuery<T> query)
            => Operation.Build(query);

        public override string ToNormalizedString()
            => ToString();

        public override string ToString()
            => $"({Operation.ToString()})";
    }

}

using System;
using System.Threading.Tasks;

namespace YesSql.Core.QueryParser
{
    public abstract class OperatorNode<T> : QueryNode<T> where T : class
    {
    }

    public class UnaryNode<T> : OperatorNode<T> where T : class
    {

        public UnaryNode(string value, Func<string, IQuery<T>, QueryExecutionContext<T>, ValueTask<IQuery<T>>> query, bool useMatch = true)
        {
            Value = value;
            Query = query;
            UseMatch = useMatch;
        }        

        public string Value { get; }
        public bool UseMatch { get; }
        public bool HasValue => !String.IsNullOrEmpty(Value);
        public Func<string, IQuery<T>, QueryExecutionContext<T>, ValueTask<IQuery<T>>> Query { get; }

        // so one simpleoption could be just to have a flag on this node. match/notmatch.

        // public override Func<IQuery<T>, ValueTask<IQuery<T>>> BuildAsync(IQuery<T> query, QueryExecutionContext context)
        // {
        //     return result => Query(Value, query, context);
        // } 

        public override Func<IQuery<T>, ValueTask<IQuery<T>>> BuildAsync(IQuery<T> query, QueryExecutionContext<T> context)
            // => BuildAsyncInternal(query, context, ((UnaryTermQueryOption<T>)context.CurrentTermOption).Query);    
        {
            // byt the time it gets to invoking steve. this is back to null.
            // if (context.CurrentQuery == null)
            // {
            //     var t =  (UnaryTermQueryOption<T>)context.CurrentTermOption.Query;
            //     context.CurrentQuery = t.MatchQuery;
            // }

            var q = context.CurrentTermOption.Query.MatchQuery;
            if (!this.UseMatch)
            {
                q = context.CurrentTermOption.Query.NotMatchQuery;
            }

            // var t =  (UnaryTermQueryOption<T>)context.CurrentTermOption.Query;
            var result = BuildAsyncInternal(query, context, q);
            // so it works when you don't reset the current query.
            // but that won't work properly when multiples are in.
            // context.CurrentQuery = null;

            return result;
        } 

        private Func<IQuery<T>, ValueTask<IQuery<T>>> BuildAsyncInternal(IQuery<T> query, QueryExecutionContext<T> context, Func<string, IQuery<T>, QueryExecutionContext<T>, ValueTask<IQuery<T>>> queryMethod)
        {
            return result => queryMethod(Value, query, context);
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

        public override Func<IQuery<T>, ValueTask<IQuery<T>>> BuildAsync(IQuery<T> query, QueryExecutionContext<T> context)
        {          
            // if (context.CurrentQuery == null)
            // {
                var t =  (BooleanTermQueryOption<T>)context.CurrentTermOption.Query;
                context.CurrentQuery = t.NotMatchQuery;
            // }
            var result = BuildAsyncInternal(query, context);
            // context.CurrentQuery = null;
            return result;
        }

        private Func<IQuery<T>, ValueTask<IQuery<T>>> BuildAsyncInternal(IQuery<T> query, QueryExecutionContext<T> context)
        {
            Func<IQuery<T>, Task<IQuery<T>>> t = (q) => Operation.BuildAsync(query, context)(q).AsTask();


            return result => new ValueTask<IQuery<T>>(query.AllAsync(
                t
                //  (q) => Operation.BuildAsync(query, context)(q).AsTask()
            )); 
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

        public override Func<IQuery<T>, ValueTask<IQuery<T>>> BuildAsync(IQuery<T> query, QueryExecutionContext<T> context)
        {
            return result => new ValueTask<IQuery<T>>(query.AnyAsync(
                (q) => Left.BuildAsync(query, context)(q).AsTask(),
                (q) => Right.BuildAsync(query, context)(q).AsTask()
            ));
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

        public override Func<IQuery<T>, ValueTask<IQuery<T>>> BuildAsync(IQuery<T> query, QueryExecutionContext<T> context)
        {
            return result => new ValueTask<IQuery<T>>(query.AllAsync(
                (q) => Left.BuildAsync(query, context)(q).AsTask(),
                (q) => Right.BuildAsync(query, context)(q).AsTask()
            ));
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

        public override Func<IQuery<T>, ValueTask<IQuery<T>>> BuildAsync(IQuery<T> query, QueryExecutionContext<T> context)
            => Operation.BuildAsync(query, context);

        public override string ToNormalizedString()
            => ToString();

        public override string ToString()
            => $"({Operation.ToString()})";
    }

}

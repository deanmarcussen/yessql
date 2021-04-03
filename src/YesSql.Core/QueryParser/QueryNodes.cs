using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YesSql.Core.QueryParser
{
    public abstract class QueryNode<T> where T : class
    {
        public abstract Func<IQuery<T>, ValueTask<IQuery<T>>> BuildAsync(IQuery<T> query, QueryExecutionContext<T> context);

        public abstract string ToNormalizedString();
    }

    public abstract class TermNode<T> : QueryNode<T> where T : class
    {
        public TermNode(string termName, OperatorNode<T> operation)
        {
            TermName = termName;
            Operation = operation;
        }

        public string TermName { get; }
        public OperatorNode<T> Operation { get; }

        public override Func<IQuery<T>, ValueTask<IQuery<T>>> BuildAsync(IQuery<T> query, QueryExecutionContext<T> context)
            => Operation.BuildAsync(query, context);            
    }

    public class NamedTermNode<T> : TermNode<T> where T : class
    {
        public NamedTermNode(string termName, OperatorNode<T> operation) : base(termName, operation)
        {
        }

        public override string ToNormalizedString()
            => $"{TermName}:{Operation.ToNormalizedString()}";

        public override string ToString()
            => $"{TermName}:{Operation.ToString()}";
    }


    public class DefaultTermNode<T> : TermNode<T> where T : class
    {
        public DefaultTermNode(string termName, OperatorNode<T> operation) : base(termName, operation)
        {
        }

        public override string ToNormalizedString() // normalizing includes the term name even if not specified.
            => $"{TermName}:{Operation.ToNormalizedString()}";

        public override string ToString()
            => $"{Operation.ToString()}";
    }
}

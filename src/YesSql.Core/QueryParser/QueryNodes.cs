using System;
using System.Collections.Generic;
using System.Linq;

namespace YesSql.Core.QueryParser
{
    public class TermList<T> where T : class
    {
        public TermList()
        {
            Terms = new();
        }

        public TermList(List<TermNode<T>> terms)
        {
            Terms = terms;
        }

        public List<TermNode<T>> Terms { get; }

        public IQuery<T> Build(IQuery<T> query)
        {
            foreach (var term in Terms)
            {
                query = term.Build(query).Invoke(query);
            }

            return query;
        }

        public string ToNormalizedString()
            => $"{String.Join(" ", Terms.Select(s => s.ToNormalizedString()))}";

        public override string ToString()
            => $"{String.Join(" ", Terms.Select(s => s.ToString()))}";
    }




    public abstract class QueryNode<T> where T : class
    {
        public abstract Func<IQuery<T>, IQuery<T>> Build(IQuery<T> query);

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

        public override Func<IQuery<T>, IQuery<T>> Build(IQuery<T> query)
        {
            return Operation.Build(query);
        }

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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YesSql.Core.QueryParser
{
    // I think we might turn this into a SearchEngine
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


        public async Task<IQuery<T>> ExecuteQueryAsync(IQuery<T> query, IServiceProvider serviceProvider) //TODO if queryexecutioncontext provided, use that.
        {
            var context = new QueryExecutionContext(serviceProvider);

            foreach (var term in Terms)
            {
                // TODO optimize value task later.

                // It's possible that context may at this point contain a CurrentQueryFunc property.
                // Which might be set here from a lookup to a Dictionary.
                // And then cast to mytype of queryfunc in the operation.

                var termQuery = term.BuildAsync(query, context);
                await termQuery.Invoke(query);

            }

            return query;
            
        }        

        public string ToNormalizedString()
            => $"{String.Join(" ", Terms.Select(s => s.ToNormalizedString()))}";

        public override string ToString()
            => $"{String.Join(" ", Terms.Select(s => s.ToString()))}";
    }

    public class QueryExecutionContext // struct?
    {
        public QueryExecutionContext(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        public IServiceProvider ServiceProvider { get; }
    }




    public abstract class QueryNode<T> where T : class
    {
        public abstract Func<IQuery<T>, ValueTask<IQuery<T>>> BuildAsync(IQuery<T> query, QueryExecutionContext context);

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

        // public override Func<IQuery<T>, IQuery<T>> Build(IQuery<T> query)
        // {
        //     return Operation.Build(query);
        // }

        // public ValueTask<IQuery<T>> ExecuteAsync(IQuery<T> query, QueryExecutionContext context)
        //     => Operation.ExecuteAsync(query, context);

        public override Func<IQuery<T>, ValueTask<IQuery<T>>> BuildAsync(IQuery<T> query, QueryExecutionContext context)
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

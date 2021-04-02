using Parlot;
using Parlot.Fluent;
using System;
using System.Threading.Tasks;
using YesSql.Indexes;
using static Parlot.Fluent.Parsers;

namespace YesSql.Core.QueryParser
{
    public abstract class OperatorParser<T> : Parser<OperatorNode<T>> where T : class
    { }

    public class UnaryParser<T> : OperatorParser<T> where T : class
    {
        // TODO make publically available, to can be retrieved for context dictionary.
        private readonly Func<string, IQuery<T>, QueryExecutionContext, ValueTask<IQuery<T>>> _query;

       
        public UnaryParser(Func<string, IQuery<T>, IQuery<T>> query)
        {
            _query = (q, val, ctx) => new ValueTask<IQuery<T>>(query(q, val));

            // TODO stop parsing this through the parser.
        }
       

        public UnaryParser(Func<string, IQuery<T>, QueryExecutionContext, ValueTask<IQuery<T>>> query)
        {
            _query = query;
        }        

        protected Parser<OperatorNode<T>> Parser 
            => Terms.String()
                .Or(
                    Terms.NonWhiteSpace()
                )
                    .Then<OperatorNode<T>>(x => new UnaryNode<T>(x.ToString(), _query));

        public override bool Parse(ParseContext context, ref ParseResult<OperatorNode<T>> result)
        {
            context.EnterParser(this);

            return Parser.Parse(context, ref result);
        }
    }
}

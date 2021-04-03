using Parlot;
using Parlot.Fluent;
using System;
using System.Threading.Tasks;
using YesSql.Indexes;
using static Parlot.Fluent.Parsers;

namespace YesSql.Core.QueryParser
{
    public abstract class OperatorParser<T> : Parser<OperatorNode<T>> where T : class
    {
        public abstract BooleanTermQueryOption<T> TermQueryOption { get; }
    }

    public class UnaryParser<T> : OperatorParser<T> where T : class
    {
        // TODO make publically available, to can be retrieved for context dictionary.
        private readonly Func<string, IQuery<T>, QueryExecutionContext<T>, ValueTask<IQuery<T>>> _query;

        // private UnaryParser()
        // {
        //     TermQueryOption = new UnaryTermQueryOption<T>(_query);
        // }


        public UnaryParser(Func<string, IQuery<T>, IQuery<T>> query) //: this()
        {
            _query = (q, val, ctx) => new ValueTask<IQuery<T>>(query(q, val));

            // TODO stop parsing this through the parser.
            TermQueryOption = new BooleanTermQueryOption<T>(_query);
        }


        public UnaryParser(Func<string, IQuery<T>, QueryExecutionContext<T>, ValueTask<IQuery<T>>> query) //: this()
        {
            _query = query;
            TermQueryOption = new BooleanTermQueryOption<T>(_query);
        }

        public override BooleanTermQueryOption<T> TermQueryOption { get; }

        protected Parser<OperatorNode<T>> Parser
            => Terms.String()
                .Or(
                    Terms.NonWhiteSpace()
                )
                    .Then<OperatorNode<T>>(static (context, node) => 
                    {
                        var ctx = (QueryParseContext<T>)context;
                        var queryOption = (UnaryTermQueryOption<T>)ctx.CurrentTermOption.Query;

                        return new UnaryNode<T>(node.ToString(), queryOption.MatchQuery);
                    });

        public override bool Parse(ParseContext context, ref ParseResult<OperatorNode<T>> result)
        {
            context.EnterParser(this);

            return Parser.Parse(context, ref result);
        }
    }

    public class UnaryTermQueryOption<T> : ITermQueryOption where T : class
    {
        public UnaryTermQueryOption(Func<string, IQuery<T>, QueryExecutionContext<T>, ValueTask<IQuery<T>>> matchQuery)
        {
            MatchQuery = matchQuery;
        }

        public Func<string, IQuery<T>, QueryExecutionContext<T>, ValueTask<IQuery<T>>> MatchQuery { get; }
    }
}

using Parlot;
using Parlot.Fluent;
using System.Collections.Generic;
using System.Linq;
using YesSql.Search;
using static Parlot.Fluent.Parsers;

namespace YesSql.Core.QueryParser
{
    public class QueryParser<T> : Parser<TermList<T>> where T : class
    {
        private static Parser<List<V>> _customSeparated<U, V>(Parser<U> separator, Parser<V> parser) => new CustomSeparated<U, V>(separator, parser);

        public QueryParser(params TermParser<T>[] parsers)
        {
            var Terms = OneOf(parsers);

            var Seperator = OneOf(parsers.Select(x => x.SeperatorParser).ToArray());

            Parser = _customSeparated(
                Seperator,
                Terms)
                    .Then(static x => new TermList<T>(x));
        }

        protected Parser<TermList<T>> Parser { get; }

        public override bool Parse(ParseContext context, ref ParseResult<TermList<T>> result)
        {
            context.EnterParser(this);

            return Parser.Parse(context, ref result);
        }
    }
}
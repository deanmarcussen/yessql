using System;
using System.Threading.Tasks;

namespace YesSql.Core.QueryParser.Fluent
{
    public static partial class QueryParsers
    {
        public static QueryParser<T> QueryParser<T>(params TermParser<T>[] termParsers) where T : class
            => new QueryParser<T>(termParsers);

        public static NamedTermParser<T> NamedTermParser<T>(string name, OperatorParser<T> operatorParser) where T : class
            => new NamedTermParser<T>(name, operatorParser);

        public static DefaultTermParser<T> DefaultTermParser<T>(string name, OperatorParser<T> operatorParser) where T : class
            => new DefaultTermParser<T>(name, operatorParser);

        public static UnaryParser<T> OneConditionParser<T>(Func<string, IQuery<T>, IQuery<T>> query) where T : class
            => new UnaryParser<T>(query);

        public static UnaryParser<T> OneConditionParser<T>(Func<string, IQuery<T>, QueryExecutionContext, ValueTask<IQuery<T>>> query) where T : class
            => new UnaryParser<T>(query);            

        public static BooleanParser<T> ManyConditionParser<T>(Func<string, IQuery<T>, IQuery<T>> matchQuery, Func<string, IQuery<T>, IQuery<T>> notMatchQuery) where T : class
            => new BooleanParser<T>(matchQuery, notMatchQuery);

        public static BooleanParser<T> ManyConditionParser<T>(Func<string, IQuery<T>, QueryExecutionContext, ValueTask<IQuery<T>>> matchQuery, Func<string, IQuery<T>, QueryExecutionContext, ValueTask<IQuery<T>>> notMatchQuery) where T : class
            => new BooleanParser<T>(matchQuery, notMatchQuery);            
    }
}
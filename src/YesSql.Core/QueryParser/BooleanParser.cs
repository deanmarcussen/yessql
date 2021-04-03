using Parlot;
using Parlot.Fluent;
using System;
using System.Threading.Tasks;
using static Parlot.Fluent.Parsers;

namespace YesSql.Core.QueryParser
{

    public class BooleanParser<T> : OperatorParser<T> where T : class
    {
        private readonly Func<string, IQuery<T>, QueryExecutionContext<T>, ValueTask<IQuery<T>>> _matchQuery;
        private readonly Func<string, IQuery<T>, QueryExecutionContext<T>, ValueTask<IQuery<T>>> _notMatchQuery;

        private BooleanParser()
        {
            var OperatorNode = Deferred<OperatorNode<T>>();

            var AndOperator = Terms.Text("AND")
                .Or(
                    Literals.Text("&&")
                );

            var NotOperator = Terms.Text("NOT")
                .Or(
                    Literals.Text("!")
                );

            var OrTextOperators = Terms.Text("OR")
                .Or(
                    Terms.Text("||")
                );

            // Operators that need to be NOT next when the default OR ' ' operator is found.
            var NotOrOperators = OneOf(AndOperator, NotOperator, OrTextOperators);

            // Default operator.
            var OrOperator = Literals.Text(" ").AndSkip(Not(NotOrOperators))// With this is is now catching everything.
                .Or(
                    OrTextOperators
                );

            var GroupNode = Between(Terms.Char('('), OperatorNode, Terms.Char(')'))
                .Then<OperatorNode<T>>(static x => new GroupNode<T>(x));

            var SingleNode = Terms.String() // A term name is never enclosed in strings.
                .Or(
                    // This must be aborted when it is consuming the next term.
                    Terms.Identifier().AndSkip(Not(Literals.Char(':'))) // TODO when this is NonWhiteSpace it sucks up paranthese. Will Identifier catch accents, i.e. multilingual.
                )
                    .Then<OperatorNode<T>>(static (context, node) => 
                    {
                        var ctx = (QueryParseContext<T>)context;
                        var queryOption = (BooleanTermQueryOption<T>)ctx.CurrentTermOption.Query;

                        return new UnaryNode<T>(node.ToString(), queryOption.MatchQuery);
                    });

            var Primary = SingleNode.Or(GroupNode);

            var UnaryNode = NotOperator.And(Primary)
                .Then<OperatorNode<T>>(static (context, node) =>
                {
                    var ctx = (QueryParseContext<T>)context;
                    var queryOption = (BooleanTermQueryOption<T>)ctx.CurrentTermOption.Query;
                    // mutate with the neg query.
                    var unaryNode = node.Item2 as UnaryNode<T>;

                    // TODO test what actually happens when just using NOT foo
                    return new NotUnaryNode<T>(node.Item1, new UnaryNode<T>(unaryNode.Value, queryOption.NotMatchQuery, false));
                })
                .Or(Primary);

            var AndNode = UnaryNode.And(ZeroOrMany(AndOperator.And(UnaryNode)))
                .Then<OperatorNode<T>>(static node =>
                {
                    // unary
                    var result = node.Item1;

                    foreach (var op in node.Item2)
                    {
                        result = new AndNode<T>(result, op.Item2, op.Item1);
                    }

                    return result;
                });

            OperatorNode.Parser = AndNode.And(ZeroOrMany(NotOperator.Or(OrOperator).And(AndNode)))
               .Then<OperatorNode<T>>(static (context, node) =>
               {
                    var ctx = (QueryParseContext<T>)context;
                    var queryOption = (BooleanTermQueryOption<T>)ctx.CurrentTermOption.Query;

                    static NotNode<T> CreateNotNode(OperatorNode<T> result, (string, OperatorNode<T>) op, BooleanTermQueryOption<T> queryOption)
                        => new NotNode<T>(result, new UnaryNode<T>(((UnaryNode<T>)op.Item2).Value, queryOption.NotMatchQuery, false), op.Item1);
                        // => new NotNode<T>(result, new NotUnaryNode<T>(op.Item1, (UnaryNode<T>)op.Item2), op.Item1);
                        // => new NotNode<T>(result, new NotUnaryNode<T>(result, op.Item2, op.Item1);
                    
                    static OrNode<T> CreateOrNode(OperatorNode<T> result, (string, OperatorNode<T>) op)
                        => new OrNode<T>(result, op.Item2, op.Item1);

                    // unary
                    var result = node.Item1;

                    foreach (var op in node.Item2)
                    {
                        result = op.Item1 switch
                        {
                            "NOT" => CreateNotNode(result, op, queryOption),
                            "!" => CreateNotNode(result, op, queryOption),
                            "OR" => CreateOrNode(result, op),
                            "||" => CreateOrNode(result, op),
                            " " => CreateOrNode(result, op),
                            _ => null
                        };
                    }

                   return result;
               });

            Parser = OperatorNode;

        }


        public BooleanParser(Func<string, IQuery<T>, IQuery<T>> matchQuery, Func<string, IQuery<T>, IQuery<T>> notMatchQuery) : this()
        {
            _matchQuery = (q, val, ctx) => new ValueTask<IQuery<T>>(matchQuery(q, val));
            _notMatchQuery = (q, val, ctx) => new ValueTask<IQuery<T>>(notMatchQuery(q, val));
            TermQueryOption = new BooleanTermQueryOption<T>(_matchQuery, _notMatchQuery);
        }

        public BooleanParser(
            Func<string, IQuery<T>, QueryExecutionContext<T>, ValueTask<IQuery<T>>> matchQuery,
            Func<string, IQuery<T>, QueryExecutionContext<T>, ValueTask<IQuery<T>>> notMatchQuery) : this()
        {
            _matchQuery = matchQuery;
            _notMatchQuery = notMatchQuery;
            TermQueryOption = new BooleanTermQueryOption<T>(_matchQuery, _notMatchQuery);
        }

        protected Parser<OperatorNode<T>> Parser { get; private set; }

        public override BooleanTermQueryOption<T> TermQueryOption { get; }

        public override bool Parse(ParseContext context, ref ParseResult<OperatorNode<T>> result)
        {
            context.EnterParser(this);

            return Parser.Parse(context, ref result);
        }
    }

    public class BooleanTermQueryOption<T> : UnaryTermQueryOption<T> where T : class
    {
        public BooleanTermQueryOption(Func<string, IQuery<T>, QueryExecutionContext<T>, ValueTask<IQuery<T>>> matchQuery) : base(matchQuery)
        {
        }

        public BooleanTermQueryOption(Func<string, IQuery<T>, QueryExecutionContext<T>, ValueTask<IQuery<T>>> matchQuery, Func<string, IQuery<T>, QueryExecutionContext<T>, ValueTask<IQuery<T>>> notMatchQuery) : base(matchQuery)
        {
            NotMatchQuery = notMatchQuery;
        }

        public Func<string, IQuery<T>, QueryExecutionContext<T>, ValueTask<IQuery<T>>> NotMatchQuery { get; }
    }
}

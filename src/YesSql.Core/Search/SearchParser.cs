using Parlot;
using Parlot.Fluent;
using System.Collections.Generic;
using static Parlot.Fluent.Parsers;

namespace YesSql.Search
{
    public interface ISearchParser
    {
        StatementList ParseSearch(string text);
    }

    public class SearchParser : Parser<StatementList>, ISearchParser
    {
        private static Parser<List<T>> CustomSeparated<U, T>(Parser<U> separator, Parser<T> parser) => new CustomSeparated<U, T>(separator, parser);

        private readonly Parser<StatementList> _parser;

        public StatementList ParseSearch(string text)
        {
            var context = new ParseContext(new Scanner(text));
            ParseResult<StatementList> result = default(ParseResult<StatementList>);
            if (this.Parse(context, ref result))
            {
                return result.Value;
            }

            return default(StatementList);
        }

        public override bool Parse(ParseContext context, ref ParseResult<StatementList> result)
        {
            context.EnterParser(this);

            return _parser.Parse(context, ref result);
        }

        /*
         * Grammar:
         * expression     => factor ( ( "-" | "+" ) factor )* ;
         * factor         => unary ( ( "/" | "*" ) unary )* ;
         * unary          => ( "-" ) unary
         *                 | primary ;
         * primary        => NUMBER
         *                  | "(" expression ")" ;
        */

        /*

        primary     => value
                        | field: value

        */

        // examples
        // steve -> where expression, default, uses Contains (like)
        // "steve"
        // NOT steve
        // text:steve -> where expression, named, uses Contains
        // text:+steve -> where expression, named, uses Contains
        // text : steve -> invalid does not allow whitespaces ??
        // text:NOT steve -> Not Contains
        // text:-steve -> not
        // text:steve AND bill -> is different to 
        // text:steve AND bill AND elon -> is different to 
        // text:"bill gates" -> is different to 
        // text:steve +bill ->   Steve AND BILL
        // text:steve bill -> steve AND bill
        // text:+steve -> uses Contains
        // text: +steve +jobs must contain steve AND jobs
        // text: steve | bill
        // text: steve OR bill
        // text: "bill gates"

        // NOTES:
        // + = whitespace (in query strings)

        // Seperator = + OR ' ' whitespace.
        // label:P0 label:P1 OR label:P0+label:P1 
        // 

        // opearators are optional.
        // appear before something.
        // steve is auto contains
        // !steve
        // -steve
        // NOT steve
        // EQUALS steve... maybe


        // so its cats NOT "hello world", where must be an operator OR
        // so it's Expression whitespace Expression
        // where Expression can contain whitespaces, until it is a complete expression
        public SearchParser()
        {
            var Value = Deferred<SearchValue>();

            var Operator = Deferred<SearchOperator>();

            // Sort
            var AscendingOperator = ZeroOrOne(
                    Literals.Text("-").SkipAnd(Literals.Text("asc", caseInsensitive: true))
                )
                .Then<SortExpression>(static x => new SortAscending(x));

            var DescendingOperator = Literals.Text("-desc", caseInsensitive: true)
                .Or(Literals.Text("-dsc", caseInsensitive: true))
                    .Then<SortExpression>(static x => new SortDescending());

            var SortOperator = DescendingOperator.Or(AscendingOperator);

            var SortValue = AnyCharBefore(Literals.Char('-'))
                .Then(static x => new SearchValue(x.ToString()));

            // Order only supports value. -asc or -desc -dsc are optional modifiers.
            var SortStatement = Terms.Text("sort", caseInsensitive: true)
                .SkipAnd(Literals.Char(':'))
                .SkipAnd(SortValue)
                .And(SortOperator)
                    .Then<SearchStatement>(static x => new SortStatement(x.Item1, x.Item2));

            // Operators

            var NotTextOperator = Literals.Text("NOT", caseInsensitive: true)
                .Then<SearchOperator>(static x => new NotMatchOperator("NOT "));

            // TODO remove. or include + as an operator.
            var NotDashOperator = Literals.Char('-')
                .Then<SearchOperator>(static x => new NotMatchOperator("-"));

            var NotExclamationOperator = Literals.Char('!')
                .Then<SearchOperator>(static x => new NotMatchOperator("!"));

            var NotOperator = OneOf(NotTextOperator, NotDashOperator, NotExclamationOperator);

            Operator.Parser = OneOf(NotOperator).AndSkip(Literals.WhiteSpace());

            var OperatorAndValue = Operator.And(Value)
                .Then<FilterExpression>(static x => new UnaryFilterExpression(x.Item1, x.Item2));

            var Seperator = Literals.WhiteSpace().SkipAnd(Terms.Identifier()).AndSkip(Literals.Char(':'));

            var SeperatorOrOperator = Seperator
                    .Then<SearchOperator>(static x => null)
                .Or(Operator);

            Value.Parser = Terms.String()
                .Or(
                    AnyCharBefore(SeperatorOrOperator)
                ).Then(static x => new SearchValue(x.ToString()));

            var UnaryFilter = OperatorAndValue.Or(
                Value
                    .Then<FilterExpression>(static value => new UnaryFilterExpression(new MatchOperator(), value))
                );

            // The idea is we want if to be steve AND balmer.
            var AndOperator = Literals.WhiteSpace()
                    .Then(x => x.ToString())
                .Or(
                    Terms.Text("AND")
                );

            var OrOperator = Terms.Text("OR").Or(Terms.Text("|")).AndSkip(Literals.WhiteSpace());

            // var LogicalOperators = AndOperator
            //     .Or(
            //         OrOperator
            //     );

            var LogicalOperators = Terms.Text("AND")
                   .Or(
                      Terms.Text("OR").Or(Terms.Text("|"))
                  )
                 .AndSkip(Literals.WhiteSpace());


            var BinaryOrUnaryFilter = UnaryFilter.And(ZeroOrMany(LogicalOperators.And(UnaryFilter)))
                .Then<FilterExpression>(static x =>
                {
                    // UnaryFilter
                    var result = x.Item1;
                    foreach (var filter in x.Item2)
                    {
                        result = filter.Item1 switch
                        {
                            "AND" => new AndFilterExpression(result, filter.Item2),
                            "OR" => new OrFilterExpression(result, filter.Item2, filter.Item1),
                            "|" => new OrFilterExpression(result, filter.Item2, filter.Item1),
                            _ => null
                        };
                    }

                    return result;
                });

            var FieldFilterStatement = Terms.Identifier().AndSkip(Literals.Char(':').AndSkip(Literals.WhiteSpace()))
                .And(
                    BinaryOrUnaryFilter
                )
                    .Then<SearchStatement>(static x => new FieldFilterStatement(x.Item1.ToString(), x.Item2));

            var DefaultFilterStatement = BinaryOrUnaryFilter
                .Then<SearchStatement>(static x => new DefaultFilterStatement(x));

            // Always consume property statements before the default statement.
            var Statements = OneOf(SortStatement, FieldFilterStatement, DefaultFilterStatement);

            _parser = CustomSeparated(
                Seperator,
                Statements)
                    .Then(static x => new StatementList(x));
        }
    }
}

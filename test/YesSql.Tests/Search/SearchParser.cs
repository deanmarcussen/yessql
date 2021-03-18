using Parlot;
using Parlot.Fluent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Xunit;
using YesSql.Indexes;
using YesSql.Services;
using static Parlot.Fluent.Parsers;

namespace YesSql.Tests.Search
{
    public class SearchParser : Parser<StatementList>
    {
        private readonly Parser<StatementList> _parser;

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
                .Then<SortOperator>(static x => new SortAscending(x));

            var DescendingOperator = Literals.Text("-desc", caseInsensitive: true)
                .Or(Literals.Text("-dsc", caseInsensitive: true))
                    .Then<SortOperator>(static x => new SortDescending());

            var SortOperator = DescendingOperator.Or(AscendingOperator);

            var SortValue = AnyCharBefore(Literals.Char('-'))
                .Then(static x => new SearchValue(x));

            // Order only supports value. -asc or -desc -dsc are optional modifiers.
            var SortStatement = Terms.Text("sort", caseInsensitive: true)
                .SkipAnd(Literals.Char(':'))
                .SkipAnd(SortValue)
                .And(SortOperator)
                    .Then<SearchStatement>(static x => new SortStatement(x.Item1, x.Item2));

            // Operators

            var NotTextOperator = Literals.Text("NOT", caseInsensitive: true)
                .Then<SearchOperator>(static x => new NotContainsOperator("NOT "));

            var NotDashOperator = Literals.Char('-')
                .Then<SearchOperator>(static x => new NotContainsOperator("- "));

            var NotExclamationOperator = Literals.Char('!')
                .Then<SearchOperator>(static x => new NotContainsOperator("! "));

            var NotOperator = OneOf(NotTextOperator, NotDashOperator, NotExclamationOperator);

            Operator.Parser = OneOf(NotOperator).AndSkip(Literals.WhiteSpace());

            var OperatorAndValue = Operator.And(Value)
                .Then<FilterExpression>(static x => new UnaryFilterExpression(x.Item1, x.Item2));

            var Seperator = Literals.Char('+').Or(Literals.Char(' '));

            var SeperatorOrOperator = Seperator
                    .Then<SearchOperator>(static x => null)
                .Or(Operator);

            Value.Parser = Terms.String()
                .Or(
                    AnyCharBefore(SeperatorOrOperator) // quick hack.
                ).Then(static x => new SearchValue(x));

            var UnaryFilter = OperatorAndValue.Or(
                Value
                    .Then<FilterExpression>(static value => new UnaryFilterExpression(new ContainsOperator(), value))
                );

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

            var PropertyFilterStatement = Terms.Identifier().AndSkip(Literals.Char(':').AndSkip(Literals.WhiteSpace()))
                .And(
                    BinaryOrUnaryFilter
                )
                    .Then<SearchStatement>(static x => new PropertyFilterStatement(x.Item1, x.Item2));

            var DefaultFilterStatement = BinaryOrUnaryFilter
                .Then<SearchStatement>(static x => new DefaultFilterStatement(x));

            // Always consume property statements before the default statement.
            var Statements = OneOf(SortStatement, PropertyFilterStatement, DefaultFilterStatement);

            _parser = Separated(
                    Seperator
                    // .AndSkip(
                    //     // Not(LogicalOperators)
                    // //     .Or(Literals.Text("+")
                    // // )
                    // )
                    ,
                Statements)
                    .Then(static x => new StatementList(x));
        }
    }

    public class SearchParserTests
    {
        private readonly SearchParser _parser = new();

        [Fact]
        public void ShouldParseValue()
        {
            Assert.Equal("steve", _parser.Parse("\"steve\"").ToString());
            Assert.Equal("steve", _parser.Parse("steve").ToString());

            Assert.Equal("NOT steve", _parser.Parse("NOT steve").ToString());
            Assert.Equal("NOT steve", _parser.Parse("NOTsteve").ToString());
            Assert.Equal("NOT steve", _parser.Parse("not steve").ToString());
            Assert.Equal("! steve", _parser.Parse("! steve").ToString());
            Assert.Equal("- steve", _parser.Parse("- steve").ToString());
            Assert.Equal("! steve", _parser.Parse("!steve").ToString());
            Assert.Equal("- steve", _parser.Parse("-steve").ToString());
            Assert.Equal("NOT steve", _parser.Parse("NOT \"steve\"").ToString());

        }

        [Fact]
        public void ShouldParseNamed()
        {
            // TODO decide about this whitespace normalization.
            Assert.Equal("name: steve", _parser.Parse("name:\"steve\"").ToString());
            Assert.Equal("name: steve", _parser.Parse("name:steve").ToString());
            Assert.Equal("name: NOT steve", _parser.Parse("name:NOT steve").ToString());
            Assert.Equal("name: NOT steve", _parser.Parse("name:not steve").ToString());
            Assert.Equal("name: ! steve", _parser.Parse("name:! steve").ToString());
            Assert.Equal("name: - steve", _parser.Parse("name:- steve").ToString());
            Assert.Equal("name: ! steve", _parser.Parse("name:!steve").ToString());
            Assert.Equal("name: - steve", _parser.Parse("name:-steve").ToString());
            Assert.Equal("name: NOT steve", _parser.Parse("name:NOT \"steve\"").ToString());
        }

        [Fact]
        public void ShouldParseOrder()
        {
            Assert.Equal("sort:name", _parser.Parse("sort:name").ToString());
            Assert.Equal("sort:name-asc", _parser.Parse("sort:name-asc").ToString());
            Assert.Equal("sort:name-desc", _parser.Parse("sort:name-desc").ToString());
            Assert.Equal("sort:name-desc", _parser.Parse("sort:name-dsc").ToString());
        }

        [Fact]
        public void SortOrderShouldBeCaseInsensitiveAndNormalize()
        {
            Assert.Equal("sort:name", _parser.Parse("SORT:name").ToString());
            // Property names are not normalized. They are considered case insensitive in the evaluation process.
            Assert.Equal("sort:NAME-asc", _parser.Parse("SORT:NAME-ASC").ToString());
            Assert.Equal("sort:name-desc", _parser.Parse("sort:name-DESC").ToString());
            Assert.Equal("sort:name-desc", _parser.Parse("sort:name-DSC").ToString());
        }

        [Fact]
        public void ShouldParseMultiple()
        {
            Assert.Equal(2, _parser.Parse("name:steve+email:steve@microsoft.com").Statements.Count());
            Assert.Equal(2, _parser.Parse("name:steve email:steve@microsoft.com").Statements.Count());
            // Need to decide how we're normalizing. Probably it's + everywhere for query strings. Not ' '
            // Currently it's only + for the statement list.
            Assert.Equal("name: steve+email: steve@microsoft.com", _parser.Parse("name:steve+email:steve@microsoft.com").ToString());
            Assert.Equal("name: steve+email: steve@microsoft.com", _parser.Parse("name:steve email:steve@microsoft.com").ToString());
        }

        [Fact]
        public void ShouldParseBinary()
        {
            Assert.Single(_parser.Parse("Steve AND Bill").Statements);
            Assert.Single(_parser.Parse("Steve | Bill").Statements);
            Assert.Single(_parser.Parse("Steve OR Bill").Statements);
            // TODO if we can get rid of the ' ' as a seperator option, and only use + then this won't need to be escaped.
            Assert.Single(_parser.Parse("\"Steve Balmer\" OR \"Bill Gates\"").Statements);

            Assert.Equal("Steve AND Bill", _parser.Parse("Steve ANDBill").ToString());
            Assert.Equal("Steve AND Bill", _parser.Parse("Steve AND Bill").ToString());
            Assert.Equal("Steve | Bill", _parser.Parse("Steve |Bill").ToString());
            Assert.Equal("Steve OR Bill", _parser.Parse("Steve ORBill").ToString());
            Assert.Equal("Steve OR Bill", _parser.Parse("Steve OR Bill").ToString());
            Assert.Equal("Steve Balmer OR Bill Gates", _parser.Parse("\"Steve Balmer\" OR \"Bill Gates\"").ToString());


            Assert.Equal("text: Steve AND Bill", _parser.Parse("text:Steve ANDBill").ToString());
        }

        [Fact]
        public void ShouldParseNamedBinary()
        {
            Assert.Single(_parser.Parse("text:Steve AND Bill").Statements);
            Assert.Single(_parser.Parse("text: Steve AND Bill").Statements);
            Assert.Single(_parser.Parse("text:Steve ANDBill").Statements);
            Assert.Single(_parser.Parse("text: Steve ANDBill").Statements);
            Assert.Single(_parser.Parse("text:Steve | Bill").Statements);
            Assert.Single(_parser.Parse("text: Steve | Bill").Statements);
            Assert.Single(_parser.Parse("text:Steve OR Bill").Statements);
            // TODO if we can get rid of the ' ' as a seperator option, and only use + then this won't need to be escaped.
            Assert.Single(_parser.Parse("text:\"Steve Balmer\" OR \"Bill Gates\"").Statements);

            Assert.Equal("text: Steve AND Bill", _parser.Parse("text:Steve ANDBill").ToString());
            Assert.Equal("text: Steve AND Bill", _parser.Parse("text: Steve AND Bill").ToString());
            Assert.Equal("text: Steve | Bill", _parser.Parse("text:Steve |Bill").ToString());
            Assert.Equal("text: Steve | Bill", _parser.Parse("text: Steve |Bill").ToString());
            Assert.Equal("text: Steve OR Bill", _parser.Parse("text: Steve ORBill").ToString());
            Assert.Equal("text: Steve OR Bill", _parser.Parse("text: Steve OR Bill").ToString());
            Assert.Equal("text: Steve Balmer OR Bill Gates", _parser.Parse("text: \"Steve Balmer\" OR \"Bill Gates\"").ToString());
        }
    }
}

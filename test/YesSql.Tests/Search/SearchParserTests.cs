using Parlot;
using Parlot.Fluent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Xunit;
using YesSql.Indexes;
using YesSql.Search;
using YesSql.Services;
using static Parlot.Fluent.Parsers;

namespace YesSql.Tests.Search
{
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
            Assert.Equal("!steve", _parser.Parse("! steve").ToString());
            // TODO I think we want to reserve - for a further seperator use.
            Assert.Equal("-steve", _parser.Parse("- steve").ToString());
            Assert.Equal("!steve", _parser.Parse("!steve").ToString());
            Assert.Equal("-steve", _parser.Parse("-steve").ToString());
            Assert.Equal("NOT steve", _parser.Parse("NOT \"steve\"").ToString());
        }

        [Fact]
        public void ShouldNotIncludeWhiteSpace()
        {
            // Should not include the whitespace.
            Assert.Equal("name:steve", _parser.Parse("name:steve ").ToString());
        }        

        [Fact]
        public void ShouldParseNamed()
        {
            // TODO decide about this whitespace normalization.
            Assert.Equal("name:steve", _parser.Parse("name:\"steve\"").ToString());
            Assert.Equal("name:steve", _parser.Parse("name:steve").ToString());
            Assert.Equal("name:NOT steve", _parser.Parse("name:NOT steve").ToString());
            Assert.Equal("name:NOT steve", _parser.Parse("name:not steve").ToString());
            Assert.Equal("name:!steve", _parser.Parse("name:! steve").ToString());
            Assert.Equal("name:-steve", _parser.Parse("name:- steve").ToString());
            Assert.Equal("name:!steve", _parser.Parse("name:!steve").ToString());
            Assert.Equal("name:-steve", _parser.Parse("name:-steve").ToString());
            Assert.Equal("name:NOT steve", _parser.Parse("name:NOT \"steve\"").ToString());
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
            Assert.Equal(2, _parser.Parse("name:steve email:steve@microsoft.com").Statements.Count());
            // Need to decide how we're normalizing. Probably it's + everywhere for query strings. Not ' '
            // Currently it's only + for the statement list.
            // TODO get rid of +
            // Assert.Equal("name:steve email: steve@microsoft.com", _parser.Parse("name:steve+email:steve@microsoft.com").ToString());
            Assert.Equal("name:steve email:steve@microsoft.com", _parser.Parse("name:steve email:steve@microsoft.com").ToString());
        }

        [Fact]
        public void ShouldParseBinary()
        {
            Assert.Single(_parser.Parse("Steve AND Bill").Statements);
            Assert.Single(_parser.Parse("Steve | Bill").Statements);
            Assert.Single(_parser.Parse("Steve OR Bill").Statements);
            Assert.Single(_parser.Parse("\"Steve Balmer\" OR \"Bill Gates\"").Statements);

            Assert.Equal("Steve AND Bill", _parser.Parse("Steve AND Bill").ToString());
            Assert.Equal("Steve | Bill", _parser.Parse("Steve | Bill").ToString());
            Assert.Equal("Steve OR Bill", _parser.Parse("Steve OR Bill").ToString());
            Assert.Equal("Steve Balmer OR Bill Gates", _parser.Parse("\"Steve Balmer\" OR \"Bill Gates\"").ToString());
            // So I think this should actually be Steve AND balmer Or Bill AND Gates
            // to make it more awesome.
            Assert.Equal("Steve Balmer OR Bill Gates", _parser.Parse("Steve Balmer OR Bill Gates").ToString());


            Assert.Equal("text:Steve AND Bill", _parser.Parse("text:Steve AND Bill").ToString());
        }

        [Fact]
        public void ShouldParseNamedBinary()
        {
            Assert.Single(_parser.Parse("text:Steve AND Bill").Statements);
            Assert.Single(_parser.Parse("text: Steve AND Bill").Statements);
            Assert.Single(_parser.Parse("text:Steve | Bill").Statements);
            Assert.Single(_parser.Parse("text: Steve | Bill").Statements);
            Assert.Single(_parser.Parse("text:Steve OR Bill").Statements);
            Assert.Single(_parser.Parse("text:\"Steve Balmer\" OR \"Bill Gates\"").Statements);

            Assert.Equal("text:Steve AND Bill", _parser.Parse("text: Steve AND Bill").ToString());
            Assert.Equal("text:Steve | Bill", _parser.Parse("text:Steve | Bill").ToString());
            Assert.Equal("text:Steve OR Bill", _parser.Parse("text: Steve OR Bill").ToString());
            Assert.Equal("text:Steve Balmer OR Bill Gates", _parser.Parse("text: \"Steve Balmer\" OR \"Bill Gates\"").ToString());
        }
    }

    public class SeperatorTests
    {

        public static Parser<List<T>> CustomSeparated<U, T>(Parser<U> separator, Parser<T> parser) => new CustomSeparated<U, T>(separator, parser);

    
        [Fact]
        public void ShouldSeperate()
        {
            Parser<TextSpan> se = Literals.WhiteSpace().SkipAnd(Terms.Identifier()).AndSkip(Literals.Char(':'));


            var defaultExpression = AnyCharBefore(se);
            // just ignoring the field name for the moment
            var fieldExpression = se.SkipAnd(AnyCharBefore(se));

            var expressions = OneOf(defaultExpression, fieldExpression);

            // Parser<(TextSpan, TextSpan)> expression = se.And(Terms.Identifier());

            var parser = CustomSeparated(se, expressions);


            Assert.True(expressions.TryParse("text:foo", out _));

            //NB foo is awesome is actually foo AND is AND awesome. maybe?
            //where foo is awesome is "foo is awesome"
            Assert.Equal("foo is awesome", expressions.Parse("foo is awesome").ToString());
            Assert.Equal("foo is awesome", expressions.Parse("text:foo is awesome").ToString());

            Assert.Equal("foo is awesome", expressions.Parse("text:foo is awesome").ToString());

            Assert.Equal(2, parser.Parse("text:foo author:bar").Count);

            Assert.Equal(2, parser.Parse("text:foo is awesome author:bar").Count);
        }
    }
}

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

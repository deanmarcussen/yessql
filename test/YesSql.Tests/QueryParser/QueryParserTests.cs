using Parlot;
using Parlot.Fluent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Xunit;
using YesSql.Core.QueryParser;
using YesSql.Indexes;
using YesSql.Search;
using YesSql.Services;
using YesSql.Tests.Indexes;
using YesSql.Tests.Models;
using static Parlot.Fluent.Parsers;

namespace YesSql.Tests.QueryParserTests
{
    public class SearchParserTests
    {

        [Fact]
        public void ShouldParseNamedTerm()
        {
            var parser = new QueryParser<Person>(new TermParser<Person>[]
                {
                    new TermParser<Person>
                    (
                        "name",
                        new UnaryParser<Person>((query, val) => query.With<PersonByName>(x => x.SomeName.Contains(val)))
                    )
                });

            Assert.Equal("name:steve", parser.Parse("name:steve").ToString());
        }

        [Theory]
        [InlineData("title:bill post", "title:(bill OR post)")]
        [InlineData("title:bill OR post", "title:(bill OR post)")] // , Skip="not working"So or still giving us problems
        [InlineData("title:beach AND sand", "title:(beach AND sand)")]
        [InlineData("title:beach AND sand OR mountain AND lake", "title:((beach AND sand) OR (mountain AND lake))")]
        [InlineData("title:(beach AND sand) OR (mountain AND lake)", "title:((beach AND sand) OR (mountain AND lake))")]
        [InlineData("title:(beach AND sand) OR (mountain AND lake) NOT lizards", "title:(((beach AND sand) OR (mountain AND lake)) NOT lizards)")]

        [InlineData("title:NOT beach", "title:NOT beach")]
        [InlineData("title:beach NOT mountain", "title:(beach NOT mountain)")]
        [InlineData("title:beach NOT mountain lake", "title:((beach NOT mountain) OR lake)")] // this is questionable, but with the right () can achieve anything
        public void Complex(string search, string normalized)
        {
            var parser = new QueryParser<Article>(new TermParser<Article>[]
            {
                new TermParser<Article>
                (
                    "title",
                    new BooleanParser<Article>(
                        (query, val) => query.With<ArticleByPublishedDate>(x => x.Title.Contains(val)),
                        (query, val) => query.With<ArticleByPublishedDate>(x => x.Title.IsNotIn<ArticleByPublishedDate>(s => s.Title, w => w.Title.Contains(val)))
                    )
                )
            });

            var result =  parser.Parse(search);

            Assert.Equal(normalized, result.ToNormalizedString());
        }
    }
}

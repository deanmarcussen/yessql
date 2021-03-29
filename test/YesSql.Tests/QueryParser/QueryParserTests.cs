using Parlot.Fluent;
using System;
using System.Linq;
using Xunit;
using YesSql.Services;
using YesSql.Tests.Indexes;
using YesSql.Tests.Models;
using static YesSql.Core.QueryParser.Fluent.QueryParsers;

namespace YesSql.Tests.QueryParserTests
{
    public class SearchParserTests
    {

        [Fact]
        public void ShouldParseNamedTerm()
        {
            var parser = QueryParser(
                NamedTermParser("name",
                    OneConditionParser<Person>((query, val) => query.With<PersonByName>(x => x.SomeName.Contains(val)))
                )
            );

            Assert.Equal("name:steve", parser.Parse("name:steve").ToString());
        }

        [Fact]
        public void ShouldParseDefaultTerm()
        {
            var parser = QueryParser(
                NamedTermParser("age",
                    OneConditionParser<Person>((query, val) =>
                    {
                        if (Int32.TryParse(val, out var age))
                        {
                            query.With<PersonByAge>(x => x.Age == age);
                        }

                        return query;
                    })
                ),
                DefaultTermParser("name",
                    OneConditionParser<Person>((query, val) => query.With<PersonByName>(x => x.SomeName.Contains(val)))
                )
            );

            Assert.Equal("name:steve", parser.Parse("name:steve").ToString());
            Assert.Equal("steve", parser.Parse("steve").ToString());
            Assert.Equal("steve age:20", parser.Parse("steve age:20").ToString());
            Assert.Equal(2, parser.Parse("steve age:20").Terms.Count());
            Assert.Equal("name:steve", parser.Parse("steve").ToNormalizedString());
        }

        [Fact]
        public void OrderOfDefaultTermShouldNotMatter()
        {
            var parser1 = QueryParser(
                 NamedTermParser("age",
                     OneConditionParser<Person>((query, val) =>
                     {
                         if (Int32.TryParse(val, out var age))
                         {
                             query.With<PersonByAge>(x => x.Age == age);
                         }

                         return query;
                     })
                 ),
                 DefaultTermParser("name",
                     OneConditionParser<Person>((query, val) => query.With<PersonByName>(x => x.SomeName.Contains(val)))
                 )
             );

            var parser2 = QueryParser(
                 DefaultTermParser("name",
                     OneConditionParser<Person>((query, val) => query.With<PersonByName>(x => x.SomeName.Contains(val)))
                 ),
                 NamedTermParser("age",
                     OneConditionParser<Person>((query, val) =>
                     {
                         if (Int32.TryParse(val, out var age))
                         {
                             query.With<PersonByAge>(x => x.Age == age);
                         }

                         return query;
                     })
                 )
             );

            Assert.Equal("steve age:20", parser1.Parse("steve age:20").ToString());
            Assert.Equal(2, parser1.Parse("steve age:20").Terms.Count());

            Assert.Equal("steve age:20", parser2.Parse("steve age:20").ToString());
            Assert.Equal(2, parser2.Parse("steve age:20").Terms.Count());
        }

        [Theory]
        [InlineData("title:bill post", "title:(bill OR post)")]
        [InlineData("title:bill OR post", "title:(bill OR post)")]
        [InlineData("title:beach AND sand", "title:(beach AND sand)")]
        [InlineData("title:beach AND sand OR mountain AND lake", "title:((beach AND sand) OR (mountain AND lake))")]
        [InlineData("title:(beach AND sand) OR (mountain AND lake)", "title:((beach AND sand) OR (mountain AND lake))")]
        [InlineData("title:(beach AND sand) OR (mountain AND lake) NOT lizards", "title:(((beach AND sand) OR (mountain AND lake)) NOT lizards)")]

        [InlineData("title:NOT beach", "title:NOT beach")]
        [InlineData("title:beach NOT mountain", "title:(beach NOT mountain)")]
        [InlineData("title:beach NOT mountain lake", "title:((beach NOT mountain) OR lake)")] // this is questionable, but with the right () can achieve anything
        public void Complex(string search, string normalized)
        {
            var parser = QueryParser(
                NamedTermParser("title",
                    ManyConditionParser<Article>(
                        (query, val) => query.With<ArticleByPublishedDate>(x => x.Title.Contains(val)),
                        (query, val) => query.With<ArticleByPublishedDate>(x => x.Title.IsNotIn<ArticleByPublishedDate>(s => s.Title, w => w.Title.Contains(val)))
                    )
                )
            );

            var result = parser.Parse(search);

            Assert.Equal(normalized, result.ToNormalizedString());
        }
    }
}

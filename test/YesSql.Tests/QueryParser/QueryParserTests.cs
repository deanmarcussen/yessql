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
            Assert.Equal("name:steve", parser.Parse("name:steve").ToNormalizedString());
        }

        [Fact]
        public void ShouldParseManyNamedTerms()
        {
            var parser = QueryParser(
                NamedTermParser("name",
                    OneConditionParser<Person>((query, val) => query.With<PersonByName>(x => x.SomeName.Contains(val)))
                ),
                NamedTermParser("status",
                    OneConditionParser<Person>((query, val) => query.With<PersonByName>(x => x.SomeName.Contains(val)))
                )
            );

            Assert.Equal("name:steve status:published", parser.Parse("name:steve status:published").ToString());
            Assert.Equal("name:steve status:published", parser.Parse("name:steve status:published").ToNormalizedString());
        }  

        [Fact]
        public void ShouldParseManyNamedTermsWithManyCondition()
        {
            var parser = QueryParser(
                NamedTermParser("name",
                    ManyConditionParser<Person>(null, null)
                ),
                NamedTermParser("status",
                    ManyConditionParser<Person>(null, null)
                )
            );

            Assert.Equal("name:steve status:published", parser.Parse("name:steve status:published").ToString());
            Assert.Equal("name:steve status:published", parser.Parse("name:steve status:published").ToNormalizedString());
        }   

        [Fact]
        public void ShouldParseDefaultTermWithManyCondition()
        {
            var parser = QueryParser(
                DefaultTermParser("name",
                    ManyConditionParser<Person>(null, null)
                ),
                NamedTermParser("status",
                    ManyConditionParser<Person>(null, null)
                )
            );

            Assert.Equal("steve status:published", parser.Parse("steve status:published").ToString());
            Assert.Equal("name:steve status:published", parser.Parse("steve status:published").ToNormalizedString());
        }  

        [Fact]
        public void ShouldParseDefaultTermWithManyConditionWhenLast()
        {
            var parser = QueryParser(
                NamedTermParser("status",
                    ManyConditionParser<Person>(null, null)
                ),
                DefaultTermParser("name",
                    ManyConditionParser<Person>(null, null)
                )
            );

            Assert.Equal("steve status:published", parser.Parse("steve status:published").ToString());
            Assert.Equal("name:steve status:published", parser.Parse("steve status:published").ToNormalizedString());
        }  

        [Fact]
        public void ShouldParseDefaultTermWithManyConditionWhenDefaultIsLast()
        {
            // TODO we just need a validation to stop this happening.
            // so really the answer is if you have two manys. you cannot have a default.
            var parser = QueryParser(
                DefaultTermParser("name",
                    ManyConditionParser<Person>(null, null)
                ),
                NamedTermParser("status",
                    ManyConditionParser<Person>(null, null)
                )
            );

            // Ah I see. output is status:(published OR steve)
            // so it doesn't matter what order they are registered in
            // but somehow we might need to get the default one to run first, and see what if can find.

            Assert.Equal("status:(published OR steve)", parser.Parse("status:published steve").ToNormalizedString());
        }                                        

        [Fact]
        public void ShouldParseDefaultTerm()
        {
            var parser = QueryParser(
                NamedTermParser("age", 
                    OneConditionParser<Person>(null)
                ),
                DefaultTermParser("name", 
                    OneConditionParser<Person>(null)
                )
            );

            Assert.Equal("name:steve", parser.Parse("name:steve").ToString());
            Assert.Equal("steve", parser.Parse("steve").ToString());
            Assert.Equal("steve age:20", parser.Parse("steve age:20").ToString());
            Assert.Equal("age:20 name:steve", parser.Parse("age:20 name:steve").ToString());
            Assert.Equal("age:20 steve", parser.Parse("age:20 steve").ToString());
            Assert.Equal(2, parser.Parse("steve age:20").Terms.Count());
            Assert.Equal("name:steve", parser.Parse("steve").ToNormalizedString());
        }

        [Fact]
        public void ShouldParseDefaultTermWithOneMany()
        {
            var parser = QueryParser(
                NamedTermParser("age", 
                    OneConditionParser<Person>(null)
                ),
                DefaultTermParser("name", 
                    ManyConditionParser<Person>(null, null)
                )
            );

            Assert.Equal("name:steve", parser.Parse("name:steve").ToString());
            Assert.Equal("steve", parser.Parse("steve").ToString());
            Assert.Equal("steve age:20", parser.Parse("steve age:20").ToString());
            Assert.Equal("age:20 name:steve", parser.Parse("age:20 name:steve").ToString());
            Assert.Equal("age:20 steve", parser.Parse("age:20 steve").ToString());
            Assert.Equal(2, parser.Parse("steve age:20").Terms.Count());
            Assert.Equal("name:steve", parser.Parse("steve").ToNormalizedString());
        }        

        [Fact]
        public void ShouldParseDefaultTermAtEndOfStatement()
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

            Assert.Equal("age:20 name:steve", parser.Parse("age:20 name:steve").ToString());
            Assert.Equal(2, parser.Parse("age:20 name:steve").Terms.Count());
            Assert.Equal("age:20 steve", parser.Parse("age:20 steve").ToString());
            Assert.Equal(2, parser.Parse("age:20 steve").Terms.Count());
        }        

        [Fact]
        public void OrderOfDefaultTermShouldNotMatter()
        {
            var namedParser = NamedTermParser("age",
                OneConditionParser<Person>(null) // TODO this is failing when it's a Many
            );

            var defaultParser = DefaultTermParser("name",
                ManyConditionParser<Person>(null, null)
            );

            var parser1 = QueryParser(
                namedParser,
                defaultParser
             );

            var parser2 = QueryParser(
                 defaultParser,
                 namedParser
             );
// sand status:published is returning 1 when it should return 2. it's the same as parser1.
            Assert.Equal("steve age:20", parser1.Parse("steve age:20").ToString());

            var result = parser1.Parse("steve age:20");
            Assert.Equal(2, result.Terms.Count());

            Assert.Equal("age:20 steve", parser1.Parse("age:20 steve").ToString());
            Assert.Equal(2, parser1.Parse("age:20 steve").Terms.Count());

            Assert.Equal("steve age:20", parser2.Parse("steve age:20").ToString());
            Assert.Equal(2, parser2.Parse("steve age:20").Terms.Count());

            Assert.Equal("age:20 steve", parser2.Parse("age:20 steve").ToString());
            Assert.Equal(2, parser2.Parse("age:20 steve").Terms.Count());
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

        [Theory]
        [InlineData("title:(bill)", "title:(bill)")]
        [InlineData("title:(bill AND steve) OR Paul", "title:((bill AND steve) OR Paul)")]
        [InlineData("title:((bill AND steve) OR Paul)", "title:((bill AND steve) OR Paul)")]
        public void ShouldGroup(string search, string normalized)
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

            Assert.Equal(search, result.ToString());
            Assert.Equal(normalized, result.ToNormalizedString());
        }        
    }
}

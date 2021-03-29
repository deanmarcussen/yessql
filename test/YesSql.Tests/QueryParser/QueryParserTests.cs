using Parlot.Fluent;
using System;
using System.Linq;
using Xunit;
using YesSql.Core.QueryParser;
using YesSql.Services;
using YesSql.Tests.Indexes;
using YesSql.Tests.Models;

namespace YesSql.Tests.QueryParserTests
{
    public class SearchParserTests
    {

        [Fact]
        public void ShouldParseNamedTerm()
        {
            var parser = new QueryParser<Person>(new TermParser<Person>[]
                {
                    new NamedTermParser<Person>
                    (
                        "name",
                        new UnaryParser<Person>((query, val) => query.With<PersonByName>(x => x.SomeName.Contains(val)))
                    )
                });

            Assert.Equal("name:steve", parser.Parse("name:steve").ToString());
        }

        [Fact]
        public void ShouldParseDefaultTerm()
        {
            var parser = new QueryParser<Person>(new TermParser<Person>[]
                {  
                    new NamedTermParser<Person>
                    (
                        "age",
                        new UnaryParser<Person>((query, val) => 
                        {
                            if (Int32.TryParse(val, out var age))
                            {
                                query.With<PersonByAge>(x => x.Age == age);
                            }
                            
                            return query;
                        })
                    ) ,
                     new DefaultTermParser<Person>
                    (
                        "name",
                        new UnaryParser<Person>((query, val) => query.With<PersonByName>(x => x.SomeName.Contains(val)))
                    ),                
                });

            Assert.Equal("name:steve", parser.Parse("name:steve").ToString());
            Assert.Equal("steve", parser.Parse("steve").ToString());
            Assert.Equal("steve age:20", parser.Parse("steve age:20").ToString());
            Assert.Equal(2, parser.Parse("steve age:20").Terms.Count());
            Assert.Equal("name:steve", parser.Parse("steve").ToNormalizedString());
        }   

        [Fact]
        public void OrderOfDefaultTermShouldNotMatter()
        {
            var parser = new QueryParser<Person>(new TermParser<Person>[]
                {  
                    new DefaultTermParser<Person>
                    (
                        "name",
                        new UnaryParser<Person>((query, val) => query.With<PersonByName>(x => x.SomeName.Contains(val)))
                    ),
                    new NamedTermParser<Person>
                    (
                        "age",
                        new UnaryParser<Person>((query, val) => 
                        {
                            if (Int32.TryParse(val, out var age))
                            {
                                query.With<PersonByAge>(x => x.Age == age);
                            }
                            
                            return query;
                        })
                    )                 
                });

            Assert.Equal("steve age:20", parser.Parse("steve age:20").ToString());
            Assert.Equal(2, parser.Parse("steve age:20").Terms.Count());
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
            var parser = new QueryParser<Article>(new NamedTermParser<Article>[]
            {
                new NamedTermParser<Article>
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

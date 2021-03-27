using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using YesSql.Provider.Sqlite;
using YesSql.Samples.FullText.Indexes;
using YesSql.Samples.FullText.Models;
using YesSql.Sql;
using YesSql.Services;

namespace YesSql.Samples.FullText
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var filename = "yessql.db";

            if (File.Exists(filename))
            {
                File.Delete(filename);
            }

            var configuration = new Configuration()
                .UseSqLite($"Data Source={filename};Cache=Shared")
                ;

            var store = await StoreFactory.CreateAndInitializeAsync(configuration);

            using (var connection = store.Configuration.ConnectionFactory.CreateConnection())
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction(store.Configuration.IsolationLevel))
                {
                    new SchemaBuilder(store.Configuration, transaction)
                        .CreateReduceIndexTable<ArticleByWord>(table => table
                            .Column<int>("Count")
                            .Column<string>("Word")
                        )
                        .CreateMapIndexTable<ArticleContent>(table => table
                            .Column<string>("Content"));

                    transaction.Commit();
                }
            }

            // register available indexes
            // store.RegisterIndexes<ArticleIndexProvider>();
            store.RegisterIndexes<ArticleContentProvider>();

/*
            // creating articles
            using (var session = store.CreateSession())
            {
                session.Save(new Article { Content = "This is a green fox" });
                session.Save(new Article { Content = "This is a yellow cat" });
                session.Save(new Article { Content = "This is a pink elephant" });
                session.Save(new Article { Content = "This is a green tiger" });
            }

            using (var session = store.CreateSession())
            {
                Console.WriteLine("Simple term: 'green'");
                var simple = await session
                    .Query<Article, ArticleByWord>(x => x.Word == "green")
                    .ListAsync();

                foreach (var article in simple)
                {
                    Console.WriteLine(article.Content);
                }

                Console.WriteLine("Boolean query: 'pink or (green and fox)'");
                var boolQuery = await session.Query<Article>()
                    .Any(
                        x => x.With<ArticleByWord>(a => a.Word == "pink"),
                        x => x.All(
                            x => x.With<ArticleByWord>(a => a.Word == "green"),
                            x => x.With<ArticleByWord>(a => a.Word == "fox")
                        )
                    ).ListAsync();

                foreach (var article in boolQuery)
                {
                    Console.WriteLine(article.Content);
                }

                var deleteQuery = await session.Query<Article>().ListAsync();
                foreach (var item in deleteQuery)
                {
                    session.Delete(item);
                }
            }
*/
            using (var session = store.CreateSession())
            {
                session.Save(new Article { Content = "Article by Steve Balmer about rabbits" });
                session.Save(new Article { Content = "Blog by Bill Gates about dogs" }); // about NOT dogs
                session.Save(new Article { Content = "Story by Paul Allen about cats" });
                session.Save(new Article { Content = "Post by Scott Guthrie about birds" });
                session.Save(new Article { Content = "Note by Scott Hansleman about guinea pigs" });
            }

            using (var session = store.CreateSession())
            {
                Console.WriteLine("query: 'steve OR bill OR story'");
                var boolQuery = await session.Query<Article>()
                        .With<ArticleContent>(a => a.Content.Contains("Steve") || a.Content.Contains("Bill") || a.Content.Contains("Story"))
                    .ListAsync();

                foreach (var article in boolQuery)
                {
                    Console.WriteLine("    - " + article.Content);
                }
            }

            using (var session = store.CreateSession())
            {
                Console.WriteLine("query works with AND : 'steve AND article'");
                var boolQuery = await session.Query<Article>()
                        .With<ArticleContent>(a => a.Content.Contains("Steve") && a.Content.Contains("Article"))
                        .ListAsync();


                if (boolQuery.Count() == 0)
                {
                    Console.WriteLine("no results");
                }
                else
                {
                    foreach (var article in boolQuery)
                    {
                        Console.WriteLine("    - " + article.Content);
                    }
                }
            }

            using (var session = store.CreateSession())
            {
                Console.WriteLine("Any: 'steve OR article'");
                var boolQuery = await session.Query<Article>()
                    .Any(
                        x => x.With<ArticleContent>(a => a.Content.Contains("Steve")),
                        x => x.With<ArticleContent>(a => a.Content.Contains("Story"))

                    )
                    .ListAsync();

                foreach (var article in boolQuery)
                {
                    Console.WriteLine("    - " + article.Content);
                }
            }

            using (var session = store.CreateSession())
            {
                Console.WriteLine("All NOT: 'about AND NOT dogs'");
                var boolQuery = await session.Query<Article>()
                    .All(
                        x => x.With<ArticleContent>(a => a.Content.Contains("about")),

                        x => x.With<ArticleContent>(b => b.Content.IsNotIn<ArticleContent>(s => s.Content, b => b.Content.Contains("dogs")))

                    )
                    .ListAsync();

                foreach (var article in boolQuery)
                {
                    Console.WriteLine("    - " + article.Content);
                }
            }  

          using (var session = store.CreateSession())
            {
                Console.WriteLine("mixed: 'scott OR rabbits AND NOT hansleman'");
                var boolQuery = await session.Query<Article>()
                // Works only when the and (All) is placed first.
                   .All(

                        x => x.With<ArticleContent>(b => b.Content.IsNotIn<ArticleContent>(s => s.Content, b => b.Content.Contains("hansleman")))

                    )                 
                    .Any(
                        x => x.With<ArticleContent>(a => a.Content.Contains("scott")),
                        x => x.With<ArticleContent>(a => a.Content.Contains("rabbits"))
                    )

                    .ListAsync();

                /* Translate this to a parser like language

// body is the parsers name for Content

DefaultTerm
(
    "body",
    // TODo don't do this. too complicated.
    BooleanExpression           --> as a parser this takes one operator, and == and != are combined with Or
    (
        Contains<ArticleContent>(x => x.Content)  Contains essentially is auto type casting to string. i.e. contains doesn't work with a date.
            .Or(
                NotContains<ArticleContent>(x => x.Content)
            )

    )

    BooleanExpression           --> as a parser this takes two operators, an And and Or. Always valueTask. val is always string, it can be converted inside the func.
    (
        Query((val, query) => query.With<ContentItemIndex>(x => x.DisplayText.Contains(val)),  Contains essentially is auto type casting to string. i.e. contains doesn't work with a date.
        Query((val, query) => query.With<ContentItemIndex>(x => x.DisplayText.IsNotIn<ContentItemIndex>(s => s.DisplayText, w => w.DisplayText.Contains(val))))

    )    
)                

Term
(
    "status",
    UnaryExpression
    (
        Query<ContentsStatus>((val, query) => // maybe try autocasting.
        {
            switch (val.ContentsStatus)
            {
                case ContentsStatus.Published:
                    return query.With<ContentItemIndex>(x => x.Published);
                    break;
                case ContentsStatus.Draft:
                    return query.With<ContentItemIndex>(x => x.Latest && !x.Published);
                    break;
                case ContentsStatus.AllVersions:
                    return query.With<ContentItemIndex>(x => x.Latest);
                    break;
                default:
                    return query.With<ContentItemIndex>(x => x.Latest);
                    break;
            }            

        })
    )
)

Term
(
    "culture",
    BooleanExpression
    (
        Query((val, query) => query.With<ContentItemIndex>(i.Published || i.Latest) && i.Culture == value), <-- Or specify value convertor?
        Query(val, query) => query.With<LocalizedContentItemIndex, string>(i => (i.Published || i.Latest) && i.Culture != value)
    )
)


ok so how do you combine these via options to configure the parser?

Terms.Add()

                */




                foreach (var article in boolQuery)
                {
                    Console.WriteLine("    - " + article.Content);
                }
            }                        
        }
    }
}
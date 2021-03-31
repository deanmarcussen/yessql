using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using YesSql.Core.QueryParser;
using YesSql.Provider.Sqlite;
using YesSql.Samples.Web.Indexes;
using YesSql.Samples.Web.Models;
using YesSql.Search;
using YesSql.Sql;
using YesSql.Services;
using static YesSql.Core.QueryParser.Fluent.QueryParsers;
using YesSql.Samples.Web.ViewModels;

namespace YesSql.Samples.Web
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            var filename = "yessql.db";

            if (File.Exists(filename))
            {
                File.Delete(filename);
            }

            services.AddDbProvider(config =>
                config.UseSqLite($"Data Source={filename};Cache=Shared"));

            services.AddMvc(options => options.EnableEndpointRouting = false);

            services.AddSingleton<IQueryParser<BlogPost>>(sp =>
                QueryParser(
                    NamedTermParser("status",
                        OneConditionParser<BlogPost>((query, val) =>
                        {
                            if (Enum.TryParse<ContentsStatus>(val, true, out var e))
                            {
                                switch (e)
                                {
                                    case ContentsStatus.Published:
                                        query.With<BlogPostIndex>(x => x.Published);
                                        break;
                                    case ContentsStatus.Draft:
                                        query.With<BlogPostIndex>(x => !x.Published);
                                        break;
                                    default:
                                        break;
                                }
                            }

                            return query;
                        })
                    ),
                    DefaultTermParser("title",
                        // OneConditionParser<BlogPost>(((query, val) => query.With<BlogPostIndex>(x => x.Title.Contains(val))))
                        ManyConditionParser<BlogPost>(
                            ((query, val) => query.With<BlogPostIndex>(x => x.Title.Contains(val))),
                            ((query, val) => query.With<BlogPostIndex>(x => x.Title.IsNotIn<BlogPostIndex>(s => s.Title, w => w.Title.Contains(val))))
                        )
                    )
                )
            );
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            var store = app.ApplicationServices.GetRequiredService<IStore>();
            store.RegisterIndexes(new[] { new BlogPostIndexProvider() });

            using (var connection = store.Configuration.ConnectionFactory.CreateConnection())
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction(store.Configuration.IsolationLevel))
                {
                    new SchemaBuilder(store.Configuration, transaction)
                        .CreateMapIndexTable<BlogPostIndex>(table => table
                            .Column<string>("Title")
                            .Column<string>("Author")
                            .Column<string>("Content")
                            .Column<DateTime>("PublishedUtc")
                            .Column<bool>("Published")
                        );

                    transaction.Commit();
                }
            }


            using (var session = app.ApplicationServices.GetRequiredService<IStore>().CreateSession())
            {
                session.Save(new BlogPost
                {
                    Title = "On the beach in the sand we found lizards",
                    Author = "Steve Balmer",
                    Content = "Steves first post",
                    PublishedUtc = DateTime.UtcNow,
                    Published = false,
                    Tags = Array.Empty<string>()
                });

                session.Save(new BlogPost
                {
                    Title = "On the beach in the sand we built sandcastles",
                    Author = "Bill Gates",
                    Content = "Bill first post",
                    PublishedUtc = DateTime.UtcNow,
                    Published = true,
                    Tags = Array.Empty<string>()
                });

                session.Save(new BlogPost
                {
                    Title = "On the mountain it snowed at the lake",
                    Author = "Paul Allen",
                    Content = "Pauls first post",
                    PublishedUtc = DateTime.UtcNow,
                    Published = true,
                    Tags = Array.Empty<string>()
                });
            }
        }
    }
}

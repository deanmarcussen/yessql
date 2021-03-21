using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using YesSql.Provider.Sqlite;
using YesSql.Samples.Web.Indexes;
using YesSql.Samples.Web.Models;
using YesSql.Search;
using YesSql.Sql;

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

            services.AddSingleton<ISearchParser, SearchParser>();

            services.AddScoped(typeof(IQueryIndexVisitor<,>), typeof(QueryIndexVisitor<,>));

            // services.AddScoped<IQueryIndexVisitor<BlogPost, BlogPostIndex>, QueryIndexVisitor<BlogPost, BlogPostIndex>>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            var store = app.ApplicationServices.GetRequiredService<IStore>();
            store.RegisterIndexes(new [] { new BlogPostIndexProvider()});

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
                    Title = "First",
                    Author = "Steve",
                    Content = "Steves first post",
                    PublishedUtc = DateTime.UtcNow,
                    Published = false,
                    Tags = Array.Empty<string>()
                });

                session.Save(new BlogPost
                {
                    Title = "Second",
                    Author = "Bill",
                    Content = "Bill first post",
                    PublishedUtc = DateTime.UtcNow,
                    Published = true,
                    Tags = Array.Empty<string>()
                });

                session.Save(new BlogPost
                {
                    Title = "Third",
                    Author = "Paul",
                    Content = "Pauls first post",
                    PublishedUtc = DateTime.UtcNow,
                    Published = true,
                    Tags = Array.Empty<string>()
                });                
            }
        }
    }
}


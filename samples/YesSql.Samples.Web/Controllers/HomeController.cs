using Microsoft.AspNetCore.Mvc;
using YesSql.Services;
using YesSql.Samples.Web.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using YesSql.Samples.Web.Indexes;
using Microsoft.AspNetCore.Routing;
using System;
using YesSql.Samples.Web.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using YesSql.Search;
using YesSql.Search.ModelBinding;

namespace YesSql.Samples.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IStore _store;
        private readonly ISearchParser _searchParser;
        private readonly IQueryIndexVisitor<BlogPost, BlogPostIndex> _queryIndexVisitor;

        public HomeController(IStore store, 
            IQueryIndexVisitor<BlogPost, BlogPostIndex> queryIndexVisitor,
            ISearchParser searchParser)
        {
            _store = store;
            _queryIndexVisitor = queryIndexVisitor;
            _searchParser = searchParser;
        }

        [Route("/")]
        public async Task<IActionResult> Index([ModelBinder(BinderType = typeof(StatementListModelBinder), Name = "q")] StatementList statementList)
        {
            IEnumerable<BlogPost> posts;

            // TODO fix this.
            if (statementList == null)
            {
                statementList = new StatementList();
            }

            string currentSearchText = String.Empty;

            using (var session = _store.CreateSession())
            {
                var query = session.Query<BlogPost>();

                if (statementList != null)
                {
                    var context = GetContext(statementList, query.With<BlogPostIndex>());

                    query = _queryIndexVisitor.Query(context);   
            
                    var visitor = new QueryIndexSerializer<BlogPost, BlogPostIndex>();

                    
                    currentSearchText = visitor.Serialize(context, new StatementList(context.StatementList.Statements), new StatementList(new List<SearchStatement>()));

                }


                posts = await query.ListAsync();
            }


            // var currentSearchText = statementList.ToString();

            var search = new Filter
            {
                SearchText = currentSearchText,
                OriginalSearchText = currentSearchText,
                Filters = new List<SelectListItem>()
                {
                    new SelectListItem("Select...", ""),
                    new SelectListItem("Published", "Published"),
                    new SelectListItem("Unpublished", "NOT Published")

                }
            };


            var vm = new BlogPostViewModel
            {
                BlogPosts = posts,
                Search = search
            };

            return View(vm);
        }



        [HttpPost("/")]
        public IActionResult IndexPost(Filter search)
        {
            // When the user has typed something into the search input no evaluation is required.
            // But we might normalize it for them.
            if (!String.Equals(search.SearchText, search.OriginalSearchText, StringComparison.OrdinalIgnoreCase))
            {
                return RedirectToAction("Index",
                      new RouteValueDictionary
                      {
                        { "q", search.SearchText }
                      }
                  );
            }


            // so this works, completely encodes the query string.
            // but it is completely decoded when it arrives in the model binder
            // so the parser only needs to know about these exact characters
            // + can be ignored as a concept.
            // and the seperator is WhiteSpace.SkipAnd(Terms.Identifier()).SKipAnd(Literals.Char(':'))

            // TODO parse query string into format.
            // serialize, removing duplicates, 
            // so that steve becomes author:steve.

            var queryStringStatementList = String.IsNullOrEmpty(search.SearchText) ? new StatementList() : _searchParser.ParseSearch(search.SearchText);


            var formStatements = new List<SearchStatement>();

            if (!String.IsNullOrEmpty(search.SelectedFilter))
            {
                // TODO rename to Field. It's more obvious that property.
                // TODO this can be a nice set of simple extension methods
                // to cover common use cases.
                formStatements.Add(
                    new FieldFilterStatement("status",
                        new UnaryFilterExpression(
                            new MatchOperator(),
                            new SearchValue(search.SelectedFilter)
                        )
                    )
                );
            }



            var context = GetContext(queryStringStatementList, null);


            var visitor = new QueryIndexSerializer<BlogPost, BlogPostIndex>();

            var serialized = visitor.Serialize(context, new StatementList(queryStringStatementList.Statements), new StatementList(formStatements));

            return RedirectToAction("Index",
                       new RouteValueDictionary
                       {
                        { "q", serialized }
                       }
                   );

        }

        private static QueryIndexContext<BlogPost, BlogPostIndex> GetContext(StatementList statementList, IQuery<BlogPost, BlogPostIndex> query)
        {
            // we could almost bury bits of this in the visitor/service. i.e. a create context method, to return an icontext.
            var context = new QueryIndexContext<BlogPost, BlogPostIndex>(statementList, query)
                .AddFilter("author", x => x.Author)
                .SetDefaultFilter("author")
                .AddFilter("status", x => x.Published)
                .AddSort("date", x => x.PublishedUtc)
                // This could also be easier.
                .SetDefaultSort(new DefaultSortStatement(new SearchValue("date"), new SortDescending()));
            return context;
        }
    }
}

/*
               .AddFilter("author", (x,y) => x.Author.Contains(y)) <- returns expression.
                .AddFilter("author", (x,y) => x.Author.Contains(y)) <- returns expression.
                .AddFilter("author", (q,y) => x.Author.Contains(y)) <- needs a conversion.

                // so returns an expression of Func<>. But may need a conversion property.




                // this is a good idea, but might not work because of the binary And.Or
                // so it would have to keep passing down.
                .AddFilter("author", x => x.Author, (query, exp) => {
                    query.Where(x => x.Author.)
                })
                '-' reservered as modifier
                .AddFilter("author", (x,y) => x.Where.Author.Contains(y)) <- returns expression.
                .AddFilter("author", (x,y) => x.Sort.Author.Contains(y)) <- returns expression.
                .SetDefaultFilter("author")
                .AddFilter("status", x => x.Published)
                .AddSort("date", x => x.PublishedUtc)
                .AddSort("sort", x, y
                .AddSort("sort-asc", x, y => {
                    // this might work if it passed a context down.
                    // where you could decide whether you're doing .OrderBy or .OrderByDescending. or .ThenBy
                    if (x.EndsWith("-desc"))
                    {

                    }
                }
                */
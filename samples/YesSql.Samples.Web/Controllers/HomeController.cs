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
        private readonly IQueryIndexVisitor<BlogPost, BlogPostIndex> _queryIndexVisitor;

        public HomeController(IStore store, IQueryIndexVisitor<BlogPost, BlogPostIndex> queryIndexVisitor)
        {
            _store = store;
            _queryIndexVisitor = queryIndexVisitor;
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

            using (var session = _store.CreateSession())
            {
                var query = session.Query<BlogPost>();

                if (statementList != null)
                {
                    // we could almost bury bits of this in the visitor/service. i.e. a create context method, to return an icontext.
                    var context = new QueryIndexContext<BlogPost, BlogPostIndex>(statementList, query.With<BlogPostIndex>())
                        .AddFilter("author", x => x.Author)
                        .SetDefaultFilter("author")
                        .AddFilter("status", x => x.Published)
                        .AddSort("date", x => x.PublishedUtc)
                        // This could also be easier.
                        .SetDefaultSort(new DefaultSortStatement(new SearchValue("date"), new SortDescending()));

                    query = _queryIndexVisitor.Query(context);
                }


                posts = await query.ListAsync();
            }


            var currentSearchText = statementList.ToString();

            var search = new Filter
            {
                SearchText = currentSearchText,
                OriginalSearchText = currentSearchText,
                Filters = new List<SelectListItem>()
                {
                    new SelectListItem("Select", ""),
                    new SelectListItem("Published", "Published"),
                    new SelectListItem("Unpublished", "Unpublished")

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

            // TODO. It would be nice if instead of not evaluated.
            // Parse it.
            // Add the other statements from the post
            // send it to a serializer which removes duplicates.
            // Then the clicks of other items, like filters,
            // Would be additive, rather than remove the existing search.
            // and there is a clear button to clear it.


            // When the user has typed something into the search input no evaluation is required.
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

  
            var statements = new List<SearchStatement>();
            
            if (!String.IsNullOrEmpty(search.SelectedFilter))
            {
                // TODO rename to Field. It's more obvious that property.
                // TODO this can be a nice set of simple extension methods
                // to cover common use cases.
                statements.Add(
                    new FieldFilterStatement("status", 
                        new UnaryFilterExpression(
                            new MatchOperator(),
                            new SearchValue(search.SelectedFilter)
                        )
                    )
                );
            }

           return RedirectToAction("Index",
                      new RouteValueDictionary
                      {
                        { "q", new StatementList(statements).ToString() }
                      }
                  );

   
        }
    }

}

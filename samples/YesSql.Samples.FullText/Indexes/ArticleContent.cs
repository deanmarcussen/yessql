using YesSql.Indexes;
using YesSql.Samples.FullText.Models;

namespace YesSql.Samples.FullText.Indexes
{
    public class ArticleContent : MapIndex
    {
        public string Content { get; set; }
    }

  public class ArticleContentProvider : IndexProvider<Article>
    {
        public override void Describe(DescribeContext<Article> context)
        {

            context
                .For<ArticleContent>()
                .Map(article => new ArticleContent { Content = article.Content });
        }
    }    
}

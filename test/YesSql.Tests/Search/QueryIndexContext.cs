using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using YesSql.Indexes;

namespace YesSql.Tests.Search
{
    public class QueryIndexContext<TDocument, TIndex>
        where TDocument : class where TIndex : class, IIndex
    {
        public QueryIndexContext(StatementList statementList, IQuery<TDocument, TIndex> query)
        {
            StatementList = statementList;
            Query = query;
        }

        public StatementList StatementList { get; set; }
        public IQuery<TDocument, TIndex> Query { get; set; }
        public QueryIndexFilterMap<TDocument, TIndex> DefaultFilter { get; set; }
        public Dictionary<string, QueryIndexFilterMap<TDocument, TIndex>> Filters { get; set; } = new();
        public Dictionary<string, QueryIndexSortMap<TDocument, TIndex>> Sorts { get; set; } = new();
    }

    public static class QueryIndexContextExtensions
    {
        public static QueryIndexContext<TDocument, TIndex> AddFilter<TDocument, TIndex>(
            this QueryIndexContext<TDocument, TIndex> context,
            string name, Expression<Func<TIndex, object>> predicate,
            Func<IQuery<TDocument, TIndex>, Expression<Func<TIndex, bool>>, IQuery<TDocument, TIndex>> filter = null
        ) where TDocument : class where TIndex : class, IIndex
            => context.AddFilter(name, ((MemberExpression)predicate.Body).Member.Name, filter);

        public static QueryIndexContext<TDocument, TIndex> AddFilter<TDocument, TIndex>(
            this QueryIndexContext<TDocument, TIndex> context,
            string name,
            string propertyName,
            Func<IQuery<TDocument, TIndex>, Expression<Func<TIndex, bool>>, IQuery<TDocument, TIndex>> filter = null
        )
            where TDocument : class where TIndex : class, IIndex
            => AddFilter(context, name, typeof(TIndex).GetProperties().FirstOrDefault(x => x.Name == propertyName), filter);

        public static QueryIndexContext<TDocument, TIndex> AddFilter<TDocument, TIndex>(
            this QueryIndexContext<TDocument, TIndex> context,
            string name,
            PropertyInfo propertyInfo,
            Func<IQuery<TDocument, TIndex>, Expression<Func<TIndex, bool>>, IQuery<TDocument, TIndex>> filter = null
        )
            where TDocument : class where TIndex : class, IIndex
        {
            context.Filters[name] = new QueryIndexFilterMap<TDocument, TIndex>(propertyInfo, filter);;

            return context;
        }

        /// <summary>
        /// Sets a default filter which will be applied when there is an unamed filter in the query.
        /// An <see cref="AddFilter"/> must be added to the <see cref="YesSqlSearchContext"/> first.
        /// <summary>
        public static QueryIndexContext<TDocument, TIndex> SetDefaultFilter<TDocument, TIndex>(
            this QueryIndexContext<TDocument, TIndex> context,
            string name
        )
            where TDocument : class where TIndex : class, IIndex
        {                
            if (context.DefaultFilter != null)
            {
                throw new InvalidOperationException("The default filter has already been set.");
            }

            if (!context.Filters.TryGetValue(name, out var filterMap))
            {
                throw new InvalidOperationException("Add the default filter first.");
            }

            context.DefaultFilter = filterMap;
            
            return context;
        }

        public static QueryIndexContext<TDocument, TIndex> AddSort<TDocument, TIndex>(
                   this QueryIndexContext<TDocument, TIndex> context,
                   string name, Expression<Func<TIndex, object>> predicate
               ) where TDocument : class where TIndex : class, IIndex
                   => context.AddSort(name, ((MemberExpression)predicate.Body).Member.Name);

        public static QueryIndexContext<TDocument, TIndex> AddSort<TDocument, TIndex>(
            this QueryIndexContext<TDocument, TIndex> context,
            string name,
            string propertyName
        )
            where TDocument : class where TIndex : class, IIndex
            => AddSort(context, name, typeof(TIndex).GetProperties().FirstOrDefault(x => x.Name == propertyName));

        public static QueryIndexContext<TDocument, TIndex> AddSort<TDocument, TIndex>(
            this QueryIndexContext<TDocument, TIndex> context,
            string name,
            PropertyInfo propertyInfo
        )
            where TDocument : class where TIndex : class, IIndex
        {
            context.Sorts[name] = new QueryIndexSortMap<TDocument, TIndex>(propertyInfo);

            return context;
        }

        /// <summary>
        /// Sets a default sort statement that will be applied only if the search query has not specified a sort.
        /// An <see cref="AddSort"/> must be added to the <see cref="YesSqlSearchContext"/> first.
        /// <summary>
        public static QueryIndexContext<TDocument, TIndex> SetDefaultSort<TDocument, TIndex>(
            this QueryIndexContext<TDocument, TIndex> context,
            DefaultSortStatement defaultSortStatement
        )
            where TDocument : class where TIndex : class, IIndex
        {
            if (!context.Sorts.TryGetValue(defaultSortStatement.PropertyName.Value, out var sortMap))
            {
                throw new InvalidOperationException("Add the sort first.");
            }

            context.StatementList.Statements.Add(defaultSortStatement);

            return context;
        }        
    }

    public class QueryIndexFilterMap<TDocument, TIndex> where TDocument : class where TIndex : class, IIndex
    {
        public QueryIndexFilterMap(PropertyInfo propertyInfo, Func<IQuery<TDocument, TIndex>, Expression<Func<TIndex, bool>>, IQuery<TDocument, TIndex>> filter = null)
        {
            PropertyInfo = propertyInfo;
            Filter = filter;
        }

        public PropertyInfo PropertyInfo { get; set; }
        public Func<IQuery<TDocument, TIndex>, Expression<Func<TIndex, bool>>, IQuery<TDocument, TIndex>> Filter { get; set; }
    }

    // TODo don't think it needs generics?
    public class QueryIndexSortMap<TDocument, TIndex> where TDocument : class where TIndex : class, IIndex
    {
        public QueryIndexSortMap(PropertyInfo propertyInfo)
        {
            PropertyInfo = propertyInfo;
        }

        public PropertyInfo PropertyInfo { get; set; }
    }
}

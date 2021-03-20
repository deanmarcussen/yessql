using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace YesSql.Tests.Search
{
    public class QueryableContext<TSource>
    {
        public QueryableContext(StatementList statementList, IQueryable<TSource> queryable)
        {
            StatementList = statementList;
            Query = queryable;
        }

        public StatementList StatementList { get; set; }
        public IQueryable<TSource> Query { get; set; }
        public QueryableFilterMap<TSource> DefaultFilter { get; set; }
        public Dictionary<string, QueryableFilterMap<TSource>> Filters { get; set; } = new();
        public Dictionary<string, QueryableSortMap<TSource>> Sorts { get; set; } = new();
    }

    public static class QueryableContextExtensions
    {
        public static QueryableContext<TSource> AddFilter<TSource>(
            this QueryableContext<TSource> context,
            string name, Expression<Func<TSource, object>> predicate,
            Func<IQueryable<TSource>, Expression<Func<TSource, bool>>, IQueryable<TSource>> filter = null
        )
            => context.AddFilter(name, ((MemberExpression)predicate.Body).Member.Name, filter);

        public static QueryableContext<TSource> AddFilter<TSource>(
            this QueryableContext<TSource> context,
            string name,
            string propertyName,
            Func<IQueryable<TSource>, Expression<Func<TSource, bool>>, IQueryable<TSource>> filter = null
        )
            => AddFilter(context, name, typeof(TSource).GetProperties().FirstOrDefault(x => x.Name == propertyName), filter);

        public static QueryableContext<TSource> AddFilter<TSource>(
            this QueryableContext<TSource> context,
            string name,
            PropertyInfo propertyInfo,
            Func<IQueryable<TSource>, Expression<Func<TSource, bool>>, IQueryable<TSource>> filter = null
        )
        {
            context.Filters[name] = new QueryableFilterMap<TSource>(propertyInfo, filter); ;

            return context;
        }

        /// <summary>
        /// Sets a default filter which will be applied when there is an unamed filter in the query.
        /// An <see cref="AddFilter"/> must be added to the <see cref="QueryableSearchContext"/> first.
        /// <summary>
        public static QueryableContext<TSource> SetDefaultFilter<TSource>(
            this QueryableContext<TSource> context,
            string name
        )
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

        public static QueryableContext<TSource> AddSort<TSource>(
                   this QueryableContext<TSource> context,
                   string name, Expression<Func<TSource, object>> predicate
               )
            => context.AddSort(name, ((MemberExpression)predicate.Body).Member.Name);

        public static QueryableContext<TSource> AddSort<TSource>(
            this QueryableContext<TSource> context,
            string name,
            string propertyName
        )
            => AddSort(context, name, typeof(TSource).GetProperties().FirstOrDefault(x => x.Name == propertyName));

        public static QueryableContext<TSource> AddSort<TSource>(
            this QueryableContext<TSource> context,
            string name,
            PropertyInfo propertyInfo
        )
        {
            context.Sorts[name] = new QueryableSortMap<TSource>(propertyInfo);

            return context;
        }

        /// <summary>
        /// Sets a default sort statement that will be applied only if the search query has not specified a sort.
        /// An <see cref="AddSort"/> must be added to the <see cref="QueryableSearchContext"/> first.
        /// <summary>
        public static QueryableContext<TSource> SetDefaultSort<TSource>(
            this QueryableContext<TSource> context,
            DefaultSortStatement defaultSortStatement
        )
        {
            if (!context.Sorts.TryGetValue(defaultSortStatement.PropertyName.Value, out var sortMap))
            {
                throw new InvalidOperationException("Add the sort first.");
            }

            context.StatementList.Statements.Add(defaultSortStatement);

            return context;
        }
    }

    public class QueryableFilterMap<TSource>
    {
        public QueryableFilterMap(PropertyInfo propertyInfo, Func<IQueryable<TSource>, Expression<Func<TSource, bool>>, IQueryable<TSource>> filter = null)
        {
            PropertyInfo = propertyInfo;
            Filter = filter;
        }

        public PropertyInfo PropertyInfo { get; set; }
        public Func<IQueryable<TSource>, Expression<Func<TSource, bool>>, IQueryable<TSource>> Filter { get; set; }
    }

    public class QueryableSortMap<TSource>
    {
        public QueryableSortMap(PropertyInfo propertyInfo)
        {
            PropertyInfo = propertyInfo;
        }

        public PropertyInfo PropertyInfo { get; set; }
    }
}

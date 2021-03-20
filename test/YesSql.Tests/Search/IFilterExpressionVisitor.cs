using Parlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using YesSql.Indexes;
using YesSql.Services;

namespace YesSql.Tests.Search
{

    public interface IFilterExpressionVisitor<TArgument, TResult>
    {
        TResult VisitUnaryFilterExpression(UnaryFilterExpression expression, TArgument argument);
        TResult VisitAndFilterExpression(AndFilterExpression expression, TArgument argument);
        TResult VisitOrFilterExpression(OrFilterExpression expression, TArgument argument);
        // TResult VisitSortAscending(SortAscending expression, TArgument argument);
        // TResult VisitSortDescending(SortDescending expression, TArgument argument);
    }  
}

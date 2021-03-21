using Parlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using YesSql.Indexes;
using YesSql.Services;

namespace YesSql.Search
{

    public interface IFilterExpressionVisitor<TArgument, TResult>
    {
        TResult VisitUnaryFilterExpression(UnaryFilterExpression expression, TArgument argument);
        TResult VisitAndFilterExpression(AndFilterExpression expression, TArgument argument);
        TResult VisitOrFilterExpression(OrFilterExpression expression, TArgument argument);
    }  
}

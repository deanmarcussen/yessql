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

    public interface IOperatorVisitor<TArgument, TResult>
    {
        TResult VisitMatchOperator(MatchOperator op, TArgument argument);
        TResult VisitNotMatchOperator(NotMatchOperator op, TArgument argument);
    }  
}

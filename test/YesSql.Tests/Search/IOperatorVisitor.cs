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

    public interface IOperatorVisitor<TResult>
    {
        TResult VisitContainsOperator(ContainsOperator op);
        TResult VisitNotContainsOperator(NotContainsOperator op);
    }  
}

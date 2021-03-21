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

    public interface IValueVisitor<TResult>
    {
        TResult VisitValue(SearchValue value);
    }  
}

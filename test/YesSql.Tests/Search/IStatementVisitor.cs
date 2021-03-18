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
    /// <summary>
    /// Takes a list of statements and applies them to a query.
    /// <summary>

    // public interface IStatementVisitor<T>
    // {
    //     T VisitDefaultFilterStatement(DefaultFilterStatement statement);
    //     T VisitPropertyFilterStatement(PropertyFilterStatement statement);
    //     T VisitSortStatement(SortStatement statement);
    // }

    public interface IStatementVisitor
    {
        void VisitDefaultFilterStatement(DefaultFilterStatement statement);
        void VisitPropertyFilterStatement(PropertyFilterStatement statement);
        void VisitSortStatement(SortStatement statement);
    }    
}

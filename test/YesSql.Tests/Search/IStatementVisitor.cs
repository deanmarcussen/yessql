namespace YesSql.Tests.Search
{
    public interface IStatementVisitor<TArgument, TResult>
    {
        TResult VisitDefaultFilterStatement(DefaultFilterStatement statement, TArgument argument);
        TResult VisitPropertyFilterStatement(PropertyFilterStatement statement, TArgument argument);
        TResult VisitSortStatement(SortStatement statement, TArgument argument);
        TResult VisitDefaultSortStatement(DefaultSortStatement statement, TArgument argument);
    }    
}

using Parlot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using YesSql.Indexes;
using YesSql.Services;

namespace YesSql.Search
{
    /// <summary>
    /// Takes a list of statements and serializes them, removing duplicates.
    /// <summary>
    public class QueryIndexSerializer<TDocument, TIndex> :
        IStatementVisitor<QueryIndexSerializerContext<TDocument, TIndex>, QueryIndexSerializerContext<TDocument, TIndex>>
        // ,
        // IFilterExpressionVisitor<QueryIndexExpressionArgument<TDocument, TIndex>, Expression>,
        // ISortExpressionVisitor<QueryIndexExpressionArgument<TDocument, TIndex>, QueryIndexContext<TDocument, TIndex>>,
        // IOperatorVisitor<QueryOperationArgument<TDocument, TIndex>, Expression>
        where TDocument : class where TIndex : class, IIndex
    {

        private readonly ExpressionValueVisitor _valueVisitor = new ExpressionValueVisitor();

        public string Serialize(QueryIndexContext<TDocument, TIndex> context, StatementList queryStringStatementList, StatementList formStatementList)
        {

            context.StatementList.Statements = new List<SearchStatement>();

            // should be a merge from the other.

            foreach(var queryString in queryStringStatementList.Statements)
            {
                if (queryString is FieldFilterStatement field)
                {
                    var formField = formStatementList.Statements.OfType<FieldFilterStatement>().FirstOrDefault(x => x.Name == field.Name);
                    if (formField == null)
                    {
                        context.StatementList.Statements.Add(field);
                    }
                    else
                    {
                        context.StatementList.Statements.Add(formField);
                        formStatementList.Statements.Remove(formField);
                    }
                }
                else
                {
                    context.StatementList.Statements.Add(queryString);
                }
            }

            context.StatementList.Statements.AddRange(formStatementList.Statements);

            var serializeContext = new QueryIndexSerializerContext<TDocument, TIndex>
            {
                Context = context,
                StringBuilder = new StringBuilder()
            };


            foreach (var statement in context.StatementList.Statements)
            {
                var t = statement.Accept(this, serializeContext);
                // TODO only if not the last one.
                t.StringBuilder.Append(' ');
            }

            // return context.Query;

            return serializeContext.StringBuilder.ToString().TrimEnd();
            // var s = context.DefaultFilterName + ':' + statement.Expression.ToString();
        }

        public QueryIndexSerializerContext<TDocument, TIndex> VisitDefaultFilterStatement(DefaultFilterStatement statement, QueryIndexSerializerContext<TDocument, TIndex> argument)
        {
            if (argument.Context.DefaultFilter == null)
            {
                return argument;
            }

            if (argument.SerializedDefaultFilter == true)
            {
                return argument;
            }

            // argument.StringBuilder.Append(argument.Context.DefaultFilterName);
            // argument.StringBuilder.Append(':');
            argument.StringBuilder.Append(statement.Expression.ToString());

            argument.SerializedDefaultFilter = true;

            return argument;
        }



        public QueryIndexSerializerContext<TDocument, TIndex> VisitFieldFilterStatement(FieldFilterStatement statement, QueryIndexSerializerContext<TDocument, TIndex> argument)
        {
            argument.StringBuilder.Append(statement.ToString());
            
            return argument;
        }

        public QueryIndexSerializerContext<TDocument, TIndex> VisitDefaultSortStatement(DefaultSortStatement statement, QueryIndexSerializerContext<TDocument, TIndex> argument)
        {
            return argument;
        }
        public QueryIndexSerializerContext<TDocument, TIndex> VisitSortStatement(SortStatement statement, QueryIndexSerializerContext<TDocument, TIndex> argument)
        {
            return argument;
        }
    }



    public struct QueryIndexSerializerContext<TDocument, TIndex> where TDocument : class where TIndex : class, IIndex
    {
        public QueryIndexContext<TDocument, TIndex> Context { get; set; }
        public StringBuilder StringBuilder { get; set; }
        public bool SerializedDefaultFilter { get; set; }
    }
}

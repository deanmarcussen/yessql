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
    public class ExpressionValueVisitor : IValueVisitor<ValueArgument, Expression>
    {
        public Expression VisitValue(SearchValue value, ValueArgument argument)
        {
            if (argument.Type == typeof(string))
            {
                return Expression.Constant(value.Value);
            }
            else
            {
                try
                {
                    var converted = Convert.ChangeType(value.Value, argument.Type);
                    return Expression.Constant(converted);
                }
                catch
                {
                    return Expression.Default(argument.Type);
                }
            }
        }
    }
 
    public struct ValueArgument
    {
        public Type Type { get; set; }
    }
}

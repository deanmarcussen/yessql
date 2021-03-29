using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YesSql.Indexes;

namespace YesSql.Core.QueryParser
{
    // Not a node. Can actually become a general helper.
    public class TermList<T> where T : class
    {
        public TermList(List<TermNode<T>> terms)
        {
            Terms = terms;
        }

        public List<TermNode<T>> Terms { get; }

        public IQuery<T> Build(IQuery<T> query)
        {
            foreach (var term in Terms)
            {
                query = term.Build(query).Invoke(query);
            }

            return query;
        }

        public string ToNormalizedString()
            => $"{String.Join(" ", Terms.Select(s => s.ToNormalizedString()))}";

        public override string ToString()
            => $"{String.Join(" ", Terms.Select(s => s.ToString()))}";
    }




    public abstract class QueryNode<T> where T : class
    {
        public abstract Func<IQuery<T>, IQuery<T>> Build(IQuery<T> query);

        public abstract string ToNormalizedString();
    }

    public class TermNode<T> : QueryNode<T> where T : class
    {
        public TermNode(string value, OperatorNode<T> operation)
        {
            Value = value;
            Operation = operation;
        }

        public string Value { get; }
        public OperatorNode<T> Operation { get; }

        public override Func<IQuery<T>, IQuery<T>> Build(IQuery<T> query)
        {
            return Operation.Build(query);
        }

        public override string ToNormalizedString()
            => $"{Value}:{Operation.ToNormalizedString()}";

        public override string ToString()
            => $"{Value}:{Operation.ToString()}";
    }

    public abstract class OperatorNode<T> : QueryNode<T> where T : class
    {
    }

    public class UnaryNode<T> : OperatorNode<T> where T : class
    {
        public UnaryNode(string value, Func<IQuery<T>, string, IQuery<T>> query)
        {
            Value = value;
            Query = query;
        }

        public string Value { get; }
        public Func<IQuery<T>, string, IQuery<T>> Query { get; }

        public override Func<IQuery<T>, IQuery<T>> Build(IQuery<T> query)
        {
            return result => Query(query, Value);
        }

        public override string ToNormalizedString()
            => ToString();

        public override string ToString()
            => $"{Value.ToString()}";
    }

    public class NotUnaryNode<T> : OperatorNode<T> where T : class
    {
        public NotUnaryNode(string operatorValue, UnaryNode<T> operation)
        {
            OperatorValue = operatorValue;
            Operation = operation;
        }

        public string OperatorValue { get; }
        public UnaryNode<T> Operation { get; }

        public override Func<IQuery<T>, IQuery<T>> Build(IQuery<T> query)
        {
            return result => query.All(
                Operation.Build(query)
            );
        }

        public override string ToNormalizedString()
            => ToString();

        public override string ToString()
            => $"{OperatorValue} {Operation.ToString()}";
    }

    public class OrNode<T> : OperatorNode<T> where T : class
    {
        public OrNode(OperatorNode<T> left, OperatorNode<T> right, string value)
        {
            Left = left;
            Right = right;
            Value = value;
        }

        public OperatorNode<T> Left { get; }
        public OperatorNode<T> Right { get; }
        public string Value { get; }

        public override Func<IQuery<T>, IQuery<T>> Build(IQuery<T> query)
        {
            return result => query.Any(
                Left.Build(query),
                Right.Build(query)
            );
        }

        public override string ToNormalizedString()
            => $"({Left.ToNormalizedString()} OR {Right.ToNormalizedString()})";

        public override string ToString()
            => $"{Left.ToString()} {Value} {Right.ToString()}";
    }

    public class AndNode<T> : OperatorNode<T> where T : class
    {
        public AndNode(OperatorNode<T> left, OperatorNode<T> right, string value)
        {
            Left = left;
            Right = right;
            Value = value;
        }

        public OperatorNode<T> Left { get; }
        public OperatorNode<T> Right { get; }
        public string Value { get; }

        public override Func<IQuery<T>, IQuery<T>> Build(IQuery<T> query)
        {
            return result => query.All(
                Left.Build(query),
                Right.Build(query)
            );
        }

        public override string ToNormalizedString()
            => $"({Left.ToNormalizedString()} AND {Right.ToNormalizedString()})";

        public override string ToString()
            => $"{Left.ToString()} {Value} {Right.ToString()}";
    }

    public class NotNode<T> : AndNode<T> where T : class
    {
        public NotNode(OperatorNode<T> left, OperatorNode<T> right, string value) : base(left, right, value)
        {
        }

        public override string ToNormalizedString()
            => $"({Left.ToNormalizedString()} NOT {Right.ToNormalizedString()})";

        public override string ToString()
            => $"{Left.ToString()} {Value} {Right.ToString()}";
    }


    /*
        public class BinaryNode<T> : OperatorNode<T> where T : class
        {
            public BinaryNode(){}

            public List<UnaryNode<T>> All { get; set; } = new List<UnaryNode<T>>();
            public List<UnaryNode<T>> Any { get; } = new List<UnaryNode<T>>();

            public Func<IQuery<T>, IQuery<T>> Build(IQuery<T> query)
            {
                var funcs = new List<Func<IQuery<T>, IQuery<T>>>();
                if (All.Count == 1 && Any.Count == 0)
                {
                    funcs.Add(All[0].Build(query)); 
                }
                else if (All.Count > 0)
                {
                    Func<IQuery<T>, IQuery<T>> f = (query) =>  query.All(
                            All.Select(a => a.Build(query)).ToArray()
                        );

                    funcs.Add(f);
                }

                if (Any.Count == 1 && All.Count == 0)
                {
                    funcs.Add(Any[0].Build(query)); 
                }
                else if (Any.Count > 0)
                {
                    Func<IQuery<T>, IQuery<T>> f = (query) => 
                        query.Any(
                            All.Select(a => a.Build(query)).ToArray()
                        );

                    funcs.Add(f);
                }            

                Func<IQuery<T>, IQuery<T>> finalFunc = (query) =>
                {   
                    foreach(var func in funcs)
                    {
                        func.Invoke(query);
                    };

                    return query;
                };

                return finalFunc;



            }
        }
            */
}
using Parlot;
using Parlot.Fluent;
using System;
using static Parlot.Fluent.Parsers;

namespace YesSql.Core.QueryParser
{
    public abstract class TermParser<T> : Parser<TermNode<T>> where T : class
    {

        public TermParser(string name)
        {
            Name = name;
        }

        public string Name { get; }
        public bool OneOrMany { get; }

        public BooleanTermQueryOption<T> TermQueryOption { get; internal set; }


        public Parser<TermNode<T>> Parser { get; internal set; }
        public Parser<char> SeperatorParser { get; internal set; }

        public override bool Parse(ParseContext context, ref ParseResult<TermNode<T>> result)
        {
            context.EnterParser(this);
            var ctx = (QueryParseContext<T>)context;

            if (ctx.TermOptions.TryGetValue(Name, out var current))
            {
                ctx.CurrentTermOption = current;
            }
            try
            {
                return Parser.Parse(context, ref result);
            }
            finally
            {
                ctx.CurrentTermOption = null;
            }
        }
    }

    // Options here for One, or Many

    public class NamedTermParser<T> : TermParser<T> where T : class
    {
        public NamedTermParser(string name, OperatorParser<T> operatorParser) : base(name)
        {
            SeperatorParser = Terms.Text(name, caseInsensitive: true)
                .SkipAnd(Literals.Char(':'));

            var parser = Terms.Text(name, caseInsensitive: true)
                .AndSkip(Literals.Char(':'))
                .And(operatorParser)
                    .Then<TermNode<T>>(static x => new NamedTermNode<T>(x.Item1, x.Item2));

            Parser = parser;

            TermQueryOption = operatorParser.TermQueryOption;
        }
    }

    public class DefaultTermParser<T> : TermParser<T> where T : class
    {
        public DefaultTermParser(string name, OperatorParser<T> operatorParser) : base(name)
        {

            SeperatorParser = Terms.Text(name, caseInsensitive: true).SkipAnd(Literals.Char(':'))
                .Or(
                    Literals.Char(' ').AndSkip(Literals.WhiteSpace()) // a default term is also seperated by a space
                );

            var termParser = Terms.Text(name, caseInsensitive: true)
                .AndSkip(Literals.Char(':'))
                .And(operatorParser)
                    .Then<TermNode<T>>(static x => new NamedTermNode<T>(x.Item1, x.Item2));

            var defaultParser = operatorParser.Then<TermNode<T>>(x => new DefaultTermNode<T>(name, x));

            var parser = termParser.Or(defaultParser);

            Parser = parser;

            TermQueryOption = operatorParser.TermQueryOption;
        }
    }
}
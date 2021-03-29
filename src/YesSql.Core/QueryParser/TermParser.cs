using Parlot;
using Parlot.Fluent;
using static Parlot.Fluent.Parsers;

namespace YesSql.Core.QueryParser
{
    public class TermParser<T> : Parser<TermNode<T>> where T: class
    {
        public readonly string _name;

        public TermParser(string name, OperatorParser<T> operatorParser)
        {
            _name = name;

            SeperatorParser = Terms.Text(name, caseInsensitive: true)
                .SkipAnd(Literals.Char(':'));

            Parser = Terms.Text(name, caseInsensitive: true)
                .AndSkip(Literals.Char(':'))
                .And(operatorParser)
                .Then(static x => new TermNode<T>(x.Item1, x.Item2));
        }

        public Parser<TermNode<T>> Parser { get; private set; }
        public Parser<char> SeperatorParser { get; private set; }

        
        public override bool Parse(ParseContext context, ref ParseResult<TermNode<T>> result)
        {
            context.EnterParser(this);

            return Parser.Parse(context, ref result);
        }
    }
}
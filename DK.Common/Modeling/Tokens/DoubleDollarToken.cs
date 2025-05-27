using DK.Code;
using DK.Syntax;

namespace DK.Modeling.Tokens
{
    internal class DoubleDollarToken : Token
    {
        internal DoubleDollarToken(Scope scope, CodeSpan span)
            : base(scope, span)
        {
            ClassifierType = ProbeClassifierType.Delimiter;
        }

        public override string Text => "$$";
    }
}

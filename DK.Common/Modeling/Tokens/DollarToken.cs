using DK.Code;
using DK.Syntax;

namespace DK.Modeling.Tokens
{
    internal class DollarToken : Token
    {
        internal DollarToken(Scope scope, CodeSpan span)
            : base(scope, span)
        {
            ClassifierType = ProbeClassifierType.Delimiter;
        }

        public override string Text => "$";
    }
}

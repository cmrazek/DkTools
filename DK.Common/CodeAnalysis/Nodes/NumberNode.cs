using DK.Code;
using DK.CodeAnalysis.Statements;
using DK.CodeAnalysis.Values;
using DK.Modeling;
using System;

namespace DK.CodeAnalysis.Nodes
{
    class NumberNode : TextNode
    {
        private decimal _value;

        public NumberNode(Statement stmt, CodeSpan span, string text)
            : base(stmt, DetectDataTypeFromNumberLiteral(text), span, text)
        {
            if (!decimal.TryParse(text, out _value))
            {
#if DEBUG
                throw new InvalidOperationException(string.Format("Unable to parse number '{0}'.", text));
#endif
            }
        }

        public override void Execute(CAScope scope) { }
        public override bool IsReportable => true;
        public override Value ReadValue(CAScope scope) => new NumberValue(DataType, _value, literal: true);
        public override string ToString() => _value.ToString();

        private static DataType DetectDataTypeFromNumberLiteral(string text)
        {
            bool gotDot = false;
            int scale = 0;
            int precision = 0;
            bool signed = false;

            foreach (var ch in text)
            {
                if (char.IsDigit(ch))
                {
                    scale++;
                    if (gotDot) precision++;
                }
                else if (ch == '-')
                {
                    signed = true;
                }
                else if (ch == '.')
                {
                    gotDot = true;
                }
            }

            return DataType.MakeNumeric(scale, precision, signed, name: null,
                new Syntax.ProbeClassifiedString(Syntax.ProbeClassifierType.Number, text));
        }
    }
}

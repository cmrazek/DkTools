using DK.Code;

namespace DK.CodeAnalysis.Values
{
    internal class InterfaceValue : Value
    {
        public InterfaceValue()
            : base(Modeling.DataType.InterfaceType, literal: false)
        { }

        public override string ToString() => "interfacetype";

        public override Value CloneNonLiteral() => this;

        public override string ToStringValue(CAScope scope, CodeSpan span) => null;

        public override decimal? ToNumber(CAScope scope, CodeSpan span) => null;

        public override DkDate? ToDate(CAScope scope, CodeSpan span) => null;

        public override DkTime? ToTime(CAScope scope, CodeSpan span) => null;

        public override char? ToChar(CAScope scope, CodeSpan span) => null;

        public override Value Convert(CAScope scope, CodeSpan span, Value value) => new InterfaceValue();

        public override bool IsEqualTo(Value other) => false;
    }
}

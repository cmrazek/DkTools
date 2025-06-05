using DK.Code;

namespace DK.CodeAnalysis.Values
{
    internal class VariantValue : Value
    {
        public VariantValue()
            : base(Modeling.DataType.Variant, literal: false)
        { }

        public override string ToString() => "variant";

        public override Value CloneNonLiteral() => this;

        public override string ToStringValue(CAScope scope, CodeSpan span) => null;

        public override decimal? ToNumber(CAScope scope, CodeSpan span) => null;

        public override DkDate? ToDate(CAScope scope, CodeSpan span) => null;

        public override DkTime? ToTime(CAScope scope, CodeSpan span) => null;

        public override char? ToChar(CAScope scope, CodeSpan span) => null;

        public override Value Convert(CAScope scope, CodeSpan span, Value value) => new VariantValue();

        public override bool IsEqualTo(Value other) => false;
    }
}

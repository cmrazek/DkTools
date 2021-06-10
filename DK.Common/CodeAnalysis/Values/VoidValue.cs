using DK.Code;
using DK.Modeling;

namespace DK.CodeAnalysis.Values
{
	class VoidValue : Value
	{
		public VoidValue()
			: base(DataType.Void)
		{
		}

		public override string ToString() => "void";

		public override string ToStringValue(CAScope scope, CodeSpan span)
		{
			return null;
		}

		public override decimal? ToNumber(CAScope scope, CodeSpan span)
		{
			return null;
		}

		public override DkDate? ToDate(CAScope scope, CodeSpan span)
		{
			return null;
		}

		public override DkTime? ToTime(CAScope scope, CodeSpan span)
		{
			return null;
		}

		public override char? ToChar(CAScope scope, CodeSpan span)
		{
			return null;
		}

		public override Value Convert(CAScope scope, CodeSpan span, Value value)
		{
			return new VoidValue();
		}

		public override bool IsEqualTo(Value other)
		{
			return false;
		}
	}
}

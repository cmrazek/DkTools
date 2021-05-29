using DK.Code;
using DK.Modeling;

namespace DK.CodeAnalysis.Values
{
	class IndRelValue : Value
	{
		private string _indRelName;

		public IndRelValue(DataType dataType, string indRelName)
			: base(dataType)
		{
			_indRelName = indRelName;
		}

		public override string ToString() => _indRelName;

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
			if (_indRelName == null) return false;
			var o = other as IndRelValue;
			if (o == null || o._indRelName == null) return false;
			return _indRelName == o._indRelName;
		}
	}
}

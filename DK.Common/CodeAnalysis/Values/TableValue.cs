using DK.Code;
using DK.Modeling;

namespace DK.CodeAnalysis.Values
{
	class TableValue : Value
	{
		private string _tableName;

		public TableValue(DataType dataType, string tableName)
			: base(dataType)
		{
			_tableName = tableName;
		}

		public override string ToString() => _tableName;

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
			if (_tableName == null) return false;
			var o = other as TableValue;
			if (o == null || o._tableName == null) return false;
			return _tableName == o._tableName;
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel;

namespace DkTools.CodeAnalysis.Values
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

		public override string ToStringValue(RunScope scope, Span span)
		{
			return null;
		}

		public override decimal? ToNumber(RunScope scope, Span span)
		{
			return null;
		}

		public override DkDate? ToDate(RunScope scope, Span span)
		{
			return null;
		}

		public override DkTime? ToTime(RunScope scope, Span span)
		{
			return null;
		}

		public override char? ToChar(RunScope scope, Span span)
		{
			return null;
		}

		public override Value Convert(RunScope scope, Span span, Value value)
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

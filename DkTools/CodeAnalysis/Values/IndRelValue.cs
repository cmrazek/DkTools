using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel;

namespace DkTools.CodeAnalysis.Values
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

		public override string ToStringValue(CAScope scope, Span span)
		{
			return null;
		}

		public override decimal? ToNumber(CAScope scope, Span span)
		{
			return null;
		}

		public override DkDate? ToDate(CAScope scope, Span span)
		{
			return null;
		}

		public override DkTime? ToTime(CAScope scope, Span span)
		{
			return null;
		}

		public override char? ToChar(CAScope scope, Span span)
		{
			return null;
		}

		public override Value Convert(CAScope scope, Span span, Value value)
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

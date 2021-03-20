using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel;

namespace DkTools.CodeAnalysis.Values
{
	class VoidValue : Value
	{
		public VoidValue()
			: base(DataType.Void)
		{
		}

		public override string ToString() => "void";

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
			return false;
		}
	}
}

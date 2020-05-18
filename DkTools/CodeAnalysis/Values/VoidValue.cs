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
			return false;
		}
	}
}

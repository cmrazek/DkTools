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
	}
}

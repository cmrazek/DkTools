using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.ErrorTagging
{
	enum ErrorType
	{
		Error,
		Warning
	}

	static class ErrorTypeEx
	{
		public static ErrorType? Combine(this ErrorType? a, ErrorType? b)
		{
			if (!a.HasValue && !b.HasValue) return null;
			if (a.HasValue && !b.HasValue) return a.Value;
			if (!a.HasValue && b.HasValue) return b.Value;
			if (a.Value == ErrorType.Error || b.Value == ErrorType.Error) return ErrorType.Error;
			return ErrorType.Warning;
		}
	}
}

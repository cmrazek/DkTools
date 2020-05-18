using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DkTools.CodeModel;

namespace DkTools.CodeAnalysis.Values
{
	class StringValue : Value
	{
		private string _value;

		public StringValue(DataType dataType, string value)
			: base(dataType)
		{
			_value = value;
		}

		public override string ToString() => $"\"{_value}\"";

		public override Value CompareEqual(RunScope scope, Span span, Value rightValue)
		{
			if (_value != null)
			{
				var right = rightValue.ToStringValue(scope, span);
				if (right != null)
				{
					return new NumberValue(DataType.Int, string.Compare(_value, right, StringComparison.OrdinalIgnoreCase) == 0 ? 1 : 0);
				}
			}

			return new NumberValue(DataType.Int, null);
		}

		public override Value CompareNotEqual(RunScope scope, Span span, Value rightValue)
		{
			if (_value != null)
			{
				var right = rightValue.ToStringValue(scope, span);
				if (right != null)
				{
					return new NumberValue(DataType.Int, string.Compare(_value, right, StringComparison.OrdinalIgnoreCase) != 0 ? 1 : 0);
				}
			}

			return new NumberValue(DataType.Int, null);
		}

		public override Value CompareLessThan(RunScope scope, Span span, Value rightValue)
		{
			if (_value != null)
			{
				var right = rightValue.ToStringValue(scope, span);
				if (right != null)
				{
					return new NumberValue(DataType.Int, string.Compare(_value, right, StringComparison.OrdinalIgnoreCase) < 0 ? 1 : 0);
				}
			}

			return new NumberValue(DataType.Int, null);
		}

		public override Value CompareGreaterThan(RunScope scope, Span span, Value rightValue)
		{
			if (_value != null)
			{
				var right = rightValue.ToStringValue(scope, span);
				if (right != null)
				{
					return new NumberValue(DataType.Int, string.Compare(_value, right, StringComparison.OrdinalIgnoreCase) > 0 ? 1 : 0);
				}
			}

			return new NumberValue(DataType.Int, null);
		}

		public override Value CompareLessEqual(RunScope scope, Span span, Value rightValue)
		{
			if (_value != null)
			{
				var right = rightValue.ToStringValue(scope, span);
				if (right != null)
				{
					return new NumberValue(DataType.Int, string.Compare(_value, right, StringComparison.OrdinalIgnoreCase) <= 0 ? 1 : 0);
				}
			}

			return new NumberValue(DataType.Int, null);
		}

		public override Value CompareGreaterEqual(RunScope scope, Span span, Value rightValue)
		{
			if (_value != null)
			{
				var right = rightValue.ToStringValue(scope, span);
				if (right != null)
				{
					return new NumberValue(DataType.Int, string.Compare(_value, right, StringComparison.OrdinalIgnoreCase) >= 0 ? 1 : 0);
				}
			}

			return new NumberValue(DataType.Int, null);
		}

		public override string ToStringValue(RunScope scope, Span span)
		{
			return _value;
		}

		public override decimal? ToNumber(RunScope scope, Span span)
		{
			if (_value != null)
			{
				decimal val;
				if (decimal.TryParse(_value, out val)) return val;
			}

			return null;
		}

		public override DkDate? ToDate(RunScope scope, Span span)
		{
			if (_value != null)
			{
				DateTime dt;
				if (DateTime.TryParse(_value, out dt)) return new DkDate(dt.Year, dt.Month, dt.Day);
			}

			return null;
		}

		private static readonly Regex _rxTime = new Regex(@"^\s*(\d{1,2})\:(\d{1,2})(?:\:(\d{1,2}))?\s*$");

		public override DkTime? ToTime(RunScope scope, Span span)
		{
			if (_value != null)
			{
				var match = _rxTime.Match(_value);
				if (match.Success)
				{
					int hour, minute, second;
					int.TryParse(match.Groups[1].Value, out hour);
					int.TryParse(match.Groups[2].Value, out minute);
					int.TryParse(match.Groups.Count > 3 ? match.Groups[3].Value : "0", out second);
					return new DkTime(hour, minute, second);
				}
			}

			return null;
		}

		public override char? ToChar(RunScope scope, Span span)
		{
			scope.CodeAnalyzer.ReportError(span, CAError.CA0055, "string", "char");	// Converting {0} to {1}.

			if (_value != null && _value.Length == 1)
			{
				return _value[0];
			}

			return null;
		}

		public override Value Convert(RunScope scope, Span span, Value value)
		{
			return new StringValue(DataType, value.ToStringValue(scope, span));
		}
	}
}

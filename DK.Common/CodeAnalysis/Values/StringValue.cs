using DK;
using DK.Code;
using DK.Modeling;
using System;
using System.Text.RegularExpressions;

namespace DK.CodeAnalysis.Values
{
	class StringValue : Value
	{
		private string _value;

		public StringValue(DataType dataType, string value, bool literal)
			: base(dataType, literal)
		{
			_value = value;
		}

		public override string ToString() => $"\"{_value}\"";

		public override Value CloneNonLiteral() => IsLiteral ? new StringValue(DataType, _value, literal: false) : this;

        public override Value CompareEqual(CAScope scope, CodeSpan span, Value rightValue)
		{
			if (_value != null)
			{
				var right = rightValue.ToStringValue(scope, span);
				if (right != null)
				{
					return new NumberValue(DataType.Int, string.Compare(_value, right, StringComparison.OrdinalIgnoreCase) == 0 ? 1 : 0,
						literal: false);
				}
			}

			return new NumberValue(DataType.Int, number: null, literal: false);
		}

		public override Value CompareNotEqual(CAScope scope, CodeSpan span, Value rightValue)
		{
			if (_value != null)
			{
				var right = rightValue.ToStringValue(scope, span);
				if (right != null)
				{
					return new NumberValue(DataType.Int, string.Compare(_value, right, StringComparison.OrdinalIgnoreCase) != 0 ? 1 : 0,
						literal: false);
				}
			}

			return new NumberValue(DataType.Int, number: null, literal: false);
		}

		public override Value CompareLessThan(CAScope scope, CodeSpan span, Value rightValue)
		{
			if (_value != null)
			{
				var right = rightValue.ToStringValue(scope, span);
				if (right != null)
				{
					return new NumberValue(DataType.Int, string.Compare(_value, right, StringComparison.OrdinalIgnoreCase) < 0 ? 1 : 0,
						literal: false);
				}
			}

			return new NumberValue(DataType.Int, number: null, literal: false);
		}

		public override Value CompareGreaterThan(CAScope scope, CodeSpan span, Value rightValue)
		{
			if (_value != null)
			{
				var right = rightValue.ToStringValue(scope, span);
				if (right != null)
				{
					return new NumberValue(DataType.Int, string.Compare(_value, right, StringComparison.OrdinalIgnoreCase) > 0 ? 1 : 0,
						literal: false);
				}
			}

			return new NumberValue(DataType.Int, number: null, literal: false);
		}

		public override Value CompareLessEqual(CAScope scope, CodeSpan span, Value rightValue)
		{
			if (_value != null)
			{
				var right = rightValue.ToStringValue(scope, span);
				if (right != null)
				{
					return new NumberValue(DataType.Int, string.Compare(_value, right, StringComparison.OrdinalIgnoreCase) <= 0 ? 1 : 0,
						literal: false);
				}
			}

			return new NumberValue(DataType.Int, number: null, literal: false);
		}

		public override Value CompareGreaterEqual(CAScope scope, CodeSpan span, Value rightValue)
		{
			if (_value != null)
			{
				var right = rightValue.ToStringValue(scope, span);
				if (right != null)
				{
					return new NumberValue(DataType.Int, string.Compare(_value, right, StringComparison.OrdinalIgnoreCase) >= 0 ? 1 : 0,
						literal: false);
				}
			}

			return new NumberValue(DataType.Int, number: null, literal: false);
		}

        public override Value CompareLike(CAScope scope, CodeSpan span, Value rightValue)
        {
            if (rightValue.DataType.ValueType != ValType.String)
			{
				scope.CodeAnalyzer.ReportError(span, CAError.CA10140);   // 'like' operator may only be used with a string.
            }

			return new NumberValue(DataType.Int, number: null, literal: false);
        }

        public override string ToStringValue(CAScope scope, CodeSpan span)
		{
			return _value;
		}

		public override decimal? ToNumber(CAScope scope, CodeSpan span)
		{
			if (_value != null)
			{
				decimal val;
				if (decimal.TryParse(_value, out val)) return val;
			}

			return null;
		}

		public override DkDate? ToDate(CAScope scope, CodeSpan span)
		{
			if (_value != null)
			{
				DateTime dt;
				if (DateTime.TryParse(_value, out dt)) return new DkDate(dt.Year, dt.Month, dt.Day);
			}

			return null;
		}

		private static readonly Regex _rxTime = new Regex(@"^\s*(\d{1,2})\:(\d{1,2})(?:\:(\d{1,2}))?\s*$");

		public override DkTime? ToTime(CAScope scope, CodeSpan span)
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

		public override char? ToChar(CAScope scope, CodeSpan span)
		{
			scope.CodeAnalyzer.ReportError(span, CAError.CA10055, "string", "char");	// Converting {0} to {1}.

			if (_value != null && _value.Length == 1)
			{
				return _value[0];
			}

			return null;
		}

		public override Value Convert(CAScope scope, CodeSpan span, Value value)
		{
			return new StringValue(DataType, value.ToStringValue(scope, span), value.IsLiteral);
		}

		public override bool IsEqualTo(Value other)
		{
			if (_value == null) return false;
			var o = other as StringValue;
			if (o == null || o._value == null) return false;
			return _value == o._value;
		}

		public override void CheckTypeConversion(CAScope scope, CodeSpan span, DataType dataType, ConversionMethod method)
		{
			if (_value != null && dataType.HasEnumOptions)
			{
				if (!dataType.IsValidEnumOption(_value, strict: true))
				{
					if (_value.Length == 0 && dataType.IsValidEnumOption(" "))
					{
						scope.CodeAnalyzer.ReportError(span, CAError.CA10060, "\"\"");   // Enum option {0} does not exist; use a single space instead of a blank string.
					}
					else
					{
						scope.CodeAnalyzer.ReportError(span, CAError.CA10059, CodeParser.StringToStringLiteral(_value));   // Enum option {0} does not exist.
					}
				}
				//else if (_value.IsWord())
				//{
				//	scope.CodeAnalyzer.ReportError(span, CAError.CA0058);   // Use non-string enum values when possible.
				//}
			}
		}
	}
}

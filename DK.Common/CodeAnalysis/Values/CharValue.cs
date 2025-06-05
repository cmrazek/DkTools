using DK.Code;
using DK.Modeling;

namespace DK.CodeAnalysis.Values
{
	class CharValue : Value
	{
		private char? _char;

		public CharValue(DataType dataType, char? value, bool literal)
			: base(dataType, literal)
		{
			_char = value;
		}

		public override string ToString() => $"'{_char}'";

		public override Value CloneNonLiteral() => IsLiteral ? new CharValue(DataType, _char, literal: false) : this;

        public override string ToStringValue(CAScope scope, CodeSpan span)
		{
			if (_char != null) return _char.Value.ToString();
			return null;
		}

		public override decimal? ToNumber(CAScope scope, CodeSpan span)
		{
			if (_char != null) return (decimal)((int)_char.Value);
			return null;
		}

		public override char? ToChar(CAScope scope, CodeSpan span)
		{
			return _char;
		}

		public override DkDate? ToDate(CAScope scope, CodeSpan span)
		{
			scope.CodeAnalyzer.ReportError(span, CAError.CA10055, "char", "date");	// Converting {0} to {1}.
			if (_char.HasValue) return new DkDate((int)_char.Value);
			return null;
		}

		public override DkTime? ToTime(CAScope scope, CodeSpan span)
		{
			scope.CodeAnalyzer.ReportError(span, CAError.CA10055, "char", "time");	// Converting {0} to {1}.
			if (_char.HasValue) return new DkTime((int)_char.Value);
			return null;
		}

		public override Value Multiply(CAScope scope, CodeSpan span, Value rightValue)
		{
			if (_char.HasValue)
			{
				var right = rightValue.ToChar(scope, span);
				if (right.HasValue)
				{
					var result = (int)_char.Value * (int)right.Value;
					if (result < 0 || result > 65535)
					{
						scope.CodeAnalyzer.ReportError(span, CAError.CA10056);	// Char math results in an out-of-bounds value.
						return new CharValue(DataType, value: null, literal: false);
					}

					return new CharValue(DataType, (char)result, literal: false);
				}
			}

			return new CharValue(DataType, value: null, literal: false);
		}

		public override Value Divide(CAScope scope, CodeSpan span, Value rightValue)
		{
			if (_char.HasValue)
			{
				var right = rightValue.ToChar(scope, span);
				if (right.HasValue)
				{
					var rightNum = (int)right.Value;
					if (rightNum == 0)
					{
						scope.CodeAnalyzer.ReportError(span, CAError.CA10051);	// Division by zero.
						return new CharValue(DataType, value: null, literal: false);
					}

					var result = (int)_char.Value / rightNum;
					if (result < 0 || result > 65535)
					{
						scope.CodeAnalyzer.ReportError(span, CAError.CA10056);	// Char math results in an out-of-bounds value.
						return new CharValue(DataType, value: null, literal: false);
					}

					return new CharValue(DataType, (char)result, literal: false);
				}
			}

			return new CharValue(DataType, value: null, literal: false);
		}

		public override Value ModulusDivide(CAScope scope, CodeSpan span, Value rightValue)
		{
			if (_char.HasValue)
			{
				var right = rightValue.ToChar(scope, span);
				if (right.HasValue)
				{
					var rightNum = (int)right.Value;
					if (rightNum == 0)
					{
						scope.CodeAnalyzer.ReportError(span, CAError.CA10051);	// Division by zero.
						return new CharValue(DataType, value: null, literal: false);
					}

					var result = (int)_char.Value % rightNum;
					if (result < 0 || result > 65535)
					{
						scope.CodeAnalyzer.ReportError(span, CAError.CA10056);	// Char math results in an out-of-bounds value.
						return new CharValue(DataType, value: null, literal: false);
					}

					return new CharValue(DataType, (char)result, literal: false);
				}
			}

			return new CharValue(DataType, value: null, literal: false);
		}

		public override Value Add(CAScope scope, CodeSpan span, Value rightValue)
		{
			if (_char.HasValue)
			{
				var right = rightValue.ToChar(scope, span);
				if (right.HasValue)
				{
					var result = (int)_char.Value + (int)right.Value;
					if (result < 0 || result > 65535)
					{
						scope.CodeAnalyzer.ReportError(span, CAError.CA10056);	// Char math results in an out-of-bounds value.
						return new CharValue(DataType, value: null, literal: false);
					}

					return new CharValue(DataType, (char)result, literal: false);
				}
			}

			return new CharValue(DataType, value: null, literal: false);
		}

		public override Value Subtract(CAScope scope, CodeSpan span, Value rightValue)
		{
			if (_char.HasValue)
			{
				var right = rightValue.ToChar(scope, span);
				if (right.HasValue)
				{
					var result = (int)_char.Value - (int)right.Value;
					if (result < 0 || result > 65535)
					{
						scope.CodeAnalyzer.ReportError(span, CAError.CA10056);	// Char math results in an out-of-bounds value.
						return new CharValue(DataType, value: null, literal: false);
					}

					return new CharValue(DataType, (char)result, literal: false);
				}
			}

			return new CharValue(DataType, value: null, literal: false);
		}

		public override Value Invert(CAScope scope, CodeSpan span)
		{
			scope.CodeAnalyzer.ReportError(span, CAError.CA10056);	// Char math results in an out-of-bounds value.
			return new CharValue(DataType, value: null, literal: false);
		}

		public override Value CompareEqual(CAScope scope, CodeSpan span, Value rightValue)
		{
			if (_char.HasValue)
			{
				var right = rightValue.ToChar(scope, span);
				if (right.HasValue)
				{
					return new NumberValue(DataType.Int, _char.Value == right.Value ? 1 : 0, literal: false);
				}
			}

			return new NumberValue(DataType.Int, number: null, literal: false);
		}

		public override Value CompareNotEqual(CAScope scope, CodeSpan span, Value rightValue)
		{
			if (_char.HasValue)
			{
				var right = rightValue.ToChar(scope, span);
				if (right.HasValue)
				{
					return new NumberValue(DataType.Int, _char.Value != right.Value ? 1 : 0, literal: false);
				}
			}

			return new NumberValue(DataType.Int, number: null, literal: false);
		}

		public override Value CompareLessThan(CAScope scope, CodeSpan span, Value rightValue)
		{
			if (_char.HasValue)
			{
				var right = rightValue.ToChar(scope, span);
				if (right.HasValue)
				{
					return new NumberValue(DataType.Int, _char.Value < right.Value ? 1 : 0, literal: false);
				}
			}

			return new NumberValue(DataType.Int, number: null, literal: false);
		}

		public override Value CompareGreaterThan(CAScope scope, CodeSpan span, Value rightValue)
		{
			if (_char.HasValue)
			{
				var right = rightValue.ToChar(scope, span);
				if (right.HasValue)
				{
					return new NumberValue(DataType.Int, _char.Value > right.Value ? 1 : 0, literal: false);
				}
			}

			return new NumberValue(DataType.Int, number: null, literal: false);
		}

		public override Value CompareLessEqual(CAScope scope, CodeSpan span, Value rightValue)
		{
			if (_char.HasValue)
			{
				var right = rightValue.ToChar(scope, span);
				if (right.HasValue)
				{
					return new NumberValue(DataType.Int, _char.Value <= right.Value ? 1 : 0, literal: false);
				}
			}

			return new NumberValue(DataType.Int, number: null, literal: false);
		}

		public override Value CompareGreaterEqual(CAScope scope, CodeSpan span, Value rightValue)
		{
			if (_char.HasValue)
			{
				var right = rightValue.ToChar(scope, span);
				if (right.HasValue)
				{
					return new NumberValue(DataType.Int, _char.Value >= right.Value ? 1 : 0, literal: false);
				}
			}

			return new NumberValue(DataType.Int, number: null, literal: false);
		}

		public override bool IsTrue
		{
			get
			{
				if (_char.HasValue) return _char != 0;
				return false;
			}
		}

		public override bool IsFalse
		{
			get
			{
				if (_char.HasValue) return _char == 0;
				return false;
			}
		}

		public override Value Convert(CAScope scope, CodeSpan span, Value value)
		{
			var str = value.ToStringValue(scope, span);
			if (str != null && str.Length == 1) return new CharValue(DataType, str[0], value.IsLiteral);
			return new CharValue(DataType, value: null, literal: false);
		}

		public override bool IsEqualTo(Value other)
		{
			if (!_char.HasValue) return false;
			var o = other as CharValue;
			if (o == null || !o._char.HasValue) return false;
			return _char.Value == o._char.Value;
		}
	}
}

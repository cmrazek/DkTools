using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel;

namespace DkTools.CodeAnalysis.Values
{
	class CharValue : Value
	{
		private char? _char;

		public CharValue(DataType dataType, char? value)
			: base(dataType)
		{
			_char = value;
		}

		public override string ToStringValue(RunScope scope, Span span)
		{
			if (_char != null) return _char.Value.ToString();
			return null;
		}

		public override decimal? ToNumber(RunScope scope, Span span)
		{
			if (_char != null) return (decimal)((int)_char.Value);
			return null;
		}

		public override char? ToChar(RunScope scope, Span span)
		{
			return _char;
		}

		public override DkDate? ToDate(RunScope scope, Span span)
		{
			scope.CodeAnalyzer.ReportError(span, CAError.CA0055, "char", "date");	// Converting {0} to {1}.
			if (_char.HasValue) return new DkDate((int)_char.Value);
			return null;
		}

		public override DkTime? ToTime(RunScope scope, Span span)
		{
			scope.CodeAnalyzer.ReportError(span, CAError.CA0055, "char", "time");	// Converting {0} to {1}.
			if (_char.HasValue) return new DkTime((int)_char.Value);
			return null;
		}

		public override Value Multiply(RunScope scope, Span span, Value rightValue)
		{
			if (_char.HasValue)
			{
				var right = rightValue.ToChar(scope, span);
				if (right.HasValue)
				{
					var result = (int)_char.Value * (int)right.Value;
					if (result < 0 || result > 65535)
					{
						scope.CodeAnalyzer.ReportError(span, CAError.CA0056);	// Char math results in an out-of-bounds value.
						return new CharValue(DataType, null);
					}

					return new CharValue(DataType, (char)result);
				}
			}

			return new CharValue(DataType, null);
		}

		public override Value Divide(RunScope scope, Span span, Value rightValue)
		{
			if (_char.HasValue)
			{
				var right = rightValue.ToChar(scope, span);
				if (right.HasValue)
				{
					var rightNum = (int)right.Value;
					if (rightNum == 0)
					{
						scope.CodeAnalyzer.ReportError(span, CAError.CA0051);	// Division by zero.
						return new CharValue(DataType, null);
					}

					var result = (int)_char.Value / rightNum;
					if (result < 0 || result > 65535)
					{
						scope.CodeAnalyzer.ReportError(span, CAError.CA0056);	// Char math results in an out-of-bounds value.
						return new CharValue(DataType, null);
					}

					return new CharValue(DataType, (char)result);
				}
			}

			return new CharValue(DataType, null);
		}

		public override Value ModulusDivide(RunScope scope, Span span, Value rightValue)
		{
			if (_char.HasValue)
			{
				var right = rightValue.ToChar(scope, span);
				if (right.HasValue)
				{
					var rightNum = (int)right.Value;
					if (rightNum == 0)
					{
						scope.CodeAnalyzer.ReportError(span, CAError.CA0051);	// Division by zero.
						return new CharValue(DataType, null);
					}

					var result = (int)_char.Value % rightNum;
					if (result < 0 || result > 65535)
					{
						scope.CodeAnalyzer.ReportError(span, CAError.CA0056);	// Char math results in an out-of-bounds value.
						return new CharValue(DataType, null);
					}

					return new CharValue(DataType, (char)result);
				}
			}

			return new CharValue(DataType, null);
		}

		public override Value Add(RunScope scope, Span span, Value rightValue)
		{
			if (_char.HasValue)
			{
				var right = rightValue.ToChar(scope, span);
				if (right.HasValue)
				{
					var result = (int)_char.Value + (int)right.Value;
					if (result < 0 || result > 65535)
					{
						scope.CodeAnalyzer.ReportError(span, CAError.CA0056);	// Char math results in an out-of-bounds value.
						return new CharValue(DataType, null);
					}

					return new CharValue(DataType, (char)result);
				}
			}

			return new CharValue(DataType, null);
		}

		public override Value Subtract(RunScope scope, Span span, Value rightValue)
		{
			if (_char.HasValue)
			{
				var right = rightValue.ToChar(scope, span);
				if (right.HasValue)
				{
					var result = (int)_char.Value - (int)right.Value;
					if (result < 0 || result > 65535)
					{
						scope.CodeAnalyzer.ReportError(span, CAError.CA0056);	// Char math results in an out-of-bounds value.
						return new CharValue(DataType, null);
					}

					return new CharValue(DataType, (char)result);
				}
			}

			return new CharValue(DataType, null);
		}

		public override Value Invert(RunScope scope, Span span)
		{
			scope.CodeAnalyzer.ReportError(span, CAError.CA0056);	// Char math results in an out-of-bounds value.
			return new CharValue(DataType, null);
		}

		public override Value CompareEqual(RunScope scope, Span span, Value rightValue)
		{
			if (_char.HasValue)
			{
				var right = rightValue.ToChar(scope, span);
				if (right.HasValue)
				{
					return new NumberValue(DataType.Int, _char.Value == right.Value ? 1 : 0);
				}
			}

			return new NumberValue(DataType.Int, null);
		}

		public override Value CompareNotEqual(RunScope scope, Span span, Value rightValue)
		{
			if (_char.HasValue)
			{
				var right = rightValue.ToChar(scope, span);
				if (right.HasValue)
				{
					return new NumberValue(DataType.Int, _char.Value != right.Value ? 1 : 0);
				}
			}

			return new NumberValue(DataType.Int, null);
		}

		public override Value CompareLessThan(RunScope scope, Span span, Value rightValue)
		{
			if (_char.HasValue)
			{
				var right = rightValue.ToChar(scope, span);
				if (right.HasValue)
				{
					return new NumberValue(DataType.Int, _char.Value < right.Value ? 1 : 0);
				}
			}

			return new NumberValue(DataType.Int, null);
		}

		public override Value CompareGreaterThan(RunScope scope, Span span, Value rightValue)
		{
			if (_char.HasValue)
			{
				var right = rightValue.ToChar(scope, span);
				if (right.HasValue)
				{
					return new NumberValue(DataType.Int, _char.Value > right.Value ? 1 : 0);
				}
			}

			return new NumberValue(DataType.Int, null);
		}

		public override Value CompareLessEqual(RunScope scope, Span span, Value rightValue)
		{
			if (_char.HasValue)
			{
				var right = rightValue.ToChar(scope, span);
				if (right.HasValue)
				{
					return new NumberValue(DataType.Int, _char.Value <= right.Value ? 1 : 0);
				}
			}

			return new NumberValue(DataType.Int, null);
		}

		public override Value CompareGreaterEqual(RunScope scope, Span span, Value rightValue)
		{
			if (_char.HasValue)
			{
				var right = rightValue.ToChar(scope, span);
				if (right.HasValue)
				{
					return new NumberValue(DataType.Int, _char.Value >= right.Value ? 1 : 0);
				}
			}

			return new NumberValue(DataType.Int, null);
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

		public override Value Convert(RunScope scope, Span span, Value value)
		{
			var str = value.ToStringValue(scope, span);
			if (str != null && str.Length == 1) return new CharValue(DataType, str[0]);
			return new CharValue(DataType, null);
		}
	}
}

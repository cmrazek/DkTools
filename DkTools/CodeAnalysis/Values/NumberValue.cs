using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel;

namespace DkTools.CodeAnalysis.Values
{
	class NumberValue : Value
	{
		private decimal? _num;

		public NumberValue(DataType dataType, decimal? number)
			: base(dataType)
		{
			_num = number;
		}

		public override Value Multiply(RunScope scope, Span span, Value rightValue)
		{
			if (_num.HasValue)
			{
				var right = rightValue.ToNumber(scope, span);
				if (right.HasValue) return new NumberValue(DataType, _num.Value * right.Value);
			}

			return new NumberValue(DataType, null);
		}

		public override Value Divide(RunScope scope, Span span, Value rightValue)
		{
			if (_num.HasValue)
			{
				var right = rightValue.ToNumber(scope, span);
				if (right.HasValue)
				{
					if (right.Value == 0)
					{
						scope.CodeAnalyzer.ReportError(span, CAError.CA0051);	// Division by zero.
					}
					else
					{
						return new NumberValue(DataType, _num.Value / right.Value);
					}
				}
			}

			return new NumberValue(DataType, null);
		}

		public override Value ModulusDivide(RunScope scope, Span span, Value rightValue)
		{
			if (_num.HasValue)
			{
				var right = rightValue.ToNumber(scope, span);
				if (right.HasValue)
				{
					if (right.Value == 0)
					{
						scope.CodeAnalyzer.ReportError(span, CAError.CA0051);	// Division by zero.
					}
					else
					{
						return new NumberValue(DataType, _num.Value % right.Value);
					}
				}
			}

			return new NumberValue(DataType, null);
		}

		public override Value Add(RunScope scope, Span span, Value rightValue)
		{
			if (_num.HasValue)
			{
				var right = rightValue.ToNumber(scope, span);
				if (right.HasValue) return new NumberValue(DataType, _num.Value + right.Value);
			}

			return new NumberValue(DataType, null);
		}

		public override Value Subtract(RunScope scope, Span span, Value rightValue)
		{
			if (_num.HasValue)
			{
				var right = rightValue.ToNumber(scope, span);
				if (right.HasValue) return new NumberValue(DataType, _num.Value - right.Value);
			}

			return new NumberValue(DataType, null);
		}

		public override Value Invert(RunScope scope, Span span)
		{
			if (_num.HasValue)
			{
				return new NumberValue(DataType, -_num.Value);
			}

			return new NumberValue(DataType, null);
		}

		public override Value CompareEqual(RunScope scope, Span span, Value rightValue)
		{
			if (_num.HasValue)
			{
				var right = rightValue.ToNumber(scope, span);
				if (right.HasValue) return new NumberValue(DataType.Int, _num.Value == right.Value ? 1 : 0);
			}

			return new NumberValue(DataType.Int, null);
		}

		public override Value CompareNotEqual(RunScope scope, Span span, Value rightValue)
		{
			if (_num.HasValue)
			{
				var right = rightValue.ToNumber(scope, span);
				if (right.HasValue) return new NumberValue(DataType.Int, _num.Value != right.Value ? 1 : 0);
			}

			return new NumberValue(DataType.Int, null);
		}

		public override Value CompareLessThan(RunScope scope, Span span, Value rightValue)
		{
			if (_num.HasValue)
			{
				var right = rightValue.ToNumber(scope, span);
				if (right.HasValue) return new NumberValue(DataType.Int, _num.Value < right.Value ? 1 : 0);
			}

			return new NumberValue(DataType.Int, null);
		}

		public override Value CompareGreaterThan(RunScope scope, Span span, Value rightValue)
		{
			if (_num.HasValue)
			{
				var right = rightValue.ToNumber(scope, span);
				if (right.HasValue) return new NumberValue(DataType.Int, _num.Value > right.Value ? 1 : 0);
			}

			return new NumberValue(DataType.Int, null);
		}

		public override Value CompareLessEqual(RunScope scope, Span span, Value rightValue)
		{
			if (_num.HasValue)
			{
				var right = rightValue.ToNumber(scope, span);
				if (right.HasValue) return new NumberValue(DataType.Int, _num.Value <= right.Value ? 1 : 0);
			}

			return new NumberValue(DataType.Int, null);
		}

		public override Value CompareGreaterEqual(RunScope scope, Span span, Value rightValue)
		{
			if (_num.HasValue)
			{
				var right = rightValue.ToNumber(scope, span);
				if (right.HasValue) return new NumberValue(DataType.Int, _num.Value >= right.Value ? 1 : 0);
			}

			return new NumberValue(DataType.Int, null);
		}

		public override bool IsTrue
		{
			get
			{
				if (_num.HasValue) return _num.Value != 0;
				return false;
			}
		}

		public override bool IsFalse
		{
			get
			{
				if (_num.HasValue) return _num.Value == 0;
				return false;
			}
		}

		public override decimal? ToNumber(RunScope scope, Span span)
		{
			return _num;
		}

		public override string ToStringValue(RunScope scope, Span span)
		{
			if (_num.HasValue)
			{
				return _num.Value.ToString();
			}
			return null;
		}

		public override DkDate? ToDate(RunScope scope, Span span)
		{
			if (_num.HasValue)
			{
				if (_num.Value < 0 || _num.Value > 65535)
				{
					scope.CodeAnalyzer.ReportError(span, CAError.CA0052);	// Date math results in an out-of-bounds value.
					return null;
				}
				return new DkDate((int)_num.Value);
			}

			return null;
		}

		public override DkTime? ToTime(RunScope scope, Span span)
		{
			if (_num.HasValue)
			{
				if (_num.Value < 0 || _num.Value > DkTime.MaxValue)
				{
					scope.CodeAnalyzer.ReportError(span, CAError.CA0054);	// Time math results in an out-of-bounds value.
					return null;
				}

				return new DkTime((int)_num.Value);
			}

			return null;
		}

		public override char? ToChar(RunScope scope, Span span)
		{
			if (_num.HasValue)
			{
				if (_num.Value < 0 || _num.Value > 65535)
				{
					scope.CodeAnalyzer.ReportError(span, CAError.CA0056);	// Char math results in an out-of-bounds value.
					return null;
				}

				return (char)_num.Value;
			}

			return null;
		}
	}
}

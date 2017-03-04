using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel;

namespace DkTools.CodeAnalysis.Values
{
	class DateValue : Value
	{
		private DkDate? _date;

		public DateValue(DataType dataType, DkDate? date)
			: base(dataType)
		{
			_date = date;
		}

		public DateValue(DataType dataType, decimal num)
			: base(dataType)
		{
			if (num < 0 || num > 65535) _date = null;
			else _date = new DkDate((int)num);
		}

		public override DkDate? ToDate(RunScope scope, Span span)
		{
			return _date;
		}

		public override string ToStringValue(RunScope scope, Span span)
		{
			if (_date.HasValue)
			{
				return _date.ToString();
			}
			return null;
		}

		public override decimal? ToNumber(RunScope scope, Span span)
		{
			if (_date.HasValue)
			{
				return (decimal)_date.Value.Number;
			}
			return null;
		}

		public override DkTime? ToTime(RunScope scope, Span span)
		{
			scope.CodeAnalyzer.ReportError(span, CAError.CA0055, "date", "time");	// Converting {0} to {1}.
			if (_date.HasValue)
			{
				return new DkTime(_date.Value.Number);
			}

			return null;
		}

		public override char? ToChar(RunScope scope, Span span)
		{
			scope.CodeAnalyzer.ReportError(span, CAError.CA0055, "date", "char");	// Converting {0} to {1}.
			if (_date.HasValue) return (char)_date.Value.Number;
			return null;
		}

		public override Value Multiply(RunScope scope, Span span, Value rightValue)
		{
			var left = ToNumber(scope, span);
			if (left.HasValue)
			{
				var right = rightValue.ToNumber(scope, span);
				if (right.HasValue)
				{
					var result = left.Value * right.Value;
					if (result < 0 || result > 65535)
					{
						scope.CodeAnalyzer.ReportError(span, CAError.CA0052);	// Date math results in an out-of-bounds value.
						return new DateValue(DataType, null);
					}
					else
					{
						return new DateValue(DataType, result);
					}
				}
			}

			return new DateValue(DataType, null);
		}

		public override Value Divide(RunScope scope, Span span, Value rightValue)
		{
			var left = ToNumber(scope, span);
			if (left.HasValue)
			{
				var right = rightValue.ToNumber(scope, span);
				if (right.HasValue)
				{
					if (right.Value == 0)
					{
						scope.CodeAnalyzer.ReportError(span, CAError.CA0051);	// Division by zero.
						return new DateValue(DataType, null);
					}
					else
					{
						var result = left.Value / right.Value;
						if (result < 0 || result > 65535)
						{
							scope.CodeAnalyzer.ReportError(span, CAError.CA0052);	// Date math results in an out-of-bounds value.
							return new DateValue(DataType, null);
						}
						else
						{
							return new DateValue(DataType, result);
						}
					}
				}
			}

			return new DateValue(DataType, null);
		}

		public override Value ModulusDivide(RunScope scope, Span span, Value rightValue)
		{
			var left = ToNumber(scope, span);
			if (left.HasValue)
			{
				var right = rightValue.ToNumber(scope, span);
				if (right.HasValue)
				{
					if (right.Value == 0)
					{
						scope.CodeAnalyzer.ReportError(span, CAError.CA0051);	// Division by zero.
						return new DateValue(DataType, null);
					}
					else
					{
						var result = left.Value % right.Value;
						if (result < 0 || result > 65535)
						{
							scope.CodeAnalyzer.ReportError(span, CAError.CA0052);	// Date math results in an out-of-bounds value.
							return new DateValue(DataType, null);
						}
						else
						{
							return new DateValue(DataType, result);
						}
					}
				}
			}

			return new DateValue(DataType, null);
		}

		public override Value Add(RunScope scope, Span span, Value rightValue)
		{
			var left = ToNumber(scope, span);
			if (left.HasValue)
			{
				var right = rightValue.ToNumber(scope, span);
				if (right.HasValue)
				{
					var result = left.Value + right.Value;
					if (result < 0 || result > 65535)
					{
						scope.CodeAnalyzer.ReportError(span, CAError.CA0052);	// Date math results in an out-of-bounds value.
						return new DateValue(DataType, null);
					}
					else
					{
						return new DateValue(DataType, result);
					}
				}
			}

			return new DateValue(DataType, null);
		}

		public override Value Subtract(RunScope scope, Span span, Value rightValue)
		{
			var left = ToNumber(scope, span);
			if (left.HasValue)
			{
				var right = rightValue.ToNumber(scope, span);
				if (right.HasValue)
				{
					var result = left.Value - right.Value;
					if (result < 0 || result > 65535)
					{
						scope.CodeAnalyzer.ReportError(span, CAError.CA0052);	// Date math results in an out-of-bounds value.
						return new DateValue(DataType, null);
					}
					else
					{
						return new DateValue(DataType, result);
					}
				}
			}

			return new DateValue(DataType, null);
		}

		public override Value Invert(RunScope scope, Span span)
		{
			scope.CodeAnalyzer.ReportError(span, CAError.CA0052);	// Date math results in an out-of-bounds value.
			return new DateValue(DataType, null);
		}

		public override Value CompareEqual(RunScope scope, Span span, Value rightValue)
		{
			if (_date.HasValue)
			{
				var right = rightValue.ToDate(scope, span);
				if (right.HasValue)
				{
					return new NumberValue(DataType.Int, _date.Value.Number == right.Value.Number ? 1 : 0);
				}
			}

			return new NumberValue(DataType.Int, null);
		}

		public override Value CompareNotEqual(RunScope scope, Span span, Value rightValue)
		{
			if (_date.HasValue)
			{
				var right = rightValue.ToDate(scope, span);
				if (right.HasValue)
				{
					return new NumberValue(DataType.Int, _date.Value.Number != right.Value.Number ? 1 : 0);
				}
			}

			return new NumberValue(DataType.Int, null);
		}

		public override Value CompareLessThan(RunScope scope, Span span, Value rightValue)
		{
			if (_date.HasValue)
			{
				var right = rightValue.ToDate(scope, span);
				if (right.HasValue)
				{
					return new NumberValue(DataType.Int, _date.Value.Number < right.Value.Number ? 1 : 0);
				}
			}

			return new NumberValue(DataType.Int, null);
		}

		public override Value CompareGreaterThan(RunScope scope, Span span, Value rightValue)
		{
			if (_date.HasValue)
			{
				var right = rightValue.ToDate(scope, span);
				if (right.HasValue)
				{
					return new NumberValue(DataType.Int, _date.Value.Number > right.Value.Number ? 1 : 0);
				}
			}

			return new NumberValue(DataType.Int, null);
		}

		public override Value CompareLessEqual(RunScope scope, Span span, Value rightValue)
		{
			if (_date.HasValue)
			{
				var right = rightValue.ToDate(scope, span);
				if (right.HasValue)
				{
					return new NumberValue(DataType.Int, _date.Value.Number <= right.Value.Number ? 1 : 0);
				}
			}

			return new NumberValue(DataType.Int, null);
		}

		public override Value CompareGreaterEqual(RunScope scope, Span span, Value rightValue)
		{
			if (_date.HasValue)
			{
				var right = rightValue.ToDate(scope, span);
				if (right.HasValue)
				{
					return new NumberValue(DataType.Int, _date.Value.Number >= right.Value.Number ? 1 : 0);
				}
			}

			return new NumberValue(DataType.Int, null);
		}

		public override bool IsTrue
		{
			get
			{
				if (_date.HasValue) return _date.Value.Number != 0;
				return false;
			}
		}

		public override bool IsFalse
		{
			get
			{
				if (_date.HasValue) return _date.Value.Number == 0;
				return false;
			}
		}
	}

	struct DkDate
	{
		private int _val;

		public static readonly DkDate Zero = new DkDate(0);

		public DkDate(int val)
		{
			_val = val;
		}

		public DkDate(int year, int month, int day)
		{
			_val = new DateTime(year, month, day).Subtract(Constants.ZeroDate).Days;
		}

		public int Number
		{
			get { return _val; }
			set
			{
				if (value < 0 || value > 65535) _val = 0;
				else _val = value;
			}
		}

		public override string ToString()
		{
			var dt = Constants.ZeroDate.AddDays(_val);
			return dt.ToString("ddMMMyyyy");
		}
	}
}

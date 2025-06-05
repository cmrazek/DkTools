using DK.Code;
using DK.Modeling;
using System;

namespace DK.CodeAnalysis.Values
{
	class DateValue : Value
	{
		private DkDate? _date;

		public DateValue(DataType dataType, DkDate? date, bool literal)
			: base(dataType, literal)
		{
			_date = date;
		}

		public override string ToString() => _date.HasValue ? _date.Value.ToString() : "(null-date)";

		public override Value CloneNonLiteral() => IsLiteral ? new DateValue(DataType, _date, literal: false) : this;

        public DateValue(DataType dataType, decimal num, bool literal)
			: base(dataType, literal)
		{
			if (num < 0 || num > 65535) _date = null;
			else _date = new DkDate((int)num);
		}

		public override DkDate? ToDate(CAScope scope, CodeSpan span)
		{
			return _date;
		}

		public override string ToStringValue(CAScope scope, CodeSpan span)
		{
			if (_date.HasValue)
			{
				return _date.ToString();
			}
			return null;
		}

		public override decimal? ToNumber(CAScope scope, CodeSpan span)
		{
			if (_date.HasValue)
			{
				return (decimal)_date.Value.Number;
			}
			return null;
		}

		public override DkTime? ToTime(CAScope scope, CodeSpan span)
		{
			scope.CodeAnalyzer.ReportError(span, CAError.CA10055, "date", "time");	// Converting {0} to {1}.
			if (_date.HasValue)
			{
				return new DkTime(_date.Value.Number);
			}

			return null;
		}

		public override char? ToChar(CAScope scope, CodeSpan span)
		{
			scope.CodeAnalyzer.ReportError(span, CAError.CA10055, "date", "char");	// Converting {0} to {1}.
			if (_date.HasValue) return (char)_date.Value.Number;
			return null;
		}

		public override Value Multiply(CAScope scope, CodeSpan span, Value rightValue)
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
						scope.CodeAnalyzer.ReportError(span, CAError.CA10052);	// Date math results in an out-of-bounds value.
						return new DateValue(DataType, date: null, literal: false);
					}
					else
					{
						return new DateValue(DataType, result, literal: false);
					}
				}
			}

			return new DateValue(DataType, date: null, literal: false);
		}

		public override Value Divide(CAScope scope, CodeSpan span, Value rightValue)
		{
			var left = ToNumber(scope, span);
			if (left.HasValue)
			{
				var right = rightValue.ToNumber(scope, span);
				if (right.HasValue)
				{
					if (right.Value == 0)
					{
						scope.CodeAnalyzer.ReportError(span, CAError.CA10051);	// Division by zero.
						return new DateValue(DataType, date: null, literal: false);
					}
					else
					{
						var result = left.Value / right.Value;
						if (result < 0 || result > 65535)
						{
							scope.CodeAnalyzer.ReportError(span, CAError.CA10052);	// Date math results in an out-of-bounds value.
							return new DateValue(DataType, date: null, literal: false);
						}
						else
						{
							return new DateValue(DataType, result, literal: false);
						}
					}
				}
			}

			return new DateValue(DataType, date: null, literal: false);
		}

		public override Value ModulusDivide(CAScope scope, CodeSpan span, Value rightValue)
		{
			var left = ToNumber(scope, span);
			if (left.HasValue)
			{
				var right = rightValue.ToNumber(scope, span);
				if (right.HasValue)
				{
					if (right.Value == 0)
					{
						scope.CodeAnalyzer.ReportError(span, CAError.CA10051);	// Division by zero.
						return new DateValue(DataType, date: null, literal: false);
					}
					else
					{
						var result = left.Value % right.Value;
						if (result < 0 || result > 65535)
						{
							scope.CodeAnalyzer.ReportError(span, CAError.CA10052);	// Date math results in an out-of-bounds value.
							return new DateValue(DataType, date: null, literal: false);
						}
						else
						{
							return new DateValue(DataType, result, literal: false);
						}
					}
				}
			}

			return new DateValue(DataType, date: null, literal: false);
		}

		public override Value Add(CAScope scope, CodeSpan span, Value rightValue)
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
						scope.CodeAnalyzer.ReportError(span, CAError.CA10052);	// Date math results in an out-of-bounds value.
						return new DateValue(DataType, date: null, literal: false);
					}
					else
					{
						return new DateValue(DataType, result, literal: false);
					}
				}
			}

			return new DateValue(DataType, date: null, literal: false);
		}

		public override Value Subtract(CAScope scope, CodeSpan span, Value rightValue)
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
						scope.CodeAnalyzer.ReportError(span, CAError.CA10052);	// Date math results in an out-of-bounds value.
						return new DateValue(DataType, date: null, literal: false);
					}
					else
					{
						return new DateValue(DataType, result, literal: false);
					}
				}
			}

			return new DateValue(DataType, date: null, literal: false);
		}

		public override Value Invert(CAScope scope, CodeSpan span)
		{
			scope.CodeAnalyzer.ReportError(span, CAError.CA10052);	// Date math results in an out-of-bounds value.
			return new DateValue(DataType, date: null, literal: false);
		}

		public override Value CompareEqual(CAScope scope, CodeSpan span, Value rightValue)
		{
			if (_date.HasValue)
			{
				var right = rightValue.ToDate(scope, span);
				if (right.HasValue)
				{
					return new NumberValue(DataType.Int, _date.Value.Number == right.Value.Number ? 1 : 0, literal: false);
				}
			}

			return new NumberValue(DataType.Int, number: null, literal: false);
		}

		public override Value CompareNotEqual(CAScope scope, CodeSpan span, Value rightValue)
		{
			if (_date.HasValue)
			{
				var right = rightValue.ToDate(scope, span);
				if (right.HasValue)
				{
					return new NumberValue(DataType.Int, _date.Value.Number != right.Value.Number ? 1 : 0, literal: false);
				}
			}

			return new NumberValue(DataType.Int, number: null, literal: false);
		}

		public override Value CompareLessThan(CAScope scope, CodeSpan span, Value rightValue)
		{
			if (_date.HasValue)
			{
				var right = rightValue.ToDate(scope, span);
				if (right.HasValue)
				{
					return new NumberValue(DataType.Int, _date.Value.Number < right.Value.Number ? 1 : 0, literal: false);
				}
			}

			return new NumberValue(DataType.Int, number: null, literal: false);
		}

		public override Value CompareGreaterThan(CAScope scope, CodeSpan span, Value rightValue)
		{
			if (_date.HasValue)
			{
				var right = rightValue.ToDate(scope, span);
				if (right.HasValue)
				{
					return new NumberValue(DataType.Int, _date.Value.Number > right.Value.Number ? 1 : 0, literal: false);
				}
			}

			return new NumberValue(DataType.Int, number: null, literal: false);
		}

		public override Value CompareLessEqual(CAScope scope, CodeSpan span, Value rightValue)
		{
			if (_date.HasValue)
			{
				var right = rightValue.ToDate(scope, span);
				if (right.HasValue)
				{
					return new NumberValue(DataType.Int, _date.Value.Number <= right.Value.Number ? 1 : 0, literal: false);
				}
			}

			return new NumberValue(DataType.Int, number: null, literal: false);
		}

		public override Value CompareGreaterEqual(CAScope scope, CodeSpan span, Value rightValue)
		{
			if (_date.HasValue)
			{
				var right = rightValue.ToDate(scope, span);
				if (right.HasValue)
				{
					return new NumberValue(DataType.Int, _date.Value.Number >= right.Value.Number ? 1 : 0, literal: false);
				}
			}

			return new NumberValue(DataType.Int, number: null, literal: false);
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

		public override Value Convert(CAScope scope, CodeSpan span, Value value)
		{
			return new DateValue(DataType, value.ToDate(scope, span), value.IsLiteral);
		}

		public override bool IsEqualTo(Value other)
		{
			if (!_date.HasValue) return false;
			var o = other as DateValue;
			if (o == null || !o._date.HasValue) return false;
			return _date.Value == o._date.Value;
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

		public override bool Equals(object obj)
		{
			if (obj == null || obj.GetType() != typeof(DkDate)) return false;
			return _val == ((DkDate)obj)._val;
		}

		public override int GetHashCode()
		{
			return _val.GetHashCode();
		}

		public static bool operator ==(DkDate a, DkDate b) => a._val == b._val;
		public static bool operator !=(DkDate a, DkDate b) => a._val != b._val;
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel;

namespace DkTools.CodeAnalysis.Values
{
	class TimeValue : Value
	{
		private DkTime? _time;

		public TimeValue(DataType dataType, DkTime? time)
			: base(dataType)
		{
			_time = time;
		}

		public override string ToString() => _time.HasValue ? _time.Value.ToString() : "(null-time)";

		public override Value Multiply(RunScope scope, Span span, Value rightValue)
		{
			if (_time.HasValue)
			{
				var right = rightValue.ToTime(scope, span);
				if (right.HasValue)
				{
					var result = _time.Value.Ticks * right.Value.Ticks;
					if (result < 0 || result > 43200)
					{
						scope.CodeAnalyzer.ReportError(span, CAError.CA0054);	// Time math results in an out-of-bounds value.
						return new TimeValue(DataType, null);
					}

					return new TimeValue(DataType, new DkTime(result));
				}
			}

			return new TimeValue(DataType, null);
		}

		public override Value Divide(RunScope scope, Span span, Value rightValue)
		{
			if (_time.HasValue)
			{
				var right = rightValue.ToTime(scope, span);
				if (right.HasValue)
				{
					var rightNum = right.Value.Ticks;
					if (rightNum == 0)
					{
						scope.CodeAnalyzer.ReportError(span, CAError.CA0051);	// Division by zero.
						return new TimeValue(DataType, null);
					}

					var result = _time.Value.Ticks / rightNum;
					if (result < 0 || result > 43200)
					{
						scope.CodeAnalyzer.ReportError(span, CAError.CA0054);	// Time math results in an out-of-bounds value.
						return new TimeValue(DataType, null);
					}

					return new TimeValue(DataType, new DkTime(result));
				}
			}

			return new TimeValue(DataType, null);
		}

		public override Value ModulusDivide(RunScope scope, Span span, Value rightValue)
		{
			if (_time.HasValue)
			{
				var right = rightValue.ToTime(scope, span);
				if (right.HasValue)
				{
					var rightNum = right.Value.Ticks;
					if (rightNum == 0)
					{
						scope.CodeAnalyzer.ReportError(span, CAError.CA0051);	// Division by zero.
						return new TimeValue(DataType, null);
					}

					var result = _time.Value.Ticks % rightNum;
					if (result < 0 || result > 43200)
					{
						scope.CodeAnalyzer.ReportError(span, CAError.CA0054);	// Time math results in an out-of-bounds value.
						return new TimeValue(DataType, null);
					}

					return new TimeValue(DataType, new DkTime(result));
				}
			}

			return new TimeValue(DataType, null);
		}

		public override Value Add(RunScope scope, Span span, Value rightValue)
		{
			if (_time.HasValue)
			{
				var right = rightValue.ToTime(scope, span);
				if (right.HasValue)
				{
					var result = _time.Value.Ticks + right.Value.Ticks;
					if (result < 0 || result > 43200)
					{
						scope.CodeAnalyzer.ReportError(span, CAError.CA0054);	// Time math results in an out-of-bounds value.
						return new TimeValue(DataType, null);
					}

					return new TimeValue(DataType, new DkTime(result));
				}
			}

			return new TimeValue(DataType, null);
		}

		public override Value Subtract(RunScope scope, Span span, Value rightValue)
		{
			if (_time.HasValue)
			{
				var right = rightValue.ToTime(scope, span);
				if (right.HasValue)
				{
					var result = _time.Value.Ticks - right.Value.Ticks;
					if (result < 0 || result > 43200)
					{
						scope.CodeAnalyzer.ReportError(span, CAError.CA0054);	// Time math results in an out-of-bounds value.
						return new TimeValue(DataType, null);
					}

					return new TimeValue(DataType, new DkTime(result));
				}
			}

			return new TimeValue(DataType, null);
		}

		public override Value Invert(RunScope scope, Span span)
		{
			scope.CodeAnalyzer.ReportError(span, CAError.CA0054);	// Time math results in an out-of-bounds value.
			return new TimeValue(DataType, null);
		}

		public override Value CompareEqual(RunScope scope, Span span, Value rightValue)
		{
			if (_time.HasValue)
			{
				var right = rightValue.ToTime(scope, span);
				if (right.HasValue)
				{
					return new NumberValue(DataType.Int, _time.Value.Ticks == right.Value.Ticks ? 1 : 0);
				}
			}

			return new TimeValue(DataType, null);
		}

		public override Value CompareNotEqual(RunScope scope, Span span, Value rightValue)
		{
			if (_time.HasValue)
			{
				var right = rightValue.ToTime(scope, span);
				if (right.HasValue)
				{
					return new NumberValue(DataType.Int, _time.Value.Ticks != right.Value.Ticks ? 1 : 0);
				}
			}

			return new TimeValue(DataType, null);
		}

		public override Value CompareLessThan(RunScope scope, Span span, Value rightValue)
		{
			if (_time.HasValue)
			{
				var right = rightValue.ToTime(scope, span);
				if (right.HasValue)
				{
					return new NumberValue(DataType.Int, _time.Value.Ticks < right.Value.Ticks ? 1 : 0);
				}
			}

			return new TimeValue(DataType, null);
		}

		public override Value CompareGreaterThan(RunScope scope, Span span, Value rightValue)
		{
			if (_time.HasValue)
			{
				var right = rightValue.ToTime(scope, span);
				if (right.HasValue)
				{
					return new NumberValue(DataType.Int, _time.Value.Ticks > right.Value.Ticks ? 1 : 0);
				}
			}

			return new TimeValue(DataType, null);
		}

		public override Value CompareLessEqual(RunScope scope, Span span, Value rightValue)
		{
			if (_time.HasValue)
			{
				var right = rightValue.ToTime(scope, span);
				if (right.HasValue)
				{
					return new NumberValue(DataType.Int, _time.Value.Ticks <= right.Value.Ticks ? 1 : 0);
				}
			}

			return new TimeValue(DataType, null);
		}

		public override Value CompareGreaterEqual(RunScope scope, Span span, Value rightValue)
		{
			if (_time.HasValue)
			{
				var right = rightValue.ToTime(scope, span);
				if (right.HasValue)
				{
					return new NumberValue(DataType.Int, _time.Value.Ticks >= right.Value.Ticks ? 1 : 0);
				}
			}

			return new TimeValue(DataType, null);
		}

		public override bool IsTrue
		{
			get
			{
				if (_time.HasValue) return _time.Value.Ticks != 0;
				return false;
			}
		}

		public override bool IsFalse
		{
			get
			{
				if (_time.HasValue) return _time.Value.Ticks == 0;
				return false;
			}
		}

		public override DkTime? ToTime(RunScope scope, Span span)
		{
			return _time;
		}

		public override string ToStringValue(RunScope scope, Span span)
		{
			return _time.HasValue ? _time.Value.ToString() : null;
		}

		public override DkDate? ToDate(RunScope scope, Span span)
		{
			scope.CodeAnalyzer.ReportError(span, CAError.CA0055, "time", "date");	// Converting {0} to {1}.

			if (_time.HasValue) return new DkDate(_time.Value.Ticks);
			return null;
		}

		public override decimal? ToNumber(RunScope scope, Span span)
		{
			if (_time.HasValue) return _time.Value.Ticks;
			return null;
		}

		public override char? ToChar(RunScope scope, Span span)
		{
			scope.CodeAnalyzer.ReportError(span, CAError.CA0055, "time", "char");	// Converting {0} to {1}.
			if (_time.HasValue) return (char)_time.Value.Ticks;
			return null;
		}

		public override Value Convert(RunScope scope, Span span, Value value)
		{
			return new TimeValue(DataType, value.ToTime(scope, span));
		}

		public override bool IsEqualTo(Value other)
		{
			if (!_time.HasValue) return false;
			var o = other as TimeValue;
			if (o == null || !o._time.HasValue) return false;
			return _time.Value == o._time.Value;
		}
	}

	struct DkTime
	{
		private int _hour, _minute, _second;

		public static readonly DkTime Zero = new DkTime(0, 0, 0);
		public const int MaxValue = 43200;

		public DkTime(int hour, int minute, int second)
		{
			_hour = hour;
			_minute = minute;
			_second = second;
		}

		public DkTime(int ticks)
		{
			ticks *= 2;

			_hour = ticks / 3600;
			ticks %= 3600;

			_minute = ticks / 60;
			ticks %= 60;

			_second = ticks;
		}

		public int Ticks
		{
			get
			{
				return (_hour * 3600 + _minute * 60 + _second) / 2;
			}
		}

		public override string ToString()
		{
			return string.Format("{0:00}:{1:00}:{2:00}", _hour, _minute, _second);
		}

		public override bool Equals(object obj)
		{
			if (obj == null || obj.GetType() != typeof(DkTime)) return false;
			var o = (DkTime)obj;
			return _hour == o._hour && _minute == o._minute && _second == o._second;
		}

		public override int GetHashCode()
		{
			return _hour.GetHashCode() * 59 + _minute.GetHashCode() * 13 + _second.GetHashCode();
		}

		public static bool operator ==(DkTime a, DkTime b) => a._hour == b._hour && a._minute == b._minute && a._second == b._second;
		public static bool operator !=(DkTime a, DkTime b) => a._hour != b._hour || a._minute != b._minute || a._second != b._second;
	}
}

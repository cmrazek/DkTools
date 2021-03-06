﻿using DK.Code;
using DK.Modeling;

namespace DK.CodeAnalysis.Values
{
	class EnumValue : Value
	{
		private string _value;
		private int? _ordinal;

		public EnumValue(DataType dataType, string value, int? ordinal)
			: base(dataType)
		{
			_value = value;
			_ordinal = ordinal;
		}

		public EnumValue(DataType dataType, int ordinal)
			: base(dataType)
		{
			_ordinal = ordinal;

			if (dataType.HasEnumOptions)
			{
				var def = dataType.GetEnumOption(ordinal);
				if (def != null) _value = def.Name;
			}
		}

		public EnumValue(DataType dataType, string value)
			: base(dataType)
		{
			_value = value;

			if (_value != null && dataType.HasEnumOptions)
			{
				_ordinal = dataType.GetEnumOrdinal(_value);
			}
		}

		public override string ToString() => !string.IsNullOrEmpty(_value) ? _value : _ordinal.ToString();

		public override decimal? ToNumber(CAScope scope, CodeSpan span)
		{
			if (_ordinal.HasValue) return (decimal)_ordinal.Value;
			return null;
		}

		public override string ToStringValue(CAScope scope, CodeSpan span)
		{
			return _value;
		}

		public override DkDate? ToDate(CAScope scope, CodeSpan span)
		{
			if (_ordinal.HasValue)
			{
				return new DkDate(_ordinal.Value);
			}

			return null;
		}

		public override DkTime? ToTime(CAScope scope, CodeSpan span)
		{
			if (_ordinal.HasValue)
			{
				return new DkTime(_ordinal.Value);
			}

			return null;
		}

		public override char? ToChar(CAScope scope, CodeSpan span)
		{
			if (_ordinal.HasValue)
			{
				if (_ordinal.Value < 0 || _ordinal.Value > 65535)
				{
					scope.CodeAnalyzer.ReportError(span, CAError.CA0056);	// Char math results in an out-of-bounds value.
					return null;
				}

				return (char)_ordinal.Value;
			}

			return null;
		}

		public int NumEnumOptions
		{
			get { return DataType.NumEnumOptions; }
		}

		public override Value Multiply(CAScope scope, CodeSpan span, Value rightValue)
		{
			if (_ordinal.HasValue)
			{
				var right = rightValue.ToNumber(scope, span);
				if (right.HasValue)
				{
					var result = (int)_ordinal.Value * (int)right.Value;
					if (result < 0 || result >= NumEnumOptions) scope.CodeAnalyzer.ReportError(span, CAError.CA0053);	// Enum math results in an out-of-bounds value.
					return new EnumValue(DataType, result);
				}
			}

			return new EnumValue(DataType, null, null);
		}

		public override Value Divide(CAScope scope, CodeSpan span, Value rightValue)
		{
			if (_ordinal.HasValue)
			{
				var right = rightValue.ToNumber(scope, span);
				if (right.HasValue)
				{
					if (right.Value == 0)
					{
						scope.CodeAnalyzer.ReportError(span, CAError.CA0051);	// Division by zero.
						return new EnumValue(DataType, null, null);
					}

					var result = (int)_ordinal.Value / (int)right.Value;
					if (result < 0 || result >= NumEnumOptions) scope.CodeAnalyzer.ReportError(span, CAError.CA0053);	// Enum math results in an out-of-bounds value.
					return new EnumValue(DataType, result);
				}
			}

			return new EnumValue(DataType, null, null);
		}

		public override Value ModulusDivide(CAScope scope, CodeSpan span, Value rightValue)
		{
			if (_ordinal.HasValue)
			{
				var right = rightValue.ToNumber(scope, span);
				if (right.HasValue)
				{
					if (right.Value == 0)
					{
						scope.CodeAnalyzer.ReportError(span, CAError.CA0051);	// Division by zero.
						return new EnumValue(DataType, null, null);
					}

					var result = (int)_ordinal.Value % (int)right.Value;
					if (result < 0 || result >= NumEnumOptions) scope.CodeAnalyzer.ReportError(span, CAError.CA0053);	// Enum math results in an out-of-bounds value.
					return new EnumValue(DataType, result);
				}
			}

			return new EnumValue(DataType, null, null);
		}

		public override Value Add(CAScope scope, CodeSpan span, Value rightValue)
		{
			if (_ordinal.HasValue)
			{
				var right = rightValue.ToNumber(scope, span);
				if (right.HasValue)
				{
					var result = (int)_ordinal.Value + (int)right.Value;
					if (result < 0 || result >= NumEnumOptions) scope.CodeAnalyzer.ReportError(span, CAError.CA0053);	// Enum math results in an out-of-bounds value.
					return new EnumValue(DataType, result);
				}
			}

			return new EnumValue(DataType, null, null);
		}

		public override Value Subtract(CAScope scope, CodeSpan span, Value rightValue)
		{
			if (_ordinal.HasValue)
			{
				var right = rightValue.ToNumber(scope, span);
				if (right.HasValue)
				{
					var result = (int)_ordinal.Value - (int)right.Value;
					if (result < 0 || result >= NumEnumOptions) scope.CodeAnalyzer.ReportError(span, CAError.CA0053);	// Enum math results in an out-of-bounds value.
					return new EnumValue(DataType, result);
				}
			}

			return new EnumValue(DataType, null, null);
		}

		public override Value Invert(CAScope scope, CodeSpan span)
		{
			scope.CodeAnalyzer.ReportError(span, CAError.CA0053);	// Enum math results in an out-of-bounds value.
			return new EnumValue(DataType, null, null);
		}

		public override Value CompareEqual(CAScope scope, CodeSpan span, Value rightValue)
		{
			if (_value != null)
			{
				var right = rightValue.ToString();
				if (right != null)
				{
					return new NumberValue(DataType.Int, DataType.NormalizeEnumOption(_value) == DataType.NormalizeEnumOption(right) ? 1 : 0);
				}
			}

			return new NumberValue(DataType.Int, null);
		}

		public override Value CompareNotEqual(CAScope scope, CodeSpan span, Value rightValue)
		{
			if (_value != null)
			{
				var right = rightValue.ToString();
				if (right != null)
				{
					return new NumberValue(DataType.Int, DataType.NormalizeEnumOption(_value) != DataType.NormalizeEnumOption(right) ? 1 : 0);
				}
			}

			return new NumberValue(DataType.Int, null);
		}

		public override Value CompareLessThan(CAScope scope, CodeSpan span, Value rightValue)
		{
			if (_ordinal != null)
			{
				var right = rightValue.ToNumber(scope, span);
				if (right.HasValue)
				{
					return new NumberValue(DataType.Int, _ordinal.Value < (int)right.Value ? 1 : 0);
				}
			}

			return new NumberValue(DataType.Int, null);
		}

		public override Value CompareGreaterThan(CAScope scope, CodeSpan span, Value rightValue)
		{
			if (_ordinal != null)
			{
				var right = rightValue.ToNumber(scope, span);
				if (right.HasValue)
				{
					return new NumberValue(DataType.Int, _ordinal.Value > (int)right.Value ? 1 : 0);
				}
			}

			return new NumberValue(DataType.Int, null);
		}

		public override Value CompareLessEqual(CAScope scope, CodeSpan span, Value rightValue)
		{
			if (_ordinal != null)
			{
				var right = rightValue.ToNumber(scope, span);
				if (right.HasValue)
				{
					return new NumberValue(DataType.Int, _ordinal.Value <= (int)right.Value ? 1 : 0);
				}
			}

			return new NumberValue(DataType.Int, null);
		}

		public override Value CompareGreaterEqual(CAScope scope, CodeSpan span, Value rightValue)
		{
			if (_ordinal != null)
			{
				var right = rightValue.ToNumber(scope, span);
				if (right.HasValue)
				{
					return new NumberValue(DataType.Int, _ordinal.Value >= (int)right.Value ? 1 : 0);
				}
			}

			return new NumberValue(DataType.Int, null);
		}

		public override bool IsTrue
		{
			get
			{
				if (_ordinal.HasValue) return _ordinal.Value != 0;
				return false;
			}
		}

		public override bool IsFalse
		{
			get
			{
				if (_ordinal.HasValue) return _ordinal.Value == 0;
				return false;
			}
		}

		public override Value Convert(CAScope scope, CodeSpan span, Value value)
		{
			var str = value.ToStringValue(scope, span);
			if (str != null && DataType.IsValidEnumOption(str)) return new EnumValue(DataType, str);
			return new EnumValue(DataType, null, null);
		}

		public override bool IsEqualTo(Value other)
		{
			if (!_ordinal.HasValue) return false;
			var o = other as EnumValue;
			if (o == null || !o._ordinal.HasValue) return false;
			return _ordinal.Value == o._ordinal.Value;
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel;

namespace DkTools.CodeAnalysis.Values
{
	abstract class Value
	{
		private DataType _dataType;

		public static readonly Value Void = new VoidValue();

		public abstract decimal? ToNumber(RunScope scope, Span span);
		public abstract string ToStringValue(RunScope scope, Span span);
		public abstract DkDate? ToDate(RunScope scope, Span span);
		public abstract DkTime? ToTime(RunScope scope, Span span);
		public abstract char? ToChar(RunScope scope, Span span);
		public abstract Value Convert(RunScope scope, Span span, Value value);
		public abstract bool IsEqualTo(Value other);

		protected Value(DataType dataType)
		{
			_dataType = dataType;
		}

		public DataType DataType
		{
			get { return _dataType; }
		}

		public bool IsVoid
		{
			get
			{
				if (_dataType == null) return true;
				if (_dataType.ValueType == ValType.Void) return true;
				return false;
			}
		}

		public static Value CreateUnknownFromDataType(DataType dataType)
		{
			if (dataType == null) return Value.Void;

			switch (dataType.ValueType)
			{
				case ValType.Numeric:
					return new NumberValue(dataType, null);
				case ValType.String:
					return new StringValue(dataType, null);
				case ValType.Char:
					return new CharValue(dataType, null);
				case ValType.Enum:
					return new EnumValue(dataType, null, null);
				case ValType.Date:
					return new DateValue(dataType, null);
				case ValType.Time:
					return new TimeValue(dataType, null);
				case ValType.Table:
					return new TableValue(dataType, null);
				case ValType.IndRel:
					return new IndRelValue(dataType, null);
				default:
					return Value.Void;
			}
		}

		public virtual Value Multiply(RunScope scope, Span span, Value rightValue)
		{
			scope.CodeAnalyzer.ReportError(span, CAError.CA0050, "Multiplication");	// {0} cannot be used with this value.
			return this;
		}

		public virtual Value Divide(RunScope scope, Span span, Value rightValue)
		{
			scope.CodeAnalyzer.ReportError(span, CAError.CA0050, "Division");	// {0} cannot be used with this value.
			return this;
		}

		public virtual Value ModulusDivide(RunScope scope, Span span, Value rightValue)
		{
			scope.CodeAnalyzer.ReportError(span, CAError.CA0050, "Modulus division");	// {0} cannot be used with this value.
			return this;
		}

		public virtual Value Add(RunScope scope, Span span, Value rightValue)
		{
			scope.CodeAnalyzer.ReportError(span, CAError.CA0050, "Addition");	// {0} cannot be used with this value.
			return this;
		}

		public virtual Value Subtract(RunScope scope, Span span, Value rightValue)
		{
			scope.CodeAnalyzer.ReportError(span, CAError.CA0050, "Subtraction");	// {0} cannot be used with this value.
			return this;
		}

		public virtual Value Invert(RunScope scope, Span span)
		{
			scope.CodeAnalyzer.ReportError(span, CAError.CA0050, "Unary minus");	// {0} cannot be used with this value.
			return this;
		}

		public virtual Value CompareEqual(RunScope scope, Span span, Value rightValue)
		{
			scope.CodeAnalyzer.ReportError(span, CAError.CA0050, "Equals Comparison");	// {0} cannot be used with this value.
			return this;
		}

		public virtual Value CompareNotEqual(RunScope scope, Span span, Value rightValue)
		{
			scope.CodeAnalyzer.ReportError(span, CAError.CA0050, "Not-equal comparison");	// {0} cannot be used with this value.
			return this;
		}

		public virtual Value CompareLessThan(RunScope scope, Span span, Value rightValue)
		{
			scope.CodeAnalyzer.ReportError(span, CAError.CA0050, "Less-than comparison");	// {0} cannot be used with this value.
			return this;
		}

		public virtual Value CompareGreaterThan(RunScope scope, Span span, Value rightValue)
		{
			scope.CodeAnalyzer.ReportError(span, CAError.CA0050, "Greater-than comparison");	// {0} cannot be used with this value.
			return this;
		}

		public virtual Value CompareLessEqual(RunScope scope, Span span, Value rightValue)
		{
			scope.CodeAnalyzer.ReportError(span, CAError.CA0050, "Less-than-or-equal-to comparison");	// {0} cannot be used with this value.
			return this;
		}

		public virtual Value CompareGreaterEqual(RunScope scope, Span span, Value rightValue)
		{
			scope.CodeAnalyzer.ReportError(span, CAError.CA0050, "Greater-than-or-equal-to comparison");	// {0} cannot be used with this value.
			return this;
		}

		public virtual bool IsTrue
		{
			get { return false; }
		}

		public virtual bool IsFalse
		{
			get { return false; }
		}

		public Value Combine(DataType dataType, IEnumerable<Value> values)
		{
			var first = values.Where(x => x != null).FirstOrDefault();
			if (first == null) return null;

			if (values.Where(x => x != null).Skip(1).All(x => x.IsEqualTo(first))) return first;

			return CreateUnknownFromDataType(dataType);
		}

		public virtual void CheckTypeConversion(RunScope scope, Span span, DataType dataType)
		{
		}
	}
}

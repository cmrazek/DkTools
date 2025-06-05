using DK.Code;
using DK.Modeling;
using System.Collections.Generic;
using System.Linq;

namespace DK.CodeAnalysis.Values
{
    abstract class Value
    {
        private DataType _dataType;
        private bool _literal;

        public static readonly Value Void = new VoidValue();

        public abstract decimal? ToNumber(CAScope scope, CodeSpan span);
        public abstract string ToStringValue(CAScope scope, CodeSpan span);
        public abstract DkDate? ToDate(CAScope scope, CodeSpan span);
        public abstract DkTime? ToTime(CAScope scope, CodeSpan span);
        public abstract char? ToChar(CAScope scope, CodeSpan span);
        public abstract Value Convert(CAScope scope, CodeSpan span, Value value);
        public abstract bool IsEqualTo(Value other);
        public abstract Value CloneNonLiteral();

        protected Value(DataType dataType, bool literal)
        {
            _dataType = dataType;
            _literal = literal;
        }

        public DataType DataType
        {
            get { return _dataType; }
        }

        public bool IsLiteral => _literal;

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
                    return new NumberValue(dataType, null, literal: false);
                case ValType.String:
                    return new StringValue(dataType, null, literal: false);
                case ValType.Char:
                    return new CharValue(dataType, null, literal: false);
                case ValType.Enum:
                    return new EnumValue(dataType, null, null, literal: false);
                case ValType.Date:
                    return new DateValue(dataType, null, literal: false);
                case ValType.Time:
                    return new TimeValue(dataType, null, literal: false);
                case ValType.Table:
                    return new TableValue(dataType, null, literal: false);
                case ValType.IndRel:
                    return new IndRelValue(dataType, null, literal: false);
                case ValType.Variant:
                    return new VariantValue();
                case ValType.Interface:
                    return new InterfaceValue();
                default:
                    return Value.Void;
            }
        }

        public virtual Value Multiply(CAScope scope, CodeSpan span, Value rightValue)
        {
            scope.CodeAnalyzer.ReportError(span, CAError.CA10050, "Multiplication");	// {0} cannot be used with this value.
            return this;
        }

        public virtual Value Divide(CAScope scope, CodeSpan span, Value rightValue)
        {
            scope.CodeAnalyzer.ReportError(span, CAError.CA10050, "Division");	// {0} cannot be used with this value.
            return this;
        }

        public virtual Value ModulusDivide(CAScope scope, CodeSpan span, Value rightValue)
        {
            scope.CodeAnalyzer.ReportError(span, CAError.CA10050, "Modulus division");	// {0} cannot be used with this value.
            return this;
        }

        public virtual Value Add(CAScope scope, CodeSpan span, Value rightValue)
        {
            scope.CodeAnalyzer.ReportError(span, CAError.CA10050, "Addition");	// {0} cannot be used with this value.
            return this;
        }

        public virtual Value Subtract(CAScope scope, CodeSpan span, Value rightValue)
        {
            scope.CodeAnalyzer.ReportError(span, CAError.CA10050, "Subtraction");	// {0} cannot be used with this value.
            return this;
        }

        public virtual Value Invert(CAScope scope, CodeSpan span)
        {
            scope.CodeAnalyzer.ReportError(span, CAError.CA10050, "Unary minus");	// {0} cannot be used with this value.
            return this;
        }

        public virtual Value CompareEqual(CAScope scope, CodeSpan span, Value rightValue)
        {
            scope.CodeAnalyzer.ReportError(span, CAError.CA10050, "Equals Comparison");	// {0} cannot be used with this value.
            return this;
        }

        public virtual Value CompareNotEqual(CAScope scope, CodeSpan span, Value rightValue)
        {
            scope.CodeAnalyzer.ReportError(span, CAError.CA10050, "Not-equal comparison");	// {0} cannot be used with this value.
            return this;
        }

        public virtual Value CompareLessThan(CAScope scope, CodeSpan span, Value rightValue)
        {
            scope.CodeAnalyzer.ReportError(span, CAError.CA10050, "Less-than comparison");	// {0} cannot be used with this value.
            return this;
        }

        public virtual Value CompareGreaterThan(CAScope scope, CodeSpan span, Value rightValue)
        {
            scope.CodeAnalyzer.ReportError(span, CAError.CA10050, "Greater-than comparison");	// {0} cannot be used with this value.
            return this;
        }

        public virtual Value CompareLessEqual(CAScope scope, CodeSpan span, Value rightValue)
        {
            scope.CodeAnalyzer.ReportError(span, CAError.CA10050, "Less-than-or-equal-to comparison");	// {0} cannot be used with this value.
            return this;
        }

        public virtual Value CompareGreaterEqual(CAScope scope, CodeSpan span, Value rightValue)
        {
            scope.CodeAnalyzer.ReportError(span, CAError.CA10050, "Greater-than-or-equal-to comparison");	// {0} cannot be used with this value.
            return this;
        }

        public virtual Value CompareLike(CAScope scope, CodeSpan span, Value rightValue)
        {
            scope.CodeAnalyzer.ReportError(span, CAError.CA10050, "Like comparison");    // {0} cannot be used with this value.
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

        public enum ConversionMethod
        {
            Assignment,         // Assignment operator (=, +=, -=, *=, /=, %=)
            Math,               // Math operator (+, -, *, /, %)
            Cast,               // Typecast
            Comparison,         // Comparison operator (==, !=, <, >, <=, >=)
            Return,             // Return statement
            FunctionArgument    // Passed as an argument for a function
        }

        public virtual void CheckTypeConversion(CAScope scope, CodeSpan span, DataType toDataType, ConversionMethod method)
        {
        }

        public virtual void CheckTypeMath(CAScope scope, CodeSpan span, Value rightValue)
        {
        }
    }
}

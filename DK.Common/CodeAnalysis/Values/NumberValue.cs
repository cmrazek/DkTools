using DK.Code;
using DK.Modeling;

namespace DK.CodeAnalysis.Values
{
    class NumberValue : Value
    {
        private decimal? _num;

        public NumberValue(DataType dataType, decimal? number)
            : base(dataType)
        {
            _num = number;
        }

        public override string ToString() => _num.HasValue ? _num.Value.ToString() : "(null)";

        public override Value Multiply(CAScope scope, CodeSpan span, Value rightValue)
        {
            if (_num.HasValue)
            {
                var right = rightValue.ToNumber(scope, span);
                if (right.HasValue) return new NumberValue(DataType, _num.Value * right.Value);
            }

            return new NumberValue(DataType, null);
        }

        public override Value Divide(CAScope scope, CodeSpan span, Value rightValue)
        {
            if (_num.HasValue)
            {
                var right = rightValue.ToNumber(scope, span);
                if (right.HasValue)
                {
                    if (right.Value == 0)
                    {
                        scope.CodeAnalyzer.ReportError(span, CAError.CA10051);	// Division by zero.
                    }
                    else
                    {
                        return new NumberValue(DataType, _num.Value / right.Value);
                    }
                }
            }

            return new NumberValue(DataType, null);
        }

        public override Value ModulusDivide(CAScope scope, CodeSpan span, Value rightValue)
        {
            if (_num.HasValue)
            {
                var right = rightValue.ToNumber(scope, span);
                if (right.HasValue)
                {
                    if (right.Value == 0)
                    {
                        scope.CodeAnalyzer.ReportError(span, CAError.CA10051);	// Division by zero.
                    }
                    else
                    {
                        return new NumberValue(DataType, _num.Value % right.Value);
                    }
                }
            }

            return new NumberValue(DataType, null);
        }

        public override Value Add(CAScope scope, CodeSpan span, Value rightValue)
        {
            if (_num.HasValue)
            {
                var right = rightValue.ToNumber(scope, span);
                if (right.HasValue) return new NumberValue(DataType, _num.Value + right.Value);
            }

            return new NumberValue(DataType, null);
        }

        public override Value Subtract(CAScope scope, CodeSpan span, Value rightValue)
        {
            if (_num.HasValue)
            {
                var right = rightValue.ToNumber(scope, span);
                if (right.HasValue) return new NumberValue(DataType, _num.Value - right.Value);
            }

            return new NumberValue(DataType, null);
        }

        public override Value Invert(CAScope scope, CodeSpan span)
        {
            if (_num.HasValue)
            {
                return new NumberValue(DataType, -_num.Value);
            }

            return new NumberValue(DataType, null);
        }

        public override Value CompareEqual(CAScope scope, CodeSpan span, Value rightValue)
        {
            if (_num.HasValue)
            {
                var right = rightValue.ToNumber(scope, span);
                if (right.HasValue) return new NumberValue(DataType.Int, _num.Value == right.Value ? 1 : 0);
            }

            return new NumberValue(DataType.Int, null);
        }

        public override Value CompareNotEqual(CAScope scope, CodeSpan span, Value rightValue)
        {
            if (_num.HasValue)
            {
                var right = rightValue.ToNumber(scope, span);
                if (right.HasValue) return new NumberValue(DataType.Int, _num.Value != right.Value ? 1 : 0);
            }

            return new NumberValue(DataType.Int, null);
        }

        public override Value CompareLessThan(CAScope scope, CodeSpan span, Value rightValue)
        {
            if (_num.HasValue)
            {
                var right = rightValue.ToNumber(scope, span);
                if (right.HasValue) return new NumberValue(DataType.Int, _num.Value < right.Value ? 1 : 0);
            }

            return new NumberValue(DataType.Int, null);
        }

        public override Value CompareGreaterThan(CAScope scope, CodeSpan span, Value rightValue)
        {
            if (_num.HasValue)
            {
                var right = rightValue.ToNumber(scope, span);
                if (right.HasValue) return new NumberValue(DataType.Int, _num.Value > right.Value ? 1 : 0);
            }

            return new NumberValue(DataType.Int, null);
        }

        public override Value CompareLessEqual(CAScope scope, CodeSpan span, Value rightValue)
        {
            if (_num.HasValue)
            {
                var right = rightValue.ToNumber(scope, span);
                if (right.HasValue) return new NumberValue(DataType.Int, _num.Value <= right.Value ? 1 : 0);
            }

            return new NumberValue(DataType.Int, null);
        }

        public override Value CompareGreaterEqual(CAScope scope, CodeSpan span, Value rightValue)
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

        public override decimal? ToNumber(CAScope scope, CodeSpan span)
        {
            return _num;
        }

        public override string ToStringValue(CAScope scope, CodeSpan span)
        {
            if (_num.HasValue)
            {
                return _num.Value.ToString();
            }
            return null;
        }

        public override DkDate? ToDate(CAScope scope, CodeSpan span)
        {
            if (_num.HasValue)
            {
                if (_num.Value < 0 || _num.Value > 65535)
                {
                    scope.CodeAnalyzer.ReportError(span, CAError.CA10052);	// Date math results in an out-of-bounds value.
                    return null;
                }
                return new DkDate((int)_num.Value);
            }

            return null;
        }

        public override DkTime? ToTime(CAScope scope, CodeSpan span)
        {
            if (_num.HasValue)
            {
                if (_num.Value < 0 || _num.Value > DkTime.MaxValue)
                {
                    scope.CodeAnalyzer.ReportError(span, CAError.CA10054);	// Time math results in an out-of-bounds value.
                    return null;
                }

                return new DkTime((int)_num.Value);
            }

            return null;
        }

        public override char? ToChar(CAScope scope, CodeSpan span)
        {
            if (_num.HasValue)
            {
                if (_num.Value < 0 || _num.Value > 65535)
                {
                    scope.CodeAnalyzer.ReportError(span, CAError.CA10056);	// Char math results in an out-of-bounds value.
                    return null;
                }

                return (char)_num.Value;
            }

            return null;
        }

        public override Value Convert(CAScope scope, CodeSpan span, Value value)
        {
            return new NumberValue(DataType, value.ToNumber(scope, span));
        }

        public override bool IsEqualTo(Value other)
        {
            if (!_num.HasValue) return false;
            var o = other as NumberValue;
            if (o == null || !o._num.HasValue) return false;
            return _num.Value == o._num.Value;
        }

        public override void CheckTypeConversion(CAScope scope, CodeSpan span, DataType toDataType, ConversionMethod method)
        {
            base.CheckTypeConversion(scope, span, toDataType, method);

            switch (method)
            {
                case ConversionMethod.Assignment:
                case ConversionMethod.FunctionArgument:
                    if (toDataType.IsNumeric && DataType.Scale != 0 && toDataType.Scale != 0)
                    {
                        bool conversionWarning = false;

                        if
                        (
                            (DataType.Signed && !toDataType.Signed) ||
                            ((_num ?? DataType.MaxNumericValue) > toDataType.MaxNumericValue) ||
                            ((_num ?? DataType.MinNumericValue) < toDataType.MinNumericValue)
                        )
                        {
                            conversionWarning = true;
                        }

                        if (toDataType.IsInteger)
                        {
                            if (DataType.IsInteger)
                            {
                                // Integer to integer
                            }
                            else
                            {
                                // Numeric to integer
                                if (DataType.Precision > 0)
                                {
                                    conversionWarning = true;
                                }
                            }
                        }
                        else
                        {
                            if (DataType.IsInteger)
                            {
                                // Integer to numeric
                            }
                            else
                            {
                                // Numeric to numeric
                                if (DataType.Scale != 0 && toDataType.Scale != 0 &&
                                (
                                    DataType.Scale > toDataType.Scale ||
                                    DataType.Precision > toDataType.Precision
                                ))
                                {
                                    conversionWarning = true;
                                }
                            }
                        }

                        if (conversionWarning)
                        {
                            scope.CodeAnalyzer.ReportError(span, CAError.CA00108, DataType.DisplayName, toDataType.DisplayName);    // Converting from '{0}' to '{1}'; possible data loss
                        }
                    }
                    break;
            }
        }
    }
}

using DK.Code;
using DK.CodeAnalysis.Statements;
using DK.CodeAnalysis.Values;
using DK.Modeling;
using System;

/*
 * Precedence:
 * 100	[]
 * 28   - (unary)
 * 26	* / %
 * 24	+ -
 * 22	< > <= >=
 * 20	== !=
 * 18	in
 * 16	and
 * 14	or
 * 12	? :
 * 10	conditional result
 * 7	= *= /= %= += -=
 * 2	unresolved nodes
 * */

namespace DK.CodeAnalysis.Nodes
{
    enum OperatorType
    {
        None,
        Assign,
        AssignMultiply,
        AssignDivide,
        AssignModulus,
        AssignAdd,
        AssignSubtract,
        CompareEqual,
        CompareNotEqual,
        CompareLessThan,
        CompareGreaterThan,
        CompareLessThanOrEqual,
        CompareGreaterThanOrEqual,
        And,
        Or,
        Multiply,
        Divide,
        Modulus,
        Add,
        Subtract,
        Negate,
        In,
        Like,
        Ternary
    }

    class OperatorNode : Node
    {
        private OperatorType _type;
        private int _prec;
        private Node _leftNode;
        private Node _rightNode;

        public OperatorNode(Statement stmt, CodeSpan span, OperatorType type, Node leftNode, Node rightNode)
            : base(stmt, null, span)
        {
            _type = type;
            _prec = OperatorPrecedence(type);
            _leftNode = leftNode;
            _rightNode = rightNode;

            if (_leftNode != null) _leftNode.Parent = this;
            if (_rightNode != null) _rightNode.Parent = this;
        }

        // Even numbers are left-to-right, odd are right-to-left
        public const int MultiplyPrecedence = 26;
        public const int AddPrecedence = 24;
        public const int CompareLessGreaterPrecedence = 22;
        public const int CompareEqualPrecedence = 20;
        public const int TernaryPrecedence = 12;
        public const int LikePrecedence = 18;
        public const int AndPrecedence = 16;
        public const int OrPrecedence = 14;
        public const int AssignPrecedence = 7;

        public static int OperatorPrecedence(OperatorType op)
        {
            switch (op)
            {
                case OperatorType.Multiply:
                case OperatorType.Divide:
                case OperatorType.Modulus:
                case OperatorType.Negate:
                    return MultiplyPrecedence;
                case OperatorType.Add:
                case OperatorType.Subtract:
                    return AddPrecedence;
                case OperatorType.CompareLessThan:
                case OperatorType.CompareGreaterThan:
                case OperatorType.CompareLessThanOrEqual:
                case OperatorType.CompareGreaterThanOrEqual:
                    return CompareLessGreaterPrecedence;
                case OperatorType.CompareEqual:
                case OperatorType.CompareNotEqual:
                    return CompareEqualPrecedence;
                case OperatorType.In:
                case OperatorType.Like:
                    return LikePrecedence;
                case OperatorType.And:
                    return AndPrecedence;
                case OperatorType.Or:
                    return OrPrecedence;
                case OperatorType.Ternary:
                    return TernaryPrecedence;
                case OperatorType.Assign:
                case OperatorType.AssignMultiply:
                case OperatorType.AssignDivide:
                case OperatorType.AssignModulus:
                case OperatorType.AssignAdd:
                case OperatorType.AssignSubtract:
                    return AssignPrecedence;
                default:
                    return 0;
            }
        }

        public static string OperatorText(OperatorType op)
        {
            switch (op)
            {
                case OperatorType.Assign: return "=";
                case OperatorType.AssignMultiply: return "*=";
                case OperatorType.AssignDivide: return "/=";
                case OperatorType.AssignModulus: return "%=";
                case OperatorType.AssignAdd: return "+=";
                case OperatorType.AssignSubtract: return "-=";
                case OperatorType.CompareEqual: return "==";
                case OperatorType.CompareNotEqual: return "!=";
                case OperatorType.CompareLessThan: return "<";
                case OperatorType.CompareGreaterThan: return ">";
                case OperatorType.CompareLessThanOrEqual: return "<=";
                case OperatorType.CompareGreaterThanOrEqual: return ">=";
                case OperatorType.And: return "and";
                case OperatorType.Or: return "or";
                case OperatorType.Multiply: return "*";
                case OperatorType.Divide: return "/";
                case OperatorType.Modulus: return "%";
                case OperatorType.Add: return "+";
                case OperatorType.Subtract: return "-";
                case OperatorType.Negate: return "-";
                case OperatorType.In: return "in";
                case OperatorType.Like: return "like";
                case OperatorType.Ternary: return "?";
                default: throw new InvalidOperatorTypeException();
            }
        }

        public static OperatorType OperatorTextToType(string text)
        {
            switch (text)
            {
                case "=": return OperatorType.Assign;
                case "*=": return OperatorType.AssignMultiply;
                case "/=": return OperatorType.AssignDivide;
                case "%=": return OperatorType.AssignModulus;
                case "+=": return OperatorType.AssignAdd;
                case "-=": return OperatorType.AssignSubtract;
                case "==": return OperatorType.CompareEqual;
                case "!=": return OperatorType.CompareNotEqual;
                case "<": return OperatorType.CompareLessThan;
                case ">": return OperatorType.CompareGreaterThan;
                case "<=": return OperatorType.CompareLessThanOrEqual;
                case ">=": return OperatorType.CompareGreaterThanOrEqual;
                case "and": case "&&": return OperatorType.And;
                case "or": case "||": return OperatorType.Or;
                case "*": return OperatorType.Multiply;
                case "/": return OperatorType.Divide;
                case "%": return OperatorType.Modulus;
                case "+": return OperatorType.Add;
                case "-": return OperatorType.Subtract;
                case "in": return OperatorType.In;
                case "like": return OperatorType.Like;
                case "?": return OperatorType.Ternary;
                default: return OperatorType.None;
            }
        }

        public override bool IsReportable => false;

        public override int Precedence
        {
            get
            {
                return _prec;
            }
        }

        public override void Execute(CAScope scope)
        {
            switch (_type)
            {
                case OperatorType.Multiply:
                case OperatorType.Divide:
                case OperatorType.Modulus:
                case OperatorType.Add:
                case OperatorType.Subtract:
                    ExecuteMath(scope);
                    break;

                case OperatorType.Negate:
                    ExecuteMinus(scope);
                    break;

                case OperatorType.CompareLessThan:
                case OperatorType.CompareGreaterThan:
                case OperatorType.CompareLessThanOrEqual:
                case OperatorType.CompareGreaterThanOrEqual:
                case OperatorType.CompareEqual:
                case OperatorType.CompareNotEqual:
                case OperatorType.And:
                case OperatorType.Or:
                case OperatorType.Like:
                    ExecuteComparison(scope);
                    break;

                case OperatorType.Assign:
                case OperatorType.AssignMultiply:
                case OperatorType.AssignDivide:
                case OperatorType.AssignModulus:
                case OperatorType.AssignAdd:
                case OperatorType.AssignSubtract:
                    ExecuteAssignment(scope);
                    break;

                default:
                    throw new InvalidOperatorTypeException();
            }
        }

        public override Value ReadValue(CAScope scope)
        {
            switch (_type)
            {
                case OperatorType.Multiply:
                case OperatorType.Divide:
                case OperatorType.Modulus:
                case OperatorType.Add:
                case OperatorType.Subtract:
                    return ExecuteMath(scope);

                case OperatorType.Negate:
                    return ExecuteMinus(scope);

                case OperatorType.CompareLessThan:
                case OperatorType.CompareGreaterThan:
                case OperatorType.CompareLessThanOrEqual:
                case OperatorType.CompareGreaterThanOrEqual:
                case OperatorType.CompareEqual:
                case OperatorType.CompareNotEqual:
                case OperatorType.And:
                case OperatorType.Or:
                case OperatorType.Like:
                    return ExecuteComparison(scope);

                case OperatorType.Assign:
                case OperatorType.AssignMultiply:
                case OperatorType.AssignDivide:
                case OperatorType.AssignModulus:
                case OperatorType.AssignAdd:
                case OperatorType.AssignSubtract:
                    return ExecuteAssignment(scope);

                default:
                    throw new InvalidOperatorTypeException();
            }
        }

        private Value ExecuteMath(CAScope scope)	// * / % + -
        {
            if (_leftNode == null) ReportError(Span, CAError.CA10007, OperatorText(_type));			// Operator '{0}' expects value on left.
            else if (_rightNode == null) ReportError(Span, CAError.CA10008, OperatorText(_type));	// Operator '{0}' expects value on right.
            if (_leftNode != null && _rightNode != null)
            {
                var leftValue = _leftNode.ReadValue(scope);
                var rightScope = scope.Clone();
                var rightValue = _rightNode.ReadValue(rightScope);
                scope.Merge(rightScope);
                if (leftValue.IsVoid) _leftNode.ReportError(_leftNode.Span, CAError.CA10007, OperatorText(_type));		// Operator '{0}' expects value on left.
                else if (rightValue.IsVoid) _rightNode.ReportError(_rightNode.Span, CAError.CA10008, OperatorText(_type));    // Operator '{0}' expects value on right.

                leftValue.CheckTypeMath(scope, _leftNode.Span.Envelope(_rightNode.Span), rightValue);

                Value result = null;
                switch (_type)
                {
                    case OperatorType.Multiply:
                        result = leftValue.Multiply(scope, Span, rightValue).CloneNonLiteral();
                        break;
                    case OperatorType.Divide:
                        result = leftValue.Divide(scope, Span, rightValue).CloneNonLiteral();
                        break;
                    case OperatorType.Modulus:
                        result = leftValue.ModulusDivide(scope, Span, rightValue).CloneNonLiteral();
                        break;
                    case OperatorType.Add:
                        result = leftValue.Add(scope, Span, rightValue).CloneNonLiteral();
                        break;
                    case OperatorType.Subtract:
                        result = leftValue.Subtract(scope, Span, rightValue).CloneNonLiteral();
                        break;
                    default:
                        throw new InvalidOperatorTypeException();
                }

                return result;
            }
            else
            {
                Value resultValue = Value.Void;
                if (_leftNode != null && _rightNode == null) resultValue = _leftNode.ReadValue(scope);
                else if (_leftNode == null && _rightNode != null) resultValue = _rightNode.ReadValue(scope);

                return resultValue;
            }
        }

        private Value ExecuteMinus(CAScope scope)
        {
            if (_rightNode == null)
            {
                ReportError(Span, CAError.CA10008, OperatorText(_type));   // Operator '{0}' expects value on right.
                return Value.Void;
            }

            var rightValue = _rightNode.ReadValue(scope).Invert(scope, Span).CloneNonLiteral();
            if (rightValue.IsVoid) _rightNode.ReportError(_rightNode.Span, CAError.CA10008, OperatorText(_type));	// Operator '{0}' expects value on right.
            return rightValue;
        }

        private Value ExecuteComparison(CAScope scope)	// < > <= >= == !=
        {
            if (_leftNode == null) ReportError(Span, CAError.CA10007, OperatorText(_type));	// Operator '{0}' expects value on left.
            else if (_rightNode == null) ReportError(Span, CAError.CA10008, OperatorText(_type));	// Operator '{0}' expects value on right.
            if (_leftNode != null && _rightNode != null)
            {
                var leftValue = _leftNode.ReadValue(scope);
                var rightScope = scope.Clone();
                var rightValue = _rightNode.ReadValue(rightScope);
                scope.Merge(rightScope);
                if (leftValue.IsVoid) _leftNode.ReportError(_leftNode.Span, CAError.CA10007, OperatorText(_type));		// Operator '{0}' expects value on left.
                else if (rightValue.IsVoid) _rightNode.ReportError(_rightNode.Span, CAError.CA10008, OperatorText(_type));    // Operator '{0}' expects value on right.

                var leftDataType = _leftNode.DataType;

                Value result = null;
                switch (_type)
                {
                    case OperatorType.CompareEqual:
                        if (leftDataType != null) rightValue.CheckTypeConversion(scope, _rightNode.Span, leftDataType, Value.ConversionMethod.Comparison);
                        result = leftValue.CompareEqual(scope, Span, rightValue).CloneNonLiteral();
                        break;
                    case OperatorType.CompareNotEqual:
                        if (leftDataType != null) rightValue.CheckTypeConversion(scope, _rightNode.Span, leftDataType, Value.ConversionMethod.Comparison);
                        result = leftValue.CompareNotEqual(scope, Span, rightValue).CloneNonLiteral();
                        break;
                    case OperatorType.CompareLessThan:
                        if (leftDataType != null) rightValue.CheckTypeConversion(scope, _rightNode.Span, leftDataType, Value.ConversionMethod.Comparison);
                        result = leftValue.CompareLessThan(scope, Span, rightValue).CloneNonLiteral();
                        break;
                    case OperatorType.CompareGreaterThan:
                        if (leftDataType != null) rightValue.CheckTypeConversion(scope, _rightNode.Span, leftDataType, Value.ConversionMethod.Comparison);
                        result = leftValue.CompareGreaterThan(scope, Span, rightValue).CloneNonLiteral();
                        break;
                    case OperatorType.CompareLessThanOrEqual:
                        if (leftDataType != null) rightValue.CheckTypeConversion(scope, _rightNode.Span, leftDataType, Value.ConversionMethod.Comparison);
                        result = leftValue.CompareLessEqual(scope, Span, rightValue).CloneNonLiteral();
                        break;
                    case OperatorType.CompareGreaterThanOrEqual:
                        if (leftDataType != null) rightValue.CheckTypeConversion(scope, _rightNode.Span, leftDataType, Value.ConversionMethod.Comparison);
                        result = leftValue.CompareGreaterEqual(scope, Span, rightValue).CloneNonLiteral();
                        break;
                    case OperatorType.And:
                        {
                            var left = leftValue.ToNumber(scope, Span);
                            var right = rightValue.ToNumber(scope, Span);
                            if (left.HasValue && right.HasValue) result = new NumberValue(DataType.Int, left.Value != 0 && right.Value != 0 ? 1 : 0, literal: false);
                            else result = new NumberValue(DataType.Int, number: null, literal: false);
                        }
                        break;
                    case OperatorType.Or:
                        {
                            var left = leftValue.ToNumber(scope, Span);
                            var right = rightValue.ToNumber(scope, Span);
                            if (left.HasValue && right.HasValue) result = new NumberValue(DataType.Int, left.Value != 0 || right.Value != 0 ? 1 : 0, literal: false);
                            else result = new NumberValue(DataType.Int, number: null, literal: false);
                        }
                        break;
                    case OperatorType.Like:
                        if (leftDataType != null) rightValue.CheckTypeConversion(scope, _rightNode.Span, leftDataType, Value.ConversionMethod.Comparison);
                        result = rightValue.CompareLike(scope, Span, rightValue).CloneNonLiteral();
                        break;
                    default:
                        throw new InvalidOperatorTypeException();
                }

                return result;
            }
            else
            {
                Value resultValue = Value.Void;
                if (_leftNode != null && _rightNode == null) resultValue = _leftNode.ReadValue(scope);
                else if (_leftNode == null && _rightNode != null) resultValue = _rightNode.ReadValue(scope);

                return resultValue.CloneNonLiteral();
            }
        }

        private Value ExecuteAssignment(CAScope scope)	// = *= /= %= += -=
        {
            if (_leftNode == null) ReportError(Span, CAError.CA10100, OperatorText(_type));			// Operator '{0}' expects assignable value on left.
            else if (_rightNode == null) ReportError(Span, CAError.CA10008, OperatorText(_type));	// Operator '{0}' expects value on right.
            if (_leftNode != null && _rightNode != null)
            {
                Value leftValue = null;
                if (_type != OperatorType.Assign)
                {
                    var leftScope = scope.Clone();
                    leftValue = _leftNode.ReadValue(leftScope);
                    scope.Merge(leftScope);
                }

                var rightScope = scope.Clone();
                var rightValue = _rightNode.ReadValue(rightScope);
                scope.Merge(rightScope);

                if (!_leftNode.CanAssignValue(scope)) _leftNode.ReportError(_leftNode.Span, CAError.CA10100, OperatorText(_type));				// Operator '{0}' expects assignable value on left.
                else if (rightValue.IsVoid) _rightNode.ReportError(_rightNode.Span, CAError.CA10008, OperatorText(_type));                // Operator '{0}' expects value on right.

                var leftDataType = _leftNode.DataType;
                if (leftDataType != null) rightValue.CheckTypeConversion(scope, _rightNode.Span.Envelope(_leftNode.Span), leftDataType, Value.ConversionMethod.Assignment);

                Value result = null;
                switch (_type)
                {
                    case OperatorType.Assign:
                        result = rightValue.CloneNonLiteral();
                        break;
                    case OperatorType.AssignMultiply:
                        result = leftValue.Multiply(scope, Span, rightValue).CloneNonLiteral();
                        break;
                    case OperatorType.AssignDivide:
                        result = leftValue.Divide(scope, Span, rightValue).CloneNonLiteral();
                        break;
                    case OperatorType.AssignModulus:
                        result = leftValue.ModulusDivide(scope, Span, rightValue).CloneNonLiteral();
                        break;
                    case OperatorType.AssignAdd:
                        result = leftValue.Add(scope, Span, rightValue).CloneNonLiteral();
                        break;
                    case OperatorType.AssignSubtract:
                        result = leftValue.Subtract(scope, Span, rightValue).CloneNonLiteral();
                        break;
                    default:
                        throw new InvalidOperatorTypeException();
                }

                _leftNode.WriteValue(scope, result);
                _leftNode.IsReportable = false;

                if (scope.InWhereClause)
                {
                    ReportError(Span, CAError.CA10073); // Assignment in select where clause.
                }

                return result;
            }
            else
            {
                Value resultValue = Value.Void;
                if (_leftNode != null && _rightNode == null) resultValue = _leftNode.ReadValue(scope);
                else if (_leftNode == null && _rightNode != null) resultValue = _rightNode.ReadValue(scope);

                return resultValue.CloneNonLiteral();
            }
        }

        public override DataType DataType
        {
            get
            {
                switch (_type)
                {
                    case OperatorType.Assign:
                    case OperatorType.AssignMultiply:
                    case OperatorType.AssignDivide:
                    case OperatorType.AssignModulus:
                    case OperatorType.AssignAdd:
                    case OperatorType.AssignSubtract:
                        return _leftNode?.DataType;
                    case OperatorType.Multiply:
                    case OperatorType.Divide:
                    case OperatorType.Modulus:
                    case OperatorType.Add:
                    case OperatorType.Subtract:
                        return _leftNode?.DataType ?? _rightNode?.DataType;
                    case OperatorType.CompareEqual:
                    case OperatorType.CompareNotEqual:
                    case OperatorType.CompareLessThan:
                    case OperatorType.CompareGreaterThan:
                    case OperatorType.CompareLessThanOrEqual:
                    case OperatorType.CompareGreaterThanOrEqual:
                    case OperatorType.And:
                    case OperatorType.Or:
                    case OperatorType.In:
                    case OperatorType.Like:
                        return DataType.Int;
                    case OperatorType.Negate:
                        return _rightNode?.DataType;
                    default:
                        return null;
                }
            }
        }

        public override string ToString()
        {
            switch (_type)
            {
                case OperatorType.Multiply:
                case OperatorType.Divide:
                case OperatorType.Modulus:
                case OperatorType.Add:
                case OperatorType.Subtract:
                case OperatorType.CompareLessThan:
                case OperatorType.CompareGreaterThan:
                case OperatorType.CompareLessThanOrEqual:
                case OperatorType.CompareGreaterThanOrEqual:
                case OperatorType.CompareEqual:
                case OperatorType.CompareNotEqual:
                case OperatorType.And:
                case OperatorType.Or:
                case OperatorType.Like:
                case OperatorType.Assign:
                case OperatorType.AssignMultiply:
                case OperatorType.AssignDivide:
                case OperatorType.AssignModulus:
                case OperatorType.AssignAdd:
                case OperatorType.AssignSubtract:
                    return $"{_leftNode?.ToString()} {OperatorText(_type)} {_rightNode?.ToString()}";

                case OperatorType.Negate:
                    return $"-{_rightNode?.ToString()}";

                default:
                    return OperatorText(_type);
            }
        }

        public OperatorType OperatorType => _type;
    }

    class InvalidOperatorTypeException : Exception { }
}

using DK.Code;
using DK.CodeAnalysis.Statements;
using DK.CodeAnalysis.Values;
using DK.Modeling;
using System;
using System.Linq;
using System.Runtime.InteropServices;

namespace DK.CodeAnalysis.Nodes
{
    class ConditionalNode : Node
    {
        private Node _leftExp;
        private Node _trueExp;
        private Node _falseExp;
        private CodeSpan _opSpan;

        private ConditionalNode(Statement stmt, CodeSpan opSpan, Node leftExp, Node trueExp, Node falseExp)
            : base(stmt, null, opSpan)
        {
            _opSpan = opSpan;
            _leftExp = leftExp ?? throw new ArgumentNullException(nameof(leftExp));
            _trueExp = trueExp;
            _falseExp = falseExp;

            _leftExp.Parent = this;
            if (_trueExp != null) _trueExp.Parent = this;
            if (_falseExp != null) _falseExp.Parent = this;
        }

        private static string[] s_stopStrings = new string[] { "?", ":" };

        public override string ToString() => new string[] { _trueExp.ToString(), ":", _falseExp?.ToString() }.Combine(" ");

        public static ConditionalNode Read(ReadParams p, DataType refDataType, CodeSpan opSpan, Node leftNode)
        {
            var code = p.Code;

            var trueExp = ExpressionNode.Read(p, refDataType, OperatorNode.TernaryPrecedence);
            if (trueExp == null)
            {
                p.Statement.ReportError(new CodeSpan(code.Position, code.Position + 1), CAError.CA10042);   // Expected value to follow conditional '?'.
                return new ConditionalNode(p.Statement, opSpan, leftNode, null, null);
            }

            if (!code.ReadExact(':'))
            {
                p.Statement.ReportError(new CodeSpan(code.Position, code.Position + 1), CAError.CA10041);   // Expected ':' to follow conditional result.
                return new ConditionalNode(p.Statement, opSpan, leftNode, trueExp, null);
            }

            var falseExp = ExpressionNode.Read(p, refDataType, OperatorNode.TernaryPrecedence);
            if (falseExp == null)
            {
                p.Statement.ReportError(new CodeSpan(code.Position, code.Position + 1), CAError.CA10043);   // Expected value to follow conditional ':'.
                return new ConditionalNode(p.Statement, opSpan, leftNode, trueExp, null);
            }

            return new ConditionalNode(p.Statement, opSpan, leftNode, trueExp, falseExp);
        }

        public override int Precedence
        {
            get
            {
                return 12;
            }
        }

        public override Value ReadValue(CAScope scope)
        {
            if (_trueExp == null || _falseExp == null) return Value.Void;

            Value result = null;

            if (!(Parent is BracketsNode) && !(Parent is FunctionCallNode))
            {
                ReportError(CAError.CA10071);   // Conditional statements should be wrapped in brackets to avoid compiler bugs.
            }

            var leftValue = _leftExp.ReadValue(scope);
            if (leftValue.IsTrue)
            {
                result = _trueExp.ReadValue(scope);
            }
            else if (leftValue.IsFalse)
            {
                result = _falseExp.ReadValue(scope);
            }
            else
            {
                // Could execute either true or false
                var trueScope = scope.Clone();
                var trueResult = _trueExp.ReadValue(trueScope);

                var falseScope = scope.Clone();
                var falseResult = _falseExp.ReadValue(falseScope);

                if (trueResult != null && !trueResult.IsVoid) result = Value.CreateUnknownFromDataType(trueResult.DataType);
                else if (falseResult != null && !falseResult.IsVoid) result = Value.CreateUnknownFromDataType(falseResult.DataType);

                scope.Merge(new CAScope[] { trueScope, falseScope });
            }

            if (result == null) result = Value.Void;
            return result;
        }
    }
}

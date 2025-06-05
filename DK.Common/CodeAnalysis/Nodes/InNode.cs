using DK.Code;
using DK.CodeAnalysis.Statements;
using DK.CodeAnalysis.Values;
using DK.Modeling;
using System;
using System.Collections.Generic;

namespace DK.CodeAnalysis.Nodes
{
    internal class InNode : Node
    {
        private Node _leftNode;
        private Node[] _expressions;

        private InNode(Statement stmt, CodeSpan span, Node leftNode, Node[] expressions)
            : base(stmt, DataType.Int, span)
        {
            _leftNode = leftNode ?? throw new ArgumentNullException(nameof(leftNode));
            _expressions = expressions;
        }

        public override bool IsReportable { get => true; set => base.IsReportable = value; }
        public override int Precedence => OperatorNode.LikePrecedence;

        public static InNode Read(ReadParams p, CodeSpan inKeywordSpan, Node leftNode)
        {
            var code = p.Code;
            var expList = new List<Node>();
            var gotError = false;

            if (code.ReadExact('('))
            {
                bool first = true;
                var lastSpan = code.Span;

                while (!code.EndOfFile)
                {
                    if (code.ReadExact(')'))
                    {
                        lastSpan = code.Span;
                        break;
                    }

                    if (first) first = false;
                    else
                    {
                        if (code.ReadExact(','))
                        {
                            lastSpan = code.Span;
                        }
                        else
                        {
                            p.CodeAnalyzer.ReportError(lastSpan, CAError.CA10131); // Expected ','.
                            gotError = true;
                            break;
                        }
                    }

                    var exp = ExpressionNode.Read(p, leftNode.DataType);
                    if (exp != null)
                    {
                        expList.Add(exp);
                        lastSpan = exp.Span;
                    }
                    else
                    {
                        p.CodeAnalyzer.ReportError(lastSpan, CAError.CA10132); // Expected expression.
                        gotError = true;
                        break;
                    }
                }

                if (!gotError && expList.Count == 0)
                {
                    p.CodeAnalyzer.ReportError(new CodeSpan(inKeywordSpan.Start, code.Position), CAError.CA10133);  // 'in' operator requires at least 1 expression.
                }

                return new InNode(p.Statement, leftNode.Span.Envelope(lastSpan), leftNode, expList.ToArray());
            }
            else
            {
                p.CodeAnalyzer.ReportError(inKeywordSpan, CAError.CA10130);    // Expected '('.
                return new InNode(p.Statement, leftNode.Span.Envelope(inKeywordSpan), leftNode, null);
            }
        }

        public override void Execute(CAScope scope)
        {
            ReadValue(scope);
        }

        public override Value ReadValue(CAScope scope)
        {
            if (_leftNode == null || _expressions.Length == 0) return Value.Void;

            var leftValue = _leftNode.ReadValue(scope);
            var rightScope = scope.Clone();
            if (leftValue.IsVoid) _leftNode.ReportError(_leftNode.Span, CAError.CA10007, "like");        // Operator '{0}' expects value on left.

            var leftDataType = _leftNode.DataType;

            Value result = null;

            foreach (var itemNode in _expressions)
            {
                var itemValue = itemNode.ReadValue(rightScope);
                if (leftDataType != null) itemValue.CheckTypeConversion(scope, itemNode.Span, leftDataType, Value.ConversionMethod.Comparison);
                result = leftValue.CompareEqual(scope, Span, itemValue);
            }

            if (result == null) result = new NumberValue(DataType.Int, number: null, literal: false);

            scope.Merge(rightScope);
            return result;
        }

        public override void OnUsed(CAScope scope)
        {
            base.OnUsed(scope);

            _leftNode.OnUsed(scope);

            foreach (var exp in _expressions)
            {
                exp.OnUsed(scope);
            }
        }
    }
}

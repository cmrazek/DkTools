using DK.Code;
using DK.CodeAnalysis.Statements;
using DK.CodeAnalysis.Values;

namespace DK.CodeAnalysis.Nodes
{
    class UnknownNode : TextNode
    {
        CAError? _errorCode;
        object[] _errorArgs;

        public UnknownNode(Statement stmt, CodeSpan span, string text, CAError? errorCode = null, params object[] errorArgs)
            : base(stmt, null, span, text)
        {
            _errorCode = errorCode;
            _errorArgs = errorArgs;
        }

        public override bool IsReportable => false;
        public override string ToString() => $"unknown ({Text})";

        public override Value ReadValue(CAScope scope)
        {
            if (_errorCode.HasValue)
            {
                ReportError(Span, _errorCode.Value, _errorArgs);
                return Value.Void;
            }

            return base.ReadValue(scope);
        }

        public override void WriteValue(CAScope scope, Value value)
        {
            if (_errorCode.HasValue)
            {
                ReportError(Span, _errorCode.Value, _errorArgs);
            }

            base.WriteValue(scope, value);
        }

        public override void Execute(CAScope scope)
        {
            if (_errorCode.HasValue)
            {
                ReportError(Span, _errorCode.Value, _errorArgs);
            }

            base.Execute(scope);
        }
    }
}

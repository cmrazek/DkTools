using DK.Code;
using DK.CodeAnalysis.Nodes;

namespace DK.CodeAnalysis.Statements
{
    class FormatStatement : Statement
    {
        private Node _rowsExp;
        private Node _colsExp;
        private Node _genpagesExp;
        private Node _outfileExp;

        public override string ToString() => "format...";

        public FormatStatement(ReadParams p, CodeSpan keywordSpan)
            : base(p.CodeAnalyzer, keywordSpan)
        {
            p = p.Clone(this);
            var code = p.Code;
            var errorSpan = keywordSpan;

            while (!code.EndOfFile)
            {
                if (code.ReadExact(';')) break;

                switch (code.PeekWordR())
                {
                    case "rows":
                        errorSpan = code.MovePeekedSpan();
                        if (!code.ReadExact('='))
                        {
                            ReportError(errorSpan, CAError.CA10033);	// Expected '='.
                            return;
                        }
                        else errorSpan = code.Span;

                        _rowsExp = ExpressionNode.Read(p, null);
                        if (_rowsExp != null) errorSpan = _rowsExp.Span;
                        break;
                    case "cols":
                        errorSpan = code.MovePeekedSpan();
                        if (!code.ReadExact('='))
                        {
                            ReportError(errorSpan, CAError.CA10033);	// Expected '='.
                            return;
                        }
                        else errorSpan = code.Span;

                        _colsExp = ExpressionNode.Read(p, null);
                        if (_colsExp != null) errorSpan = _colsExp.Span;
                        break;
                    case "genpages":
                        errorSpan = code.MovePeekedSpan();
                        if (!code.ReadExact('='))
                        {
                            ReportError(errorSpan, CAError.CA10033);	// Expected '='.
                            return;
                        }
                        else errorSpan = code.Span;

                        _genpagesExp = ExpressionNode.Read(p, null);
                        if (_genpagesExp != null) errorSpan = _genpagesExp.Span;
                        break;
                    case "outfile":
                        errorSpan = code.MovePeekedSpan();
                        if (!code.ReadExact('='))
                        {
                            ReportError(errorSpan, CAError.CA10033);	// Expected '='.
                            return;
                        }
                        else errorSpan = code.Span;

                        _outfileExp = ExpressionNode.Read(p, null);
                        if (_outfileExp != null) errorSpan = _outfileExp.Span;
                        break;
                    default:
                        ReportError(errorSpan, CAError.CA10015);	// Expected ';'.
                        return;
                }
            }
        }

        public override void Execute(CAScope scope)
        {
            base.Execute(scope);

            if (_rowsExp != null) _rowsExp.ReadValue(scope);
            if (_colsExp != null) _colsExp.ReadValue(scope);
            if (_genpagesExp != null) _genpagesExp.ReadValue(scope);
            if (_outfileExp != null) _outfileExp.ReadValue(scope);
        }
    }
}

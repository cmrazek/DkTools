using DK.Code;
using DK.CodeAnalysis.Nodes;
using System.Collections.Generic;

namespace DK.CodeAnalysis.Statements
{
    abstract class Statement
    {
        private CodeAnalyzer _ca;
        private CodeSpan _span;
        private List<Statement> _onErrorBody;

        public Statement(CodeAnalyzer ca)
        {
            _ca = ca;
        }

        public Statement(CodeAnalyzer ca, CodeSpan span)
        {
            _ca = ca;
            _span = span;
        }

        public static Statement Read(ReadParams p)
        {
            p.Code.SkipWhiteSpace();
            if (p.Code.EndOfFile) return null;

            var word = p.Code.PeekWordR();
            if (!string.IsNullOrEmpty(word))
            {
                switch (word)
                {
                    case "break":
                        return new BreakStatement(p, p.Code.MovePeekedSpan());
                    case "center":
                        return new CenterStatement(p, p.Code.MovePeekedSpan());
                    case "col":
                    case "colff":
                        return new ColStatement(p, p.Code.MovePeekedSpan());
                    case "continue":
                        return new ContinueStatement(p, p.Code.MovePeekedSpan());
                    case "extract":
                        return new ExtractStatement(p, p.Code.MovePeekedSpan());
                    case "footer":
                        return new FooterStatement(p, p.Code.MovePeekedSpan());
                    case "for":
                        return new ForStatement(p, p.Code.MovePeekedSpan());
                    case "format":
                        return new FormatStatement(p, p.Code.MovePeekedSpan());
                    case "header":
                        return new HeaderStatement(p, p.Code.MovePeekedSpan());
                    case "if":
                        return new IfStatement(p, p.Code.MovePeekedSpan());
                    case "page":
                        return new PageStatement(p, p.Code.MovePeekedSpan());
                    case "return":
                        return new ReturnStatement(p, p.Code.MovePeekedSpan());
                    case "row":
                        return new RowStatement(p, p.Code.MovePeekedSpan());
                    case "select":
                        return new SelectStatement(p, p.Code.MovePeekedSpan());
                    case "switch":
                        return new SwitchStatement(p, p.Code.MovePeekedSpan());
                    case "while":
                        return new WhileStatement(p, p.Code.MovePeekedSpan());
                }
            }

            if (p.Code.ReadExact('{'))
            {
                return new ScopeStatement(p, p.Code.Span);
            }

            var stmt = new SimpleStatement(p.CodeAnalyzer);
            p = p.Clone(stmt);

            var exp = ExpressionNode.Read(p, refDataType: null, errorIfNothingFound: true);
            if (exp != null)
            {
                stmt.AddNode(exp);

                if (p.Code.ReadExactWholeWord("onerror"))
                {
                    stmt.ProcessOnError(p, p.Code.Span);
                }
                else
                {
                    if (p.Code.ReadStringLiteral())
                    {
                        stmt.AddColumnHeading(p.Code.Span);
                    }

                    if (!p.Code.ReadExact(';'))
                    {
                        p.CodeAnalyzer.ReportError(exp.Span.Last(3), CAError.CA10015);  // Expected ';'.
                    }
                }
            }

            if (stmt.NumChildren == 0) return null;
            return stmt;
        }

        public virtual void Execute(CAScope scope)
        {
            if (scope.Returned == TriState.True ||
                scope.Breaked == TriState.True ||
                scope.Continued == TriState.True)
            {
                if (scope.UnreachableCodeReported != TriState.True)
                {
                    ReportError(Span, CAError.CA10016);  // Unreachable code.
                    scope.UnreachableCodeReported = TriState.True;
                }
            }

            if (_onErrorBody != null)
            {
                var scopes = new CAScope[] { scope.Clone(), scope.Clone() };
                foreach (var stmt in _onErrorBody) stmt.Execute(scopes[1]);
                scope.Merge(scopes);
            }
        }

        public CodeAnalyzer CodeAnalyzer
        {
            get { return _ca; }
        }

        public void ReportError(CodeSpan span, CAError errorCode, params object[] args)
        {
            CodeAnalyzer.ReportError(span, errorCode, args);
        }

        public CodeSpan Span
        {
            get { return _span; }
            set { _span = value; }
        }

        private void ProcessOnError(ReadParams p, CodeSpan keywordSpan)
        {
            var code = p.Code;

            if (code.ReadExactWholeWord("resume"))
            {
                var resumeSpan = code.Span;
                if (!code.ReadExact(';'))
                {
                    ReportError(resumeSpan, CAError.CA10015);  // Expected ';'.
                }
            }
            else if (code.ReadExactWholeWord("goto"))
            {
                var gotoSpan = code.Span;
                if (!code.ReadWord())
                {
                    ReportError(gotoSpan, CAError.CA10150); // Expected goto label.
                }
                else if (!code.ReadExact(';'))
                {
                    var labelSpan = code.Span;
                    ReportError(labelSpan, CAError.CA10015);  // Expected ';'.
                }
            }
            else if (code.ReadExact('{'))
            {
                _onErrorBody = new List<Statement>();
                while (!code.EndOfFile && !code.ReadExact("}"))
                {
                    var stmt = Statement.Read(p);
                    if (stmt == null) break;
                    _onErrorBody.Add(stmt);
                }
            }
            else
            {
                ReportError(keywordSpan, CAError.CA10019);   // Expected '{'.
            }
        }
    }
}

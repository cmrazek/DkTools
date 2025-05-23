using DK.Code;
using System.Collections.Generic;

namespace DK.CodeAnalysis.Statements
{
    /// <summary>
    /// Code scope represented by a braces { } block.
    /// </summary>
    internal class ScopeStatement : Statement
    {
        private List<Statement> _statements;

        public ScopeStatement(ReadParams p, CodeSpan openBraceSpan)
            : base(p.CodeAnalyzer, openBraceSpan)
        {
            var code = p.Code;

            while (!code.EndOfFile)
            {
                if (code.ReadExact('}')) break;

                var stmt = Statement.Read(p);
                if (stmt == null) break;

                if (_statements == null) _statements = new List<Statement>();
                _statements.Add(stmt);
            }
        }

        public override void Execute(CAScope scope)
        {
            base.Execute(scope);

            if (_statements != null)
            {
                foreach (var stmt in _statements)
                {
                    stmt.Execute(scope);
                }
            }
        }
    }
}

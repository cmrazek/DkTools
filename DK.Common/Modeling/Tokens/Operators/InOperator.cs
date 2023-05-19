using System;
using System.Collections.Generic;
using System.Linq;

namespace DK.Modeling.Tokens
{
    class InOperator : CompletionOperator
    {
        private DataType _expectedDataType;
        private static readonly string[] _endTokens = new string[] { ")" };

        private InOperator(Scope scope)
            : base(scope)
        { }

        internal static InOperator Parse(Scope scope, Token lastToken, OperatorToken opToken, IEnumerable<string> endTokens, DataType expectedDataType)
        {
            var ret = new InOperator(scope);
            if (lastToken != null) ret.AddToken(lastToken);
            ret.AddToken(opToken ?? throw new ArgumentNullException(nameof(opToken)));

            string[] mergedEndTokens = null;

            var code = scope.Code;
            if (code.ReadExact('('))
            {
                var bracketsToken = new BracketsToken(scope);
                bracketsToken.AddOpen(code.Span);
                ret.AddToken(bracketsToken);
                ret._expectedDataType = expectedDataType;

                while (!code.EndOfFile)
                {
                    if (code.ReadExact(')'))
                    {
                        bracketsToken.AddClose(code.Span);
                        break;
                    }
                    else if (code.ReadExact(','))
                    {
                        bracketsToken.AddToken(new DelimiterToken(scope, code.Span));
                        continue;
                    }

                    if (mergedEndTokens == null)
                    {
                        if (endTokens != null && endTokens.Any()) mergedEndTokens = endTokens.Concat(_endTokens).ToArray();
                        else mergedEndTokens = _endTokens;
                    }

                    var exp = ExpressionToken.TryParse(scope, mergedEndTokens, expectedDataType: expectedDataType);
                    if (exp != null) bracketsToken.AddToken(exp);
                    else break;
                }
            }

            return ret;
        }

        public override DataType CompletionDataType => _expectedDataType;
    }
}

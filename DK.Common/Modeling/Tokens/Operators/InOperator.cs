using System;
using System.Collections.Generic;

namespace DK.Modeling.Tokens
{
    class InOperator : CompletionOperator
    {
        private DataType _expectedDataType;

        private InOperator(Scope scope)
            : base(scope)
        { }

        internal static InOperator Parse(Scope scope, Token lastToken, OperatorToken opToken, IEnumerable<string> endTokens, DataType expectedDataType)
        {
            var ret = new InOperator(scope);
            if (lastToken != null) ret.AddToken(lastToken);
            ret.AddToken(opToken ?? throw new ArgumentNullException(nameof(opToken)));

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

                    var exp = ExpressionToken.TryParse(scope, endTokens, expectedDataType: expectedDataType);
                    if (exp != null) bracketsToken.AddToken(exp);
                    else break;
                }
            }

            return ret;
        }

        public override DataType CompletionDataType => _expectedDataType;
    }
}

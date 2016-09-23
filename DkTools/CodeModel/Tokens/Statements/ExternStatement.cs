using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DkTools.CodeModel.Definitions;

namespace DkTools.CodeModel.Tokens.Statements
{
	class ExternStatement : GroupToken
	{
		private ExternStatement(Scope scope)
			: base(scope)
		{
		}

		public static ExternStatement Parse(Scope scope, KeywordToken keywordToken)
		{
			var code = scope.Code;
			var ret = new ExternStatement(scope);
			ret.AddToken(keywordToken);

			var dataType = DataType.TryParse(new DataType.ParseArgs
			{
				Code = code,
				Scope = scope,
				VariableCallback = (name) =>
				{
					return scope.DefinitionProvider.GetLocal<VariableDefinition>(code.Position, name).FirstOrDefault();
				},
				DataTypeCallback = (name) =>
				{
					return scope.DefinitionProvider.GetAny<DataTypeDefinition>(code.Position, name).FirstOrDefault();
				},
				TokenCreateCallback = (token) =>
				{
					ret.AddToken(token);
				},
				VisibleModel = true
			});
			if (dataType == null) dataType = DataType.Int;

			// Name
			if (!code.ReadWord()) return ret;
			var funcName = code.Text;
			var funcNameSpan = code.Span;
			var funcDef = scope.DefinitionProvider.GetAny<FunctionDefinition>(funcNameSpan.Start, funcName).FirstOrDefault();
			if (funcDef != null)
			{
				ret.AddToken(new IdentifierToken(scope, funcNameSpan, funcName, funcDef));

				// Arguments
				if (!code.ReadExact('(')) return ret;
				ret.AddToken(ArgsToken.Parse(scope, new OperatorToken(scope, code.Span, "("), funcDef.Signature));

				ParseFunctionAttributes(scope, ret);
			}
			else ret.AddToken(new UnknownToken(scope, funcNameSpan, funcName));

			return ret;
		}

		private static readonly string[] _attribEndTokens = new string[] { ";", "{", "}", "description", "prompt", "comment", "nomenu", "accel", "BEGINHLP", "ENDHLP", "tag" };

		private static void ParseFunctionAttributes(Scope scope, GroupToken parentToken)
		{
			var code = scope.Code;
			while (!code.EndOfFile)
			{
				if (code.PeekExact(';') || code.PeekExact('{') || code.PeekExact('}')) return;

				if (code.ReadWord())
				{
					switch (code.Text)
					{
						case "description":
						case "prompt":
						case "comment":
						case "BEGINHLP":
						case "accel":
							{
								parentToken.AddToken(new KeywordToken(scope, code.Span, code.Text));
								var exp = ExpressionToken.TryParse(scope, _attribEndTokens);
								if (exp != null) parentToken.AddToken(exp);
							}
							break;

						case "nomenu":
						case "ENDHLP":
							parentToken.AddToken(new KeywordToken(scope, code.Span, code.Text));
							break;

						case "tag":
							{
								parentToken.AddToken(new KeywordToken(scope, code.Span, code.Text));
								if (code.ReadTagName()) parentToken.AddToken(new KeywordToken(scope, code.Span, code.Text));
								var exp = ExpressionToken.TryParse(scope, _attribEndTokens);
								if (exp != null) parentToken.AddToken(exp);
							}
							break;

						default:
							return;
					}
				}
				else break;
			}
		}

	}
}

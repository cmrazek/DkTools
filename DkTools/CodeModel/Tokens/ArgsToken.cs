using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Tokens
{
	class ArgsToken : GroupToken
	{
		private DataType[] _argDataTypes;

		private ArgsToken(Scope scope, OperatorToken openBracketToken)
			: base(scope)
		{
			AddToken(openBracketToken);
		}

		private static string[] _endTokens = new string[] { ",", ")" };

		/// <summary>
		/// Parses a set of brackets containing arguments.
		/// </summary>
		/// <param name="scope">The scope for this token.</param>
		/// <param name="openBracketToken">The open bracket token.</param>
		/// <param name="argDataTypes">(optional) The data types required for each argument, if known.</param>
		/// <returns>A new argument token.</returns>
		/// <remarks>This function assumes the opening bracket has already been read from the stream.</remarks>
		public static ArgsToken Parse(Scope scope, OperatorToken openBracketToken, IEnumerable<DataType> argDataTypes)
		{
			var code = scope.Code;
			var ret = new ArgsToken(scope, openBracketToken);
			var argIndex = 0;
			DataType dataType;

			if (argDataTypes != null) ret._argDataTypes = argDataTypes.ToArray();

			while (code.SkipWhiteSpace())
			{
				code.Peek();
				if (code.Text == ")")
				{
					ret.AddToken(new OperatorToken(scope, code.MovePeekedSpan(), ")"));
					return ret;
				}

				if (code.Text == ",")
				{
					code.MovePeeked();
					ret.AddToken(new OperatorToken(scope, code.MovePeekedSpan(), ","));
					argIndex++;
					continue;
				}

				if (ret._argDataTypes != null && argIndex < ret._argDataTypes.Length) dataType = ret._argDataTypes[argIndex];
				else dataType = null;

				var exp = ExpressionToken.TryParse(scope, _endTokens, expectedDataType: dataType);
				if (exp != null) ret.AddToken(exp);
				else break;
			}

			return ret;
		}
	}
}

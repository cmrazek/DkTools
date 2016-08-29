using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Tokens.Statements
{
	internal class CastStatement : GroupToken
	{
		private DataType _castDataType;

		private CastStatement(Scope scope)
			: base(scope)
		{ }

		public static CastStatement Parse(Scope scope, BracketsToken castToken, IEnumerable<string> endTokens)
		{
			var ret = new CastStatement(scope);
			ret.AddToken(castToken);
			ret._castDataType = castToken.CastDataType;

			var exp = ExpressionToken.TryParse(scope, endTokens, expectedDataType: ret._castDataType);
			if (exp != null) ret.AddToken(exp);

			return ret;
		}

		public override DataType ValueDataType
		{
			get
			{
				return _castDataType;
			}
		}
	}
}

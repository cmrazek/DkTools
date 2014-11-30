using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Tokens
{
	internal class ClassAndFunctionToken : GroupToken
	{
		private FunctionCallToken _funcToken;

		public ClassAndFunctionToken(GroupToken parent, Scope scope, ClassToken classToken, DotToken dotToken,
			FunctionCallToken funcToken, Definitions.FunctionDefinition funcDef)
			: base(parent, scope, new Token[] { classToken, dotToken, funcToken })
		{
			_funcToken = funcToken;
		}

		public override DataType ValueDataType
		{
			get
			{
				return _funcToken.ValueDataType;
			}
		}
	}
}

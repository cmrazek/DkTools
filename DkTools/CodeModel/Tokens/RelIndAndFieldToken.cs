using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Tokens
{
	internal class RelIndAndFieldToken : GroupToken
	{
		private RelIndFieldToken _fieldToken;

		public RelIndAndFieldToken(GroupToken parent, Scope scope, RelIndToken relIndToken, DotToken dotToken, RelIndFieldToken fieldToken)
			: base(parent, scope, new Token[] { relIndToken, dotToken, fieldToken })
		{
			_fieldToken = fieldToken;
		}

		public override DataType ValueDataType
		{
			get
			{
				return _fieldToken.ValueDataType;
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DK.Modeling.Tokens
{
	public class RelIndAndFieldToken : GroupToken
	{
		private RelIndFieldToken _fieldToken;

		internal RelIndAndFieldToken(Scope scope, RelIndToken relIndToken, DotToken dotToken, RelIndFieldToken fieldToken)
			: base(scope)
		{
			AddToken(relIndToken);
			AddToken(dotToken);
			AddToken(_fieldToken = fieldToken);
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

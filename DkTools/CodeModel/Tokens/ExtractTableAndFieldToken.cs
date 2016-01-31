using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Tokens
{
	internal class ExtractTableAndFieldToken : GroupToken
	{
		public ExtractTableAndFieldToken(Scope scope, ExtractTableToken exToken, DotToken dotToken, ExtractFieldToken fieldToken)
			: base(scope)
		{
			AddToken(exToken);
			AddToken(dotToken);
			AddToken(fieldToken);
		}
	}
}

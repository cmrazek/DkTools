using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Tokens
{
	internal class ExtractTableAndFieldToken : GroupToken
	{
		public ExtractTableAndFieldToken(GroupToken parent, Scope scope, ExtractTableToken exToken, DotToken dotToken, ExtractFieldToken fieldToken)
			: base(parent, scope, new Token[] { exToken, dotToken, fieldToken })
		{ }
	}
}

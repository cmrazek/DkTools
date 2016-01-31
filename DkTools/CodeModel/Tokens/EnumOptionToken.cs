using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Tokens
{
	class EnumOptionToken : WordToken
	{
		public EnumOptionToken(Scope scope, Span span, string text)
			: base(scope, span, text)
		{
		}
	}
}

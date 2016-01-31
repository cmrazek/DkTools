using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Tokens
{
	class CompositeToken : GroupToken
	{
		public CompositeToken(Scope scope, params Token[] tokens)
			: base(scope)
		{
			foreach (var token in tokens) AddToken(token);
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Tokens
{
	class CompositeToken : GroupToken
	{
		public CompositeToken(GroupToken parent, Scope scope, IEnumerable<Token> tokens)
			: base(parent, scope, tokens)
		{ }
	}
}

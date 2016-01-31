using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Tokens
{
	internal sealed class AlterToken : GroupToken
	{
		private AlterToken(GroupToken parent, Scope scope, KeywordToken alterToken)
			: base(parent, scope, new Token[] { alterToken })
		{
		}

		public static AlterToken Parse(GroupToken parent, Scope scope, KeywordToken alterToken)
		{
#if DEBUG
			if (parent == null) throw new ArgumentNullException("parent");
			if (alterToken == null) throw new ArgumentNullException("alterToken");
#endif
			var ret = new AlterToken(parent, scope, alterToken);
			return ret;

			// TODO: this should be replaced with some proper alter statement logic
			//var file = scope.File;

			//file.SkipWhiteSpaceAndComments(scope);
			//var word = file.PeekWord();

			//var alterScope = scope;
			//alterScope.Hint |= ScopeHint.InsideAlter;

			//ret.ParseScope(alterScope, t =>
			//{
			//	if (t.BreaksStatement) return ParseScopeResult.StopAndReject;
			//	return ParseScopeResult.Continue;
			//});

			//return ret;
		}
	}
}

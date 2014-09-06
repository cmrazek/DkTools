using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DkTools.CodeModel.Definitions;

namespace DkTools.CodeModel.Tokens
{
	/// <summary>
	/// A call to a previously defined macro.
	/// </summary>
	internal class MacroCallToken : GroupToken
	{
		private IdentifierToken _nameToken;
		private BracketsToken _argsToken;

		/// <summary>
		/// Creates a macro call token.
		/// </summary>
		/// <param name="parent">(required) Parent token</param>
		/// <param name="scope">(required) Current scope</param>
		/// <param name="nameToken">(required) Function name</param>
		/// <param name="argsToken">(required) Arguments being passed</param>
		/// <param name="def">(required) Definition of this macro in file</param>
		public MacroCallToken(GroupToken parent, Scope scope, IdentifierToken nameToken, BracketsToken argsToken, MacroDefinition def)
			: base(parent, scope, new Token[] { nameToken, argsToken })
		{
#if DEBUG
			if (nameToken == null) throw new ArgumentNullException("nameToken");
			if (argsToken == null) throw new ArgumentNullException("argsToken");
			if (def == null) throw new ArgumentNullException("def");
#endif
			_nameToken = nameToken;
			_nameToken.SourceDefinition = def;

			_argsToken = argsToken;
		}
	}
}

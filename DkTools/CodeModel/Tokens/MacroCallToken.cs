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

		/// <summary>
		/// Creates a macro call token.
		/// </summary>
		/// <param name="parent">(required) Parent token</param>
		/// <param name="scope">(required) Current scope</param>
		/// <param name="nameToken">(required) Function name</param>
		/// <param name="def">(required) Definition of this macro in file</param>
		public MacroCallToken(GroupToken parent, Scope scope, IdentifierToken nameToken, MacroDefinition def)
			: base(parent, scope, new Token[] { nameToken })
		{
#if DEBUG
			if (nameToken == null) throw new ArgumentNullException("nameToken");
			if (def == null) throw new ArgumentNullException("def");
#endif
			_nameToken = nameToken;
			_nameToken.SourceDefinition = def;
		}
	}
}

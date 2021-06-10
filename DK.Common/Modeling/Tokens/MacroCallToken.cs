using DK.Definitions;
using System;

namespace DK.Modeling.Tokens
{
	/// <summary>
	/// A call to a previously defined macro.
	/// </summary>
	public class MacroCallToken : GroupToken
	{
		private IdentifierToken _nameToken;

		/// <summary>
		/// Creates a macro call token.
		/// </summary>
		/// <param name="scope">(required) Current scope</param>
		/// <param name="nameToken">(required) Function name</param>
		/// <param name="def">(required) Definition of this macro in file</param>
		internal MacroCallToken(Scope scope, IdentifierToken nameToken, MacroDefinition def)
			: base(scope)
		{
#if DEBUG
			if (nameToken == null) throw new ArgumentNullException("nameToken");
			if (def == null) throw new ArgumentNullException("def");
#endif
			AddToken(_nameToken = nameToken);
			_nameToken.SourceDefinition = def;
		}
	}
}

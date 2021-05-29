using DK.Definitions;
using System;

namespace DK.Modeling.Tokens
{
	public class InterfacePropertyToken : GroupToken
	{
		private InterfacePropertyDefinition _propDef;

		internal InterfacePropertyToken(Scope scope, VariableToken intVarToken, DotToken dotToken, IdentifierToken nameToken, InterfacePropertyDefinition propDef)
			: base(scope)
		{
#if DEBUG
			if (intVarToken == null) throw new ArgumentNullException("intVarToken");
			if (dotToken == null) throw new ArgumentNullException("dotToken");
			if (nameToken == null) throw new ArgumentNullException("nameToken");
			if (propDef == null) throw new ArgumentNullException("propDef");
#endif
			nameToken.SourceDefinition = propDef;
			_propDef = propDef;

			AddToken(intVarToken);
			AddToken(dotToken);
			AddToken(nameToken);
		}

		public override DataType ValueDataType
		{
			get
			{
				return _propDef.DataType;
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel.Definitions;

namespace DkTools.CodeModel.Tokens
{
	internal sealed class InterfacePropertyToken : GroupToken
	{
		private InterfacePropertyDefinition _propDef;

		public InterfacePropertyToken(GroupToken parent, Scope scope, VariableToken intVarToken, DotToken dotToken, IdentifierToken nameToken, InterfacePropertyDefinition propDef)
			: base(parent, scope, new Token[] { intVarToken, dotToken, nameToken })
		{
#if DEBUG
			if (intVarToken == null) throw new ArgumentNullException("intVarToken");
			if (dotToken == null) throw new ArgumentNullException("dotToken");
			if (nameToken == null) throw new ArgumentNullException("nameToken");
			if (propDef == null) throw new ArgumentNullException("propDef");
#endif
			nameToken.SourceDefinition = propDef;
			_propDef = propDef;
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel.Definitions;

namespace DkTools.CodeModel.Tokens
{
	internal sealed class InterfaceMethodCallToken : GroupToken
	{
		private InterfaceMethodDefinition _methodDef;
		private IdentifierToken _nameToken;

		public InterfaceMethodCallToken(GroupToken parent, Scope scope, VariableToken intVarToken, DotToken dotToken, IdentifierToken nameToken, BracketsToken argsToken, InterfaceMethodDefinition def)
			: base(parent, scope, new Token[] { intVarToken, dotToken, nameToken, argsToken })
		{
#if DEBUG
			if (intVarToken == null) throw new ArgumentNullException("intVarToken");
			if (dotToken == null) throw new ArgumentNullException("dotToken");
			if (nameToken == null) throw new ArgumentNullException("nameToken");
			if (argsToken == null) throw new ArgumentNullException("argsToken");
			if (def == null) throw new ArgumentNullException("def");
#endif
			nameToken.SourceDefinition = def;
			_methodDef = def;
			_nameToken = nameToken;
		}

		public InterfaceMethodDefinition MethodDefinition
		{
			get { return _methodDef; }
		}

		public IdentifierToken NameToken
		{
			get { return _nameToken; }
		}

		public override DataType ValueDataType
		{
			get
			{
				return _methodDef.ReturnDataType;
			}
		}
	}
}

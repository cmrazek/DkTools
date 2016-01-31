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

		public InterfaceMethodCallToken(Scope scope, VariableToken intVarToken, DotToken dotToken, IdentifierToken nameToken, BracketsToken argsToken, InterfaceMethodDefinition def)
			: base(scope)
		{
#if DEBUG
			if (intVarToken == null) throw new ArgumentNullException("intVarToken");
			if (dotToken == null) throw new ArgumentNullException("dotToken");
			if (nameToken == null) throw new ArgumentNullException("nameToken");
			if (argsToken == null) throw new ArgumentNullException("argsToken");
			if (def == null) throw new ArgumentNullException("def");
#endif
			AddToken(intVarToken);
			AddToken(dotToken);
			AddToken(_nameToken = nameToken);
			AddToken(argsToken);

			_methodDef = def;
			_nameToken.SourceDefinition = def;
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel;
using DkTools.CodeModel.Definitions;

namespace DkTools.DkDict
{
	class InterfaceMethod
	{
		private InterfaceMethodDefinition _def;

		public InterfaceMethod(InterfaceTypeDefinition typeDef, FunctionSignature sig)
		{
			_def = new InterfaceMethodDefinition(typeDef, sig.FunctionName, sig, sig.ReturnDataType);
		}

		public InterfaceMethodDefinition Definition
		{
			get { return _def; }
		}
	}
}

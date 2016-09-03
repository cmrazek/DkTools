using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel;
using DkTools.CodeModel.Definitions;

namespace DkTools.DkDict
{
	class InterfaceProperty
	{
		private InterfacePropertyDefinition _def;

		public InterfaceProperty(InterfaceTypeDefinition typeDef, string name, DataType dataType)
		{
			_def = new InterfacePropertyDefinition(typeDef, name, dataType);
		}

		public InterfacePropertyDefinition Definition
		{
			get { return _def; }
		}
	}
}

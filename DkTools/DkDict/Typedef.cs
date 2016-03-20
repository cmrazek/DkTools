using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel;
using DkTools.CodeModel.Definitions;

namespace DkTools.DkDict
{
	class Typedef
	{
		public string Name { get; private set; }
		public DataType DataType { get; private set; }

		private DataTypeDefinition _def;

		public Typedef(string name, DataType dataType)
		{
			Name = name;
			DataType = dataType;

			_def = new DataTypeDefinition(name, dataType);
		}

		public DataTypeDefinition Definition
		{
			get { return _def; }
		}
	}
}

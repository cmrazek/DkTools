using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel.Definitions;

namespace DkTools.CodeModel
{
	internal struct DefinitionLocation
	{
		private Definition _def;
		private int _localContainerOffset;

		public DefinitionLocation(Definition def, int localContainerOffset)
		{
			_def = def;
			_localContainerOffset = localContainerOffset;
		}

		public Definition Definition
		{
			get { return _def; }
			set { _def = value; }
		}

		public int LocalContainerOffset
		{
			get { return _localContainerOffset; }
			set { _localContainerOffset = value; }
		}
	}
}

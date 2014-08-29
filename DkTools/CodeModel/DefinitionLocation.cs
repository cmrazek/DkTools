using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel
{
	internal struct DefinitionLocation
	{
		private Definition _def;
		private int _localFileOffset;

		public DefinitionLocation(Definition def, int localFileOffset)
		{
			_def = def;
			_localFileOffset = localFileOffset;
		}

		public Definition Definition
		{
			get { return _def; }
			set { _def = value; }
		}

		public int LocalFileOffset
		{
			get { return _localFileOffset; }
			set { _localFileOffset = value; }
		}
	}
}

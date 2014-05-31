using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools
{
	[Serializable]
	class ProbeException : Exception
	{
		public ProbeException(string message)
			: base(message)
		{ }
	}
}

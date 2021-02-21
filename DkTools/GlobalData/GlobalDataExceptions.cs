using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.GlobalData
{
	internal class InvalidRepoException : Exception
	{
		public InvalidRepoException(string message) : base(message) { }
	}

	internal class InvalidAddressException : Exception
	{
		public InvalidAddressException() { }
	}
}

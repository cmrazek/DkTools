using System;

namespace DK.Repository
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DK.Preprocessing.Tokens
{
	internal abstract class Token
	{
		protected GroupToken _parent;

		public Token(GroupToken parent)
		{
			_parent = parent;
		}

		public abstract long? Value { get; }
	}
}

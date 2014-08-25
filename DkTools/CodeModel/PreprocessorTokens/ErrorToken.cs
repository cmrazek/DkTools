using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.PreprocessorTokens
{
	internal class ErrorToken : Token
	{
		public ErrorToken(GroupToken parent)
			: base(parent)
		{
		}

		public override long? Value
		{
			get { return null; }
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Tokens.Operators
{
	abstract class CompletionOperator : GroupToken
	{
		public abstract DataType CompletionDataType { get; }

		public CompletionOperator(Scope scope)
			: base(scope)
		{ }
	}
}

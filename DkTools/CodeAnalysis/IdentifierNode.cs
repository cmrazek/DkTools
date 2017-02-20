using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel;
using DkTools.CodeModel.Definitions;

namespace DkTools.CodeAnalysis
{
	class IdentifierNode : TextNode
	{
		private Definition _def;

		public IdentifierNode(Span span, string name, Definition def)
			: base(span, name)
		{
			_def = def;
		}
	}
}

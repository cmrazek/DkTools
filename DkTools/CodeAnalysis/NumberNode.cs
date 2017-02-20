using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel;

namespace DkTools.CodeAnalysis
{
	class NumberNode : TextNode
	{
		public NumberNode(Span span, string text)
			: base(span, text)
		{
		}
	}
}

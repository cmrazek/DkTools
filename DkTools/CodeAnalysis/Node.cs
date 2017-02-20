using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel;

namespace DkTools.CodeAnalysis
{
	abstract class Node
	{
		private CodeModel.Span _span;

		public Node(CodeModel.Span span)
		{
			_span = span;
		}

		public Span Span
		{
			get { return _span; }
			protected set { _span = value; }
		}
	}
}

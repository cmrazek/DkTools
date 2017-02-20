using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel;
using DkTools.CodeModel.Definitions;

namespace DkTools.CodeAnalysis
{
	class FunctionCallNode : Node
	{
		private string _name;
		private Span _funcNameSpan;
		private List<Node> _args = new List<Node>();
		private Definition _def;

		public FunctionCallNode(Span funcNameSpan, string funcName)
			: base(funcNameSpan)
		{
			_name = funcName;
			_funcNameSpan = funcNameSpan;
		}

		public void AddArgument(Node node)
		{
			_args.Add(node);
		}

		public int NumArguments
		{
			get { return _args.Count; }
		}

		public Definition Definition
		{
			get { return _def; }
			set { _def = value; }
		}
	}
}

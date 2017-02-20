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
		private List<GroupNode> _args = new List<GroupNode>();
		private Definition _def;

		public FunctionCallNode(Statement stmt, Span funcNameSpan, string funcName)
			: base(stmt, funcNameSpan)
		{
			_name = funcName;
			_funcNameSpan = funcNameSpan;
		}

		public void AddArgument(GroupNode node)
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

		public override Value Value
		{
			get
			{
				Execute();
				return new Value(_def.DataType, true);
			}
			set
			{
				base.Value = value;
			}
		}

		public override bool CanAssignValue
		{
			get
			{
				return false;
			}
		}

		public override int Precedence
		{
			get
			{
				return 0;
			}
		}

		public override void Execute()
		{
			foreach (var arg in _args)
			{
				arg.Execute();
				var value = arg.Value;	// To simulate a 'read' of the expression
			}
		}
	}
}

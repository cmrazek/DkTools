using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeAnalysis.Statements;
using DkTools.CodeModel;
using DkTools.CodeModel.Definitions;

namespace DkTools.CodeAnalysis.Nodes
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

		public static FunctionCallNode Read(ReadParams p, Span funcNameSpan, string funcName, Definition funcDef = null)
		{
			var funcCallNode = new FunctionCallNode(p.Statement, funcNameSpan, funcName);

			GroupNode curArg = null;
			var code = p.Code;

			while (!code.EndOfFile)
			{
				if (code.ReadExact(','))
				{
					if (curArg != null) funcCallNode.AddArgument(curArg);
					curArg = null;
				}
				else if (code.ReadExact(')')) break;
				else if (code.ReadExact(';')) break;

				if (curArg == null) curArg = new GroupNode(p.Statement);

				var node = ExpressionNode.Read(p, ",", ")", ";");
				if (node != null) curArg.AddNode(node);
			}

			if (curArg != null) funcCallNode.AddArgument(curArg);

			if (funcDef != null)
			{
				funcCallNode.Definition = funcDef;
			}
			else
			{
				var funcDefs = (from d in p.Statement.CodeAnalyzer.PreprocessorModel.DefinitionProvider.GetAny(funcNameSpan.Start, funcName)
								where d.ArgumentsRequired
								select d).ToArray();
				if (funcDefs.Length == 1)
				{
					funcCallNode.Definition = funcDefs[0];
				}
				else if (funcDefs.Length > 1)
				{
					var numArgs = funcCallNode.NumArguments;
					funcDef = funcDefs.FirstOrDefault(f => f.Arguments.Count() == numArgs);
					if (funcDef == null)
					{
						funcCallNode.ReportError(funcNameSpan, CAError.CA0002, funcName, numArgs);	// Function '{0}' with {1} argument(s) not found.
					}
					else
					{
						funcCallNode.Definition = funcDef;
					}
				}
				else
				{
					funcCallNode.ReportError(funcNameSpan, CAError.CA0003, funcName);	// Function '{0}' not found.
				}
			}

			return funcCallNode;
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

		public override Value ReadValue(RunScope scope)
		{
			foreach (var arg in _args)
			{
				arg.ReadValue(scope);
			}

			if (_def == null) return new Value(DataType.Void);
			return new Value(_def.DataType);
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
	}
}

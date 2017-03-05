using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeAnalysis.Statements;
using DkTools.CodeAnalysis.Values;
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

				var node = ExpressionNode.Read(p, ",", ")");
				if (node != null) curArg.AddChild(node);
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
			switch (_name)
			{
				case "abs":
					return Read_abs(scope);
				case "count":
					return Read_count(scope);
				case "max":
					return Read_max(scope);
				case "min":
					return Read_min(scope);
				case "oldvalue":
					return Read_oldvalue(scope);
				case "sum":
					return Read_sum(scope);
			}

			var defArgs = _def != null ? _def.Arguments.ToArray() : new ArgumentDescriptor[0];
			var argIndex = 0;
			foreach (var arg in _args)
			{
				var defArg = argIndex < defArgs.Length ? defArgs[argIndex] : null;
				if (defArg != null)
				{
					if (defArg.PassByMethod == PassByMethod.Reference || defArg.PassByMethod == PassByMethod.ReferencePlus)
					{
						var writeScope = scope.Clone(dataTypeContext: defArg.DataType);
						arg.WriteValue(writeScope, Value.CreateUnknownFromDataType(defArg.DataType));
						scope.Merge(writeScope);
					}
					else
					{
						var readScope = scope.Clone(dataTypeContext: defArg.DataType);
						arg.ReadValue(readScope);
						scope.Merge(readScope);
					}
				}
				else
				{
					arg.ReadValue(scope);
				}
				argIndex++;
			}

			if (_def == null) return Value.Void;
			return Value.CreateUnknownFromDataType(_def.DataType);
		}

		public override bool CanAssignValue(RunScope scope)
		{
			return false;
		}

		public override int Precedence
		{
			get
			{
				return 0;
			}
		}

		public override DataType GetDataType(RunScope scope)
		{
			if (_def != null) return _def.DataType;
			return DataType.Void;
		}

		private Value Read_oldvalue(RunScope scope)
		{
			if (_args.Count != 1)
			{
				ReportError(_funcNameSpan, CAError.CA0057, 1);	// Function expects {0} argument(s).
			}

			return Value.CreateUnknownFromDataType(_args[0].GetDataType(scope));
		}

		private Value Read_abs(RunScope scope)
		{
			if (_args.Count != 1)
			{
				ReportError(_funcNameSpan, CAError.CA0057, 1);	// Function expects {0} argument(s).
			}

			return Value.CreateUnknownFromDataType(_args[0].GetDataType(scope));
		}

		private Value Read_count(RunScope scope)
		{
			if (_args.Count < 1 || _args.Count > 2)
			{
				ReportError(_funcNameSpan, CAError.CA0057, "1 or 2");	// Function expects {0} argument(s).
			}

			return Value.CreateUnknownFromDataType(DataType.Int);
		}

		private Value Read_sum(RunScope scope)
		{
			if (_args.Count < 1 || _args.Count > 2)
			{
				ReportError(_funcNameSpan, CAError.CA0057, "1 or 2");	// Function expects {0} argument(s).
			}

			return Value.CreateUnknownFromDataType(DataType.Int);
		}

		private Value Read_max(RunScope scope)
		{
			if (_args.Count != 1)
			{
				ReportError(_funcNameSpan, CAError.CA0057, 1);	// Function expects {0} argument(s).
			}

			return Value.CreateUnknownFromDataType(_args[0].GetDataType(scope));
		}

		private Value Read_min(RunScope scope)
		{
			if (_args.Count != 1)
			{
				ReportError(_funcNameSpan, CAError.CA0057, 1);	// Function expects {0} argument(s).
			}

			return Value.CreateUnknownFromDataType(_args[0].GetDataType(scope));
		}
	}
}

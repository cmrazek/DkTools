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

		public FunctionCallNode(Statement stmt, Span funcNameSpan, string funcName, Definition funcDef)
			: base(stmt, funcDef != null ? funcDef.DataType : DataType.Void, funcNameSpan)
		{
			_name = funcName;
			_funcNameSpan = funcNameSpan;
			_def = funcDef;
		}

		private static FunctionCallNode ParseArguments(ReadParams p, Span funcNameSpan, string funcName, Definition funcDef)
		{
			var funcCallNode = new FunctionCallNode(p.Statement, funcNameSpan, funcName, funcDef);
			var code = p.Code;
			var resetPos = code.Position;
			var commaExpected = false;
			var closed = false;
			var argIndex = 0;
			var args = new List<GroupNode>();
			var argDefs = funcDef.Arguments.ToArray();

			if (code.ReadExact(')'))
			{
				closed = true;
			}
			else
			{
				while (!code.EndOfFile)
				{
					if (commaExpected)
					{
						if (code.ReadExact(')'))
						{
							closed = true;
							break;
						}
						if (!code.ReadExact(','))
						{
							code.Position = resetPos;
							return null;
						}
						commaExpected = false;
					}
					else
					{
						var argDef = argDefs != null && argIndex < argDefs.Length ? argDefs[argIndex] : null;

						var arg = ExpressionNode.Read(p, argDef != null ? argDef.DataType : null, ",", ")");
						if (arg != null) funcCallNode.AddArgument(arg);
						commaExpected = true;
						argIndex++;
					}
				}
			}

			if (!closed)
			{
				code.Position = resetPos;
				return null;
			}

			//if (argDefs.Length != funcCallNode.NumArguments)
			//{
			//	code.Position = resetPos;
			//	return null;
			//}
			
			return funcCallNode;
		}

		public static FunctionCallNode Read(ReadParams p, Span funcNameSpan, string funcName, Definition funcDef = null)
		{
			if (funcDef != null)
			{
				var node = ParseArguments(p, funcNameSpan, funcName, funcDef);
				if (node != null) return node;
			}

			var funcDefs = (from d in p.Statement.CodeAnalyzer.PreprocessorModel.DefinitionProvider.GetAny(funcNameSpan.Start, funcName)
							where d.ArgumentsRequired
							select d).ToArray();
			foreach (var def in funcDefs)
			{
				var fd = def as FunctionDefinition;
				if (fd == null) continue;

				var node = ParseArguments(p, funcNameSpan, funcName, fd);
				if (node != null) return node;
			}

			var funcCallNode = new FunctionCallNode(p.Statement, funcNameSpan, funcName, null);
			funcCallNode.ReportError(funcNameSpan, CAError.CA0003, funcName);	// Function '{0}' not found.
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

		public override DataType DataType
		{
			get
			{
				switch (_name)
				{
					case "abs":
					case "max":
					case "min":
					case "oldvalue":
					case "sum":
						return _args.Count > 0 ? _args[0].DataType : DataType.Void;
					case "count":
						return DataType.Int;
					default:
						return base.DataType;
				}
			}
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
						var readScope = scope.Clone();
						readScope.SuppressInitializedCheck = true;
						arg.ReadValue(readScope);
						scope.Merge(readScope);

						var writeScope = scope.Clone();
						arg.WriteValue(writeScope, Value.CreateUnknownFromDataType(defArg.DataType));
						scope.Merge(writeScope);
					}
					else
					{
						var readScope = scope.Clone();
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
			get { return 0; }
		}

		private Value Read_oldvalue(RunScope scope)
		{
			if (_args.Count != 1)
			{
				ReportError(_funcNameSpan, CAError.CA0057, 1);	// Function expects {0} argument(s).
			}

			return Value.CreateUnknownFromDataType(_args[0].DataType);
		}

		private Value Read_abs(RunScope scope)
		{
			if (_args.Count != 1)
			{
				ReportError(_funcNameSpan, CAError.CA0057, 1);	// Function expects {0} argument(s).
			}

			return Value.CreateUnknownFromDataType(_args[0].DataType);
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

			return Value.CreateUnknownFromDataType(_args[0].DataType);
		}

		private Value Read_min(RunScope scope)
		{
			if (_args.Count != 1)
			{
				ReportError(_funcNameSpan, CAError.CA0057, 1);	// Function expects {0} argument(s).
			}

			return Value.CreateUnknownFromDataType(_args[0].DataType);
		}
	}
}

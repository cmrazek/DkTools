using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CM=DkTools.CodeModel;
using DkTools.ErrorTagging;
using DkTools.CodeModel.Definitions;

namespace DkTools.CodeAnalysis
{
	class CodeAnalyzer
	{
		private OutputPane _pane;
		private CM.CodeModel _codeModel;
		private CM.PreprocessorModel _prepModel;
		private string _fullSource;
		private Microsoft.VisualStudio.Text.Editor.ITextView _view;

		private CodeParser _code;
		private int _funcOffset;
		private List<Statement> _stmts;
		private Dictionary<string, Variable> _vars;
		private Statement _stmt;

		public CodeAnalyzer(OutputPane pane, CM.CodeModel model, Microsoft.VisualStudio.Text.Editor.ITextView view)
		{
			if (pane == null) throw new ArgumentNullException("pane");
			if (model == null) throw new ArgumentNullException("model");
			if (view == null) throw new ArgumentNullException("view");

			_pane = pane;
			_codeModel = model;
			_view = view;
		}

		public void Run()
		{
			_pane.WriteLine(string.Format("Starting code analysis on file: {0}", _codeModel.FileName));

			_prepModel = _codeModel.PreprocessorModel;
			_fullSource = _codeModel.Source.Text;

			ErrorTaskProvider.Instance.RemoveAllForSource(ErrorTaskSource.CodeAnalysis, _codeModel.FileName);

			foreach (var func in _prepModel.LocalFunctions)
			{
				AnalyzeFunction(func);
			}
		}

		private void AnalyzeFunction(CodeModel.PreprocessorModel.LocalFunction func)
		{
			_pane.WriteLine(string.Format("Analyzing function '{0}'", func.Definition.Name));	// TODO: remove

			var body = _fullSource.Substring(func.StartPos, func.EndPos - func.StartPos);
			if (body.EndsWith("}")) body = body.Substring(0, body.Length - 1);	// End pos is just after the closing brace

			_code = new CodeParser(body);
			_funcOffset = func.StartPos;
			_stmts = new List<Statement>();

			// Parse the function body
			while (!_code.EndOfFile)
			{
				var stmt = ReadStatement();
				if (stmt == null) break;
				_stmts.Add(stmt);
			}

			_vars = new Dictionary<string, Variable>();
			foreach (var arg in func.Arguments)
			{
				if (!string.IsNullOrEmpty(arg.Name))
				{
					_vars[arg.Name] = new Variable(arg.Name, arg.DataType, new Value(arg.DataType, true), true);
				}
			}

			foreach (var v in func.Variables)
			{
				_vars[v.Name] = new Variable(v.Name, v.DataType, new Value(v.DataType, false), false);
			}

			foreach (var stmt in _stmts)
			{
				if (stmt.IsEmpty && !stmt.EndSpan.IsEmpty)
				{
					ReportError(stmt.EndSpan, CAError.CA0005);	// Empty statement not allowed.
				}
				else
				{
					stmt.Execute();
				}
			}
		}

		private Statement ReadStatement()
		{
			_code.SkipWhiteSpace();
			if (_code.EndOfFile) return null;

			_stmt = new Statement(this);

			while (!_code.EndOfFile)
			{
				if (_code.ReadExact(';'))
				{
					_stmt.EndSpan = _code.Span;
					return _stmt;
				}

				var node = ReadExpression(";");
				if (node == null) break;
				_stmt.AddNode(node);
			}

			return _stmt;
		}

		public void ReportError(CM.Span span, ErrorType type, CAError errorCode, params object[] args)
		{
			var filePos = _prepModel.Source.GetFilePosition(span.Start + _funcOffset);
			var primaryFileSpan = _prepModel.Source.GetPrimaryFileSpan(span.Offset(_funcOffset));

			int lineNum, linePos;
			Util.CalcLineAndPosFromOffset(_fullSource, span.Start, out lineNum, out linePos);

			var message = errorCode.GetText(args);

			_pane.WriteLine(string.Format("{0}({1}) {2}", filePos.FileName, lineNum, message));

			if (!primaryFileSpan.IsEmpty)
			{
				var task = new ErrorTagging.ErrorTask(filePos.FileName, lineNum, message, type,
					ErrorTaskSource.CodeAnalysis, _codeModel.FileName, _codeModel.Snapshot,
					primaryFileSpan.ToVsTextSnapshotSpan(_codeModel.Snapshot));
				ErrorTaskProvider.Instance.Add(task);
			}
		}

		public void ReportError(CM.Span span, CAError errorCode, params object[] args)
		{
			ReportError(span, ErrorType.Error, errorCode, args);
		}

		public void ReportWarning(CM.Span span, CAError errorCode, params object[] args)
		{
			ReportError(span, ErrorType.Warning, errorCode, args);
		}

		private Node ReadExpression(params string[] stopStrings)
		{
			ExpressionNode exp = null;

			while (!_code.EndOfFile)
			{
				if (stopStrings != null)
				{
					foreach (var str in stopStrings)
					{
						if (str.IsWord())
						{
							if (_code.PeekExactWholeWord(str)) return exp;
						}
						else
						{
							if (_code.PeekExact(str)) return exp;
						}
					}
				}

				if (!_code.Read()) break;
				if (exp == null) exp = new ExpressionNode(_stmt);

				switch (_code.Type)
				{
					case CodeType.Number:
						exp.AddNode(new NumberNode(_stmt, _code.Span, _code.Text));
						break;
					case CodeType.StringLiteral:
						exp.AddNode(new StringLiteralNode(_stmt, _code.Span, _code.Text));
						break;
					case CodeType.Word:
						exp.AddNode(ReadWord());
						break;
					case CodeType.Operator:
						switch (_code.Text)
						{
							case "(":
							case "[":
								exp.AddNode(ReadNestable(_code.Span, _code.Text, stopStrings));
								break;
							default:
								exp.AddNode(new OperatorNode(_stmt, _code.Span, _code.Text));
								break;
						}
						break;
					default:
						ReportError(_code.Span, ErrorType.Error, CAError.CA0001);	// Unknown '{0}'.
						exp.AddNode(new UnknownNode(_stmt, _code.Span, _code.Text));
						break;
				}
			}

			return exp;
		}

		private Node ReadWord()
		{
			var word = _code.Text;
			var span = _code.Span;

			if (_code.ReadExact('('))
			{
				// This is a function call
				return ReadFunctionCall(span, word);
			}

			if (_code.ReadExact('.'))
			{
				var dotSpan = _code.Span;

				if (_code.ReadWord())
				{
					var childWord = _code.Text;
					var combinedWord = string.Concat(word, ".", childWord);
					var combinedSpan = span.Envelope(_code.Span);

					if (_code.ReadExact('('))
					{
						foreach (var parentDef in (from d in _prepModel.DefinitionProvider.GetAny(_code.Position + _funcOffset, word)
												   where d.AllowsChild
												   select d))
						{
							var childDef = parentDef.ChildDefinitions.FirstOrDefault(c => c.Name == childWord && c.ArgumentsRequired);
							if (childDef != null)
							{
								return ReadFunctionCall(combinedSpan, combinedWord, childDef);
							}
						}

						ReportError(combinedSpan, CAError.CA0003, combinedWord);	// Function '{0}' not found.
						return new UnknownNode(_stmt, combinedSpan, combinedWord);
					}
					else // No opening bracket
					{
						foreach (var parentDef in (from d in _prepModel.DefinitionProvider.GetAny(_code.Position + _funcOffset, word)
												   where d.AllowsChild
												   select d))
						{
							var childDef = parentDef.ChildDefinitions.FirstOrDefault(c => c.Name == childWord && !c.ArgumentsRequired);
							if (childDef != null)
							{
								return new IdentifierNode(_stmt, combinedSpan, combinedWord, childDef);
							}
						}

						ReportError(combinedSpan, CAError.CA0001, combinedWord);	// Unknown '{0}'.
						return new UnknownNode(_stmt, combinedSpan, combinedWord);
					}
				}
				else // No word after dot
				{
					ReportError(dotSpan, CAError.CA0004);	// Expected identifier to follow '.'
					return new UnknownNode(_stmt, span.Envelope(dotSpan), string.Concat(word, "."));
				}
			}
			else // No dot after word
			{
				var def = (from d in _prepModel.DefinitionProvider.GetAny(_code.Position + _funcOffset, word)
						   where !d.RequiresChild && !d.ArgumentsRequired
						   select d).FirstOrDefault();
				if (def != null) return new IdentifierNode(_stmt, span, word, def);

				ReportError(span, CAError.CA0001, word);	// Unknown '{0}'.
				return new UnknownNode(_stmt, span, word);
			}
		}

		private FunctionCallNode ReadFunctionCall(CM.Span span, string funcName, Definition funcDef = null)
		{
			var funcCallNode = new FunctionCallNode(_stmt, span, funcName);

			GroupNode curArg = null;

			while (!_code.EndOfFile)
			{
				if (_code.ReadExact(','))
				{
					if (curArg != null) funcCallNode.AddArgument(curArg);
					curArg = null;
				}
				else if (_code.ReadExact(')')) break;
				else if (_code.ReadExact(';')) break;

				if (curArg == null) curArg = new GroupNode(_stmt);

				var node = ReadExpression(",", ")", ";");
				if (node != null) curArg.AddNode(node);
			}

			if (funcDef != null)
			{
				funcCallNode.Definition = funcDef;
			}
			else
			{
				var funcDefs = (from d in _prepModel.DefinitionProvider.GetAny(span.Start, funcName)
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
						ReportError(span, CAError.CA0002, funcName, numArgs);	// Function '{0}' with {1} argument(s) not found.
					}
				}
				else
				{
					ReportError(span, CAError.CA0003, funcName);	// Function '{0}' not found.
				}
			}

			return funcCallNode;
		}

		private Node ReadNestable(CM.Span openSpan, string text, string[] stopStrings)
		{
			GroupNode groupNode;
			string endText;
			switch (text)
			{
				case "(":
					groupNode = new BracketsNode(_stmt, openSpan);
					endText = ")";
					break;
				case "[":
					groupNode = new ArrayNode(_stmt, openSpan);
					endText = "]";
					break;
				default:
					throw new ArgumentOutOfRangeException("text");
			}

			if (stopStrings == null) stopStrings = new string[] { endText };
			else stopStrings = stopStrings.Concat(new string[] { endText }).ToArray();

			while (!_code.EndOfFile)
			{
				if (_code.ReadExact(endText))
				{
					groupNode.Span = groupNode.Span.Envelope(_code.Span);
					break;
				}

				var exp = ReadExpression(stopStrings);
				if (exp == null) break;
				groupNode.AddNode(exp);
			}

			return groupNode;
		}

		public Value GetVariable(string name)
		{
			Variable v;
			if (_vars.TryGetValue(name, out v))
			{
				return v.Value;
			}

			return Value.Empty;
		}

		public void SetVariable(string name, Value value)
		{
			Variable v;
			if (_vars.TryGetValue(name, out v))
			{
				v.Value = value;
			}
		}
	}

	class CAException : Exception
	{
		public CAException(string message)
			: base(message)
		{
		}
	}
}

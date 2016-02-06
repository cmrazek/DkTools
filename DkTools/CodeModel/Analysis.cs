#if REPORT_ERRORS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel.AnalysisNodes;
using DkTools.CodeModel.Definitions;
using DkTools.ErrorTagging;

namespace DkTools.CodeModel
{
	internal class Analysis
	{
		private CodeModel _model;
		private ErrorProvider _errProv;
		private CodeSource _source;
		private CodeParser _code;
		private DefinitionProvider _defProv;
		private PreprocessorModel.LocalFunction _func;

		public Analysis(CodeModel model)
		{
#if DEBUG
			if (model == null) throw new ArgumentNullException("model");
#endif
			_model = model;
			_source = model.PreprocessorModel.Source;
			_errProv = model.PreprocessorModel.ErrorProvider;
			_defProv = _model.DefinitionProvider;
		}

		public void Perform()
		{
#if DEBUG
			Log.WriteDebug("Performing code analysis...");
			var startTime = DateTime.Now;
#endif

			foreach (var func in _model.PreprocessorModel.LocalFunctions)
			{
				AnalyzeFunction(func);
			}

#if DEBUG
			var endTime = DateTime.Now;
			Log.WriteDebug("Code analysis finished. Elapsed: [{0}] Errors Reported [{1}]", endTime.Subtract(startTime), _errProv.ErrorCount);
#endif
		}

		public ErrorProvider ErrorProvider
		{
			get { return _errProv; }
		}

		private void AnalyzeFunction(PreprocessorModel.LocalFunction func)
		{
			_func = func;

			var funcBodySource = _source.Text.Substring(_func.StartPos, _func.EndPos - _func.StartPos);
			_code = new CodeParser(funcBodySource);
			_code.DocumentOffset = _func.StartPos;

			var pos = _code.Position;
			var lastPos = pos;

			while (!_code.EndOfFile)
			{
				if (_code.ReadExact('}')) return;
				ReadStatement();

				pos = _code.Position;
				if (pos == lastPos)
				{
					if (_code.Read())
					{
						ReportError(_code.TokenSpan, ErrorCode.Func_InvalidToken, _code.TokenText);
					}
					else
					{
						ReportError(_code.TokenSpan, ErrorCode.Func_InfiniteLoop);
						break;
					}
				}
			}

			ReportErrorRawCoords(func.NameSpan, ErrorCode.Stmt_UnclosedFunction);
		}

		private void ReportError(Span span, ErrorCode errorCode, params object[] args)
		{
			if (_code.DocumentOffset > 0)
			{
				span = new Span(span.Start + _code.DocumentOffset, span.End + _code.DocumentOffset);
			}

			var primarySpan = _source.GetPrimaryFileSpan(span);
			if (primarySpan.Length > 0)
			{
				_errProv.ReportError(primarySpan, errorCode, args);
			}
		}

		private void ReportErrorRawCoords(Span span, ErrorCode errorCode, params object[] args)
		{
			var primarySpan = _source.GetPrimaryFileSpan(span);
			if (primarySpan.Length > 0)
			{
				_errProv.ReportError(primarySpan, errorCode, args);
			}
		}

		private bool ReadStatement()
		{
			if (_code.ReadExact("if")) return ReadIfStatement(_code.TokenSpan);
			if (_code.ReadExact("return")) return ReadReturnStatement(_code.TokenSpan);
			if (_code.ReadExact("switch")) return ReadSwitchStatement(_code.TokenSpan);
			if (_code.ReadExact(';'))
			{
				ReportError(_code.TokenSpan, ErrorCode.Stmt_Empty);
				return true;
			}

			var statementNodes = new List<Node>();
			Node lastNode = null;
			var gotTerminator = false;

			while (!_code.EndOfFile)
			{
				if (_code.ReadExact(';'))
				{
					gotTerminator = true;
					break;
				}
				var node = ReadContent(lastNode);
				if (node == null) break;
				statementNodes.Add(node);
				lastNode = node;
			}

			if (!gotTerminator)
			{
				ReportError(lastNode != null ? lastNode.Span : _code.TokenSpan, ErrorCode.Stmt_NotComplete);
				return false;
			}

			AnalyzeStatementNodes(statementNodes);
			return true;
		}

		/// <summary>
		/// Reads an 'if' statement from the file.
		/// </summary>
		/// <param name="ifSpan">The span of the 'if' word</param>
		/// <returns>True if the if statement was read successfully and is complete; otherwise false.</returns>
		private bool ReadIfStatement(Span ifSpan)
		{
			// Read the condition
			var gotTerminator = false;
			var conditionNodes = new List<Node>();
			var mainBodyStartSpan = Span.Empty;
			var lastNode = null as Node;

			while (!_code.EndOfFile)
			{
				if (_code.ReadExact('{'))
				{
					gotTerminator = true;
					mainBodyStartSpan = _code.TokenSpan;
					break;
				}
				var node = ReadContent(lastNode);
				lastNode = node;
				if (node != null) conditionNodes.Add(node);
				else break;
			}

			if (!gotTerminator)
			{
				ReportError(ifSpan, ErrorCode.If_ConditionNotComplete);
				return false;
			}

			AnalyzeConditionNodes(conditionNodes);

			// Read the main body
			gotTerminator = false;
			while (!_code.EndOfFile)
			{
				if (_code.ReadExact('}'))
				{
					gotTerminator = true;
					break;
				}
				if (!ReadStatement()) break;
			}

			if (!gotTerminator)
			{
				ReportError(mainBodyStartSpan, ErrorCode.If_MainBodyNotComplete);
				return false;
			}

			if (_code.ReadExact("else"))
			{
				var elseSpan = _code.TokenSpan;

				if (_code.ReadExact("if"))
				{
					if (!ReadIfStatement(_code.TokenSpan)) return false;
				}
				else if (_code.ReadExact('{'))
				{
					// Read the else body

					var elseBodyStartSpan = _code.TokenSpan;

					gotTerminator = false;
					while (!_code.EndOfFile)
					{
						if (_code.ReadExact('}'))
						{
							gotTerminator = true;
							break;
						}
						if (!ReadStatement()) break;
					}

					if (!gotTerminator)
					{
						ReportError(elseBodyStartSpan, ErrorCode.If_ElseBodyNotComplete);
						return false;
					}
				}
				else
				{
					ReportError(elseSpan, ErrorCode.If_ElseNotComplete);
					return false;
				}
			}

			return true;
		}

		private bool ReadSwitchStatement(Span switchSpan)
		{
			// Read the condition

			var gotTerminator = false;
			var lastNode = null as Node;
			var conditionNodes = new List<Node>();
			var bodyOpenSpan = Span.Empty;

			while (!_code.EndOfFile)
			{
				if (_code.ReadExact('{'))
				{
					gotTerminator = true;
					bodyOpenSpan = _code.TokenSpan;
					break;
				}
				var node = ReadContent(lastNode);
				lastNode = node;
				if (node != null) conditionNodes.Add(node);
				else break;
			}

			if (!gotTerminator)
			{
				ReportError(switchSpan, ErrorCode.Switch_ConditionNotComplete);
				return false;
			}

			// Read the case statements

			gotTerminator = false;
			var gotDefault = false;
			var caseSpan = Span.Empty;
			var gotCaseOrDefault = false;
			var suppressError_BodyDoesNotStartWithCaseOrDefault = false;

			while (!_code.EndOfFile)
			{
				if (_code.ReadExact('}'))
				{
					gotTerminator = true;
					break;
				}
				else if (_code.ReadExact("case"))
				{
					caseSpan = _code.TokenSpan;
					gotCaseOrDefault = true;

					// Read the case value.
					lastNode = null;
					var gotColon = false;
					var valueNodes = new List<Node>();

					while (!_code.EndOfFile)
					{
						if (_code.ReadExact(':'))
						{
							gotColon = true;
							break;
						}

						var node = ReadContent(lastNode);
						lastNode = node;
						if (node != null) valueNodes.Add(node);
						else break;
					}

					if (!gotColon)
					{
						ReportError(caseSpan, ErrorCode.Switch_CaseValueNotComplete, "case");
						break;
					}

					AnalyzeSwitchCaseValueNodes(valueNodes);
				}
				else if (_code.ReadExact("default"))
				{
					caseSpan = _code.TokenSpan;
					gotCaseOrDefault = true;

					if (gotDefault) ReportError(caseSpan, ErrorCode.Switch_DuplicateDefault);
					gotDefault = true;

					if (!_code.ReadExact(':'))
					{
						ReportError(caseSpan, ErrorCode.Switch_CaseValueNotComplete, "default");
						break;
					}
				}
				else
				{
					if (!ReadStatement()) break;
					if (!gotCaseOrDefault)
					{
						if (!suppressError_BodyDoesNotStartWithCaseOrDefault)
						{
							ReportError(bodyOpenSpan, ErrorCode.Switch_BodyDoesNotStartWithCaseOrDefault);
							suppressError_BodyDoesNotStartWithCaseOrDefault = true;
						}
					}
				}
			}

			if (!gotTerminator)
			{
				ReportError(bodyOpenSpan, ErrorCode.Switch_BodyNotComplete);
				return false;
			}

			return true;
		}

		private bool ReadReturnStatement(Span returnSpan)
		{
			var gotTerminator = false;
			var lastNode = null as Node;
			var returnNodes = new List<Node>();

			while (!_code.EndOfFile)
			{
				if (_code.ReadExact(';'))
				{
					gotTerminator = true;
					break;
				}
				var node = ReadContent(lastNode);
				lastNode = node;
				if (node == null) break;
				returnNodes.Add(node);
			}

			if (!gotTerminator)
			{
				ReportError(returnSpan, ErrorCode.Return_NotComplete);
				return false;
			}

			AnalyzeReturnValueNodes(returnNodes);
			return true;
		}

		private Node ReadContent(Node prevNode)
		{
			// ';', '{', '}', ',' are not considered valid nodes, as they control statement flow

			var startPos = _code.Position;
			if (!_code.Read()) return null;

			switch (_code.TokenType)
			{
				case CodeType.Word:
					{
						var word1 = _code.TokenText;
						var word1Span = _code.TokenSpan;

						if (_code.ReadExact('.'))
						{
							// Dot following this word
							if (_code.ReadWord())
							{
								var word2 = _code.TokenText;
								var word2Span = _code.TokenSpan;

								var table = ProbeEnvironment.GetTable(word1);
								if (table != null)
								{
									var field = table.GetField(word2);
									if (field != null)
									{
										return new TableAndFieldNode(new Span(word1Span, word2Span), field.Definition);
									}
								}

								if (table != null)
								{
									ReportError(word2Span, ErrorCode.DotSepWords_TableFieldNotFound, table.Name, word2);
									return new TableNode(new Span(word1Span, word2Span), table.BaseDefinition);
								}
								else
								{
									ReportError(new Span(word1Span.Start, word2Span.End), ErrorCode.DotSepWords_NotFound, word1, word2);
									return new UnknownNode(new Span(word1Span, word2Span));
								}
							}
							else
							{
								ReportError(_code.TokenSpan, ErrorCode.DotSepWords_NoWord2, word1);
								return new UnknownNode(new Span(word1Span, _code.TokenSpan));
							}
						}
						else if (_code.ReadExact('('))
						{
							var openBracketSpan = _code.TokenSpan;

							// Has a trailing '(', so it must be a function
							foreach (var def in _defProv.GetGlobalFromAnywhere<FunctionDefinition>(word1))
							{
								Span argsSpan;
								ReadFunctionCallArguments(openBracketSpan, out argsSpan);
								return new FunctionCallNode(new Span(word1Span, argsSpan), def);
							}

							ReportError(word1Span, ErrorCode.Stmt_FunctionNotFound, word1);
							return new UnknownNode(word1Span);
						}
						else
						{
							// Nothing after this word

							if (prevNode != null && prevNode.RequiresRValueEnumOption)
							{
								if (prevNode.IsValidRValueEnumOption(word1))
								{
									return new EnumOptionNode(word1Span, word1);
								}
							}

							foreach (var argDef in _func.Arguments)
							{
								if (argDef.Name == word1 && argDef is VariableDefinition) return new ArgumentNode(word1Span, argDef as VariableDefinition);
							}

							foreach (var varDef in _func.Variables)
							{
								if (varDef.Name == word1 && varDef is VariableDefinition) return new VariableNode(word1Span, varDef as VariableDefinition);
							}

							foreach (var def in _defProv.GetGlobalFromAnywhere(word1))
							{
								if (def is FunctionDefinition) continue;	// Would have been caught above when searching for the leading '('
								if (def is VariableDefinition) return new GlobalVariableNode(word1Span, def as VariableDefinition);
							}

							ReportError(word1Span, ErrorCode.Stmt_WordNotFound, word1);
							return new UnknownNode(word1Span);
						}
					}

				case CodeType.StringLiteral:
					{
						var text = _code.TokenText;
						var textSpan = _code.TokenSpan;

						if (prevNode != null && prevNode.RequiresRValueEnumOption)
						{
							var optionText = CodeParser.StringLiteralToString(text);
							if (prevNode.IsValidRValueEnumOption(text))
							{
								return new EnumOptionNode(textSpan, optionText);
							}
						}

						return new StringLiteralNode(textSpan, CodeParser.StringLiteralToString(text));
					}

				case CodeType.Number:
					return new NumberNode(_code.TokenSpan, _code.TokenText);

				case CodeType.Operator:
					{
						var op = _code.TokenText;
						var opSpan = _code.TokenSpan;

						switch (op)
						{
							case "(":
								return ReadBrackets(opSpan);
							case "[":
								return ReadArrayBrackets(opSpan);
							case "{":
								ReportError(opSpan, ErrorCode.Stmt_OpenBraceAlone);
								return null;
							case ")":
							case "}":
							case "]":
								// These tokens control code flow, and shouldn't be reported on here.
								// Back up to before this token.
								_code.Position = _code.TokenSpan.Start;
								return null;
							case ",":
							case ";":
								ReportError(opSpan, ErrorCode.Stmt_InvalidOperator, op);
								return null;
							case "!":
								ReportError(opSpan, ErrorCode.Stmt_InvalidOperator, op);
								return new UnknownNode(opSpan);
							default:
								return new OperatorNode(opSpan, op, prevNode);
						}
					}

				default:
					ReportError(_code.TokenSpan, ErrorCode.Stmt_UnknownToken, _code.TokenText);	// TODO: this needs to go somewhere
					return null;
			}
		}

		private BracketsNode ReadBrackets(Span openSpan)
		{
			var gotTerminator = false;
			var nodes = new List<Node>();
			var lastNode = null as Node;

			while (!_code.EndOfFile)
			{
				if (_code.ReadExact(')'))
				{
					gotTerminator = true;
					break;
				}
				var node = ReadContent(lastNode);
				lastNode = node;
				if (node != null) nodes.Add(node);
				else break;
			}

			if (!gotTerminator)
			{
				ReportError(openSpan, ErrorCode.Stmt_BracketNotClosed, "(");
			}
			
			AnalyzeBracketsNodes(nodes);

			return new BracketsNode(new Span(openSpan, lastNode != null ? lastNode.Span : _code.TokenSpan));
		}

		private ArrayBracketsNode ReadArrayBrackets(Span openSpan)
		{
			var gotTerminator = false;
			var nodes = new List<Node>();
			var lastNode = null as Node;

			while (!_code.EndOfFile)
			{
				if (_code.ReadExact(']'))
				{
					gotTerminator = true;
					break;
				}
				var node = ReadContent(lastNode);
				lastNode = node;
				if (node != null) nodes.Add(node);
				else break;
			}

			if (!gotTerminator)
			{
				ReportError(openSpan, ErrorCode.Stmt_BracketNotClosed, "(");
			}
			
			AnalyzeArrayBracketsNodes(nodes);

			return new ArrayBracketsNode(new Span(openSpan, lastNode != null ? lastNode.Span : _code.TokenSpan));
		}

		private void ReadFunctionCallArguments(Span openBracketSpan, out Span argsSpan)
		{
			var gotTerminator = false;
			var argNodes = new List<Node>();
			var lastNode = null as Node;
			var argCount = 0;
			var terminatorSpan = Span.Empty;

			while (!_code.EndOfFile)
			{
				if (_code.ReadExact(')'))
				{
					gotTerminator = true;
					terminatorSpan = _code.TokenSpan;
					break;
				}

				if (_code.ReadExact(','))
				{
					AnalyzeFunctionCallArgumentNodes(argNodes);
					argNodes.Clear();
					argCount++;
					continue;
				}

				var node = ReadContent(lastNode);
				lastNode = node;
				argNodes.Add(node);
			}

			if (argNodes.Count > 0)
			{
				argCount++;
				AnalyzeFunctionCallArgumentNodes(argNodes);
			}

			// TODO: examine the number of arguments vs the signature

			if (!gotTerminator)
			{
				ReportError(openBracketSpan, ErrorCode.FuncCall_ArgsNotComplete);
				argsSpan = new Span(openBracketSpan, terminatorSpan);
			}
			else
			{
				argsSpan = new Span(openBracketSpan.Start, _code.TokenSpan.End);
			}
		}

		private void AnalyzeConditionNodes(IList<Node> nodes)
		{
			// TODO
		}

		private void AnalyzeStatementNodes(IList<Node> nodes)
		{
			// TODO
		}

		private void AnalyzeFunctionCallArgumentNodes(IList<Node> nodes)
		{
			// TODO
		}

		private void AnalyzeBracketsNodes(IList<Node> nodes)
		{
			// TODO
		}

		private void AnalyzeArrayBracketsNodes(IList<Node> nodes)
		{
			// TODO
		}

		private void AnalyzeSwitchConditionNodes(IList<Node> nodes)
		{
		}

		private void AnalyzeSwitchCaseValueNodes(IList<Node> nodes)
		{
		}

		private void AnalyzeReturnValueNodes(IList<Node> nodes)
		{
		}
	}
}
#endif

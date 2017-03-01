﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DkTools.CodeAnalysis.Nodes;
using DkTools.CodeAnalysis.Statements;
using DkTools.CodeModel;
using DkTools.CodeModel.Definitions;
using DkTools.ErrorTagging;

namespace DkTools.CodeAnalysis
{
	class CodeAnalyzer
	{
		private OutputPane _pane;
		private DkTools.CodeModel.CodeModel _codeModel;
		private PreprocessorModel _prepModel;
		private string _fullSource;
		private Microsoft.VisualStudio.Text.Editor.ITextView _view;

		private List<Statement> _stmts;
		private RunScope _scope;
		private ReadParams _read;
		private int _numErrors;
		private int _numWarnings;

		public CodeAnalyzer(OutputPane pane, DkTools.CodeModel.CodeModel model, Microsoft.VisualStudio.Text.Editor.ITextView view)
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

			ErrorTaskProvider.Instance.FireTagsChangedEvent();

			_pane.WriteLine("Code analysis complete:");
			_pane.WriteLine(string.Format("{0} error(s), {1} warning(s)", _numErrors, _numWarnings));
		}

		private void AnalyzeFunction(CodeModel.PreprocessorModel.LocalFunction func)
		{
			//_pane.WriteLine(string.Format("Analyzing function '{0}'", func.Definition.Name));	// TODO: remove

			var body = _fullSource.Substring(func.StartPos, func.EndPos - func.StartPos);
			if (body.EndsWith("}")) body = body.Substring(0, body.Length - 1);	// End pos is just after the closing brace

			_read = new ReadParams
			{
				CodeAnalyzer = this,
				Code = new CodeParser(body),
				FuncOffset = func.StartPos,
				FuncDef = func.Definition
			};

			_scope = new RunScope(func.Definition, func.StartPos);
			_stmts = new List<Statement>();

			// Parse the function body
			while (!_read.Code.EndOfFile)
			{
				var stmt = Statement.Read(_read);
				if (stmt == null) break;
				_stmts.Add(stmt);
			}

			foreach (var arg in func.Arguments)
			{
				if (!string.IsNullOrEmpty(arg.Name))
				{
					_scope.AddVariable(new Variable(arg.Name, arg.DataType, new Value(arg.DataType), true, true));
				}
			}

			foreach (var v in func.Variables)
			{
				_scope.AddVariable(new Variable(v.Name, v.DataType, new Value(v.DataType), false, false));
			}

			foreach (var v in _prepModel.DefinitionProvider.GetGlobalFromFile<VariableDefinition>())
			{
				_scope.AddVariable(new Variable(v.Name, v.DataType, new Value(v.DataType), false, true));
			}

			foreach (var stmt in _stmts)
			{
				stmt.Execute(_scope);
			}

			if (func.Definition.DataType.ValueType != ValType.Void && _scope.Returned != TriState.True)
			{
				ReportErrorAbsolute(func.NameSpan, CAError.CA0017);	// Function does not return a value.
			}
		}

		public void ReportError(Span span, CAError errorCode, params object[] args)
		{
			ReportErrorAbsolute(span.Offset(_read.FuncOffset), errorCode, args);
		}

		private static readonly Regex _rxExclusion = new Regex(@"CA\d{4}");

		public void ReportErrorAbsolute(Span span, CAError errorCode, params object[] args)
		{
			if (span.IsEmpty)
			{
				Log.Debug("Error {0} not reported because the span is empty.", errorCode);
				return;
			}

			string fileName;
			bool isPrimary;
			var fileSpan = _prepModel.Source.GetFileSpan(span, out fileName, out isPrimary);

			int lineNum = 0, linePos = 0;
			string fileContent = null;
			foreach (var incl in _prepModel.IncludeDependencies)
			{
				if (string.Equals(incl.FileName, fileName, StringComparison.OrdinalIgnoreCase))
				{
					fileContent = incl.Content;
					Util.CalcLineAndPosFromOffset(fileContent, fileSpan.Start, out lineNum, out linePos);
					break;
				}
			}

			if (fileContent != null)
			{
				// Check for any exclusions on this line
				var lineText = GetLineText(fileContent, fileSpan.Start);
				var code = new CodeParser(lineText) { ReturnComments = true };
				while (code.Read())
				{
					if (code.Type != CodeType.Comment) continue;

					foreach (Match match in _rxExclusion.Matches(code.Text))
					{
						CAError excludedErrorCode;
						if (Enum.TryParse<CAError>(match.Value, true, out excludedErrorCode) && excludedErrorCode == errorCode)
						{
							return;
						}
					}
				}
			}

			var message = errorCode.GetText(args);
			var type = errorCode.GetErrorType();

			if (type == ErrorType.Warning) _numWarnings++;
			else _numErrors++;

			_pane.WriteLine(string.Format("{0}({1},{2}) : {3} : {4}", fileName, lineNum + 1, linePos + 1, type, message));

			var task = new ErrorTagging.ErrorTask(fileName, lineNum, linePos, message, type,
				ErrorTaskSource.CodeAnalysis, _codeModel.FileName,
				isPrimary ? _codeModel.Snapshot : null, fileSpan);
			ErrorTaskProvider.Instance.Add(task, true);
		}

		public PreprocessorModel PreprocessorModel
		{
			get { return _prepModel; }
		}

		private string GetLineText(string source, int pos)
		{
			if (pos < 0 || pos >= source.Length) return string.Empty;

			// Find the line start
			var lineStart = 0;
			for (int i = pos; i > 0; i--)
			{
				if (source[i] == '\n')
				{
					lineStart = i + 1;
					break;
				}
			}

			// Find the line end
			var lineEnd = source.Length;
			for (int i = lineStart; i < source.Length; i++)
			{
				if (source[i] == '\r' || source[i] == '\n')
				{
					lineEnd = i;
					break;
				}
			}

			return source.Substring(lineStart, lineEnd - lineStart);
		}
	}

	class CAException : Exception
	{
		public CAException(string message)
			: base(message)
		{
		}
	}

	class ReadParams
	{
		public CodeAnalyzer CodeAnalyzer { get; set; }
		public CodeParser Code { get; set; }
		public Statement Statement { get; set; }
		public int FuncOffset { get; set; }
		public FunctionDefinition FuncDef { get; set; }

		public ReadParams Clone(Statement stmt)
		{
			return new ReadParams
			{
				CodeAnalyzer = CodeAnalyzer,
				Code = Code,
				Statement = stmt,
				FuncOffset = FuncOffset,
				FuncDef = FuncDef
			};
		}
	}
}

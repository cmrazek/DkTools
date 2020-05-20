using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DkTools.CodeAnalysis.Nodes;
using DkTools.CodeAnalysis.Statements;
using DkTools.CodeAnalysis.Values;
using DkTools.CodeModel;
using DkTools.CodeModel.Definitions;
using DkTools.ErrorTagging;

namespace DkTools.CodeAnalysis
{
	class CodeAnalyzer
	{
		private DkTools.CodeModel.CodeModel _codeModel;
		private PreprocessorModel _prepModel;
		private string _fullSource;
		private WarningSuppressionTracker _warningSuppressions;

		private List<Statement> _stmts;
		private RunScope _scope;
		private ReadParams _read;
		private List<ErrorTask> _tasks = new List<ErrorTask>();
		private int _numErrors;
		private int _numWarnings;

		public CodeAnalyzer(OutputPane pane, DkTools.CodeModel.CodeModel model)
		{
			if (model == null) throw new ArgumentNullException("model");

			_codeModel = model;
		}

		public void Run()
		{
			Log.Debug("Starting code analysis on file: {0}", _codeModel.FileName);
			var startTime = DateTime.Now;

			_prepModel = _codeModel.PreprocessorModel;
			_fullSource = _codeModel.Source.Text;
			_warningSuppressions = _codeModel.PreprocessorModel.Preprocessor.WarningSuppressions;

			foreach (var func in _prepModel.LocalFunctions)
			{
				AnalyzeFunction(func);
			}

			if (_numErrors > 0 || _numWarnings > 0)
			{
				ErrorTaskProvider.Instance.ReplaceForSourceAndInvokingFile(
					ErrorTaskSource.CodeAnalysis, _codeModel.FileName, _tasks);
			}

			Log.Debug("Completed code analysis (elapsed: {0} msec)", DateTime.Now.Subtract(startTime).TotalMilliseconds);
		}

		private void AnalyzeFunction(CodeModel.PreprocessorModel.LocalFunction func)
		{
			var body = _fullSource.Substring(func.StartPos, func.EndPos - func.StartPos);
			if (body.EndsWith("}")) body = body.Substring(0, body.Length - 1);	// End pos is just after the closing brace

			_read = new ReadParams
			{
				CodeAnalyzer = this,
				Code = new CodeParser(body),
				FuncOffset = func.StartPos,
				FuncDef = func.Definition
			};

			_scope = new RunScope(this, func.Definition, func.StartPos);
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
				if (!string.IsNullOrEmpty(arg.Definition.Name))
				{
					_scope.AddVariable(new Variable(arg.Definition, arg.Definition.Name, arg.Definition.DataType,
						Value.CreateUnknownFromDataType(arg.Definition.DataType), true, TriState.True, true, arg.RawSpan));
				}
			}

			foreach (var v in func.Variables)
			{
				_scope.AddVariable(new Variable(v.Definition, v.Definition.Name, v.Definition.DataType,
					Value.CreateUnknownFromDataType(v.Definition.DataType), false, TriState.False, false, v.RawSpan));
			}

			foreach (var v in _prepModel.GlobalVariables)
			{
				_scope.AddVariable(new Variable(v.Definition, v.Definition.Name, v.Definition.DataType,
					Value.CreateUnknownFromDataType(v.Definition.DataType), false, TriState.True, true, v.RawSpan));
			}

			foreach (var stmt in _stmts)
			{
				stmt.Execute(_scope);
			}

			if (func.Definition.DataType.ValueType != ValType.Void && _scope.Returned != TriState.True &&
				func.Definition.Name != "staticinitialize")
			{
				ReportErrorAbsolute(func.NameSpan, CAError.CA0017);	// Function does not return a value.
			}

			foreach (var v in _scope.Variables)
			{
				if (!v.IsUsed)
				{
					if (v.IsInitialized != TriState.False)
					{
						var def = v.Definition;
						ReportErrorAbsolute(v.RawSpan, CAError.CA0111, v.Name); // Variable '{0}' is assigned a value, but is never used.
					}
					else
					{
						var def = v.Definition;
						ReportErrorAbsolute(v.RawSpan, CAError.CA0112, v.Name); // Variable '{0}' is not used.
					}
				}
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
				return;
			}

			if (errorCode == CAError.CA0110)
			{
				var TODO = 0;
			}

			if (int.TryParse(errorCode.ToString().Substring(2), out int code))
			{
				if (_warningSuppressions.IsWarningSuppressed(code, span.Start))
				{
					return;
				}
			}

			var fileSpan = _prepModel.Source.GetFileSpan(span, out var fileName, out var _);
			
			string fileContent = null;
			foreach (var incl in _prepModel.IncludeDependencies)
			{
				if (string.Equals(incl.FileName, fileName, StringComparison.OrdinalIgnoreCase))
				{
					fileContent = incl.Content;
					break;
				}
			}

			ReportErrorLocal_Internal(fileName, fileSpan, fileContent, errorCode, args);
		}

		private void ReportErrorLocal_Internal(string filePath, Span fileSpan, string fileContent, CAError errorCode, params object[] args)
		{
			int lineNum = 0, linePos = 0;

			if (fileContent == null)
			{
				foreach (var incl in _prepModel.IncludeDependencies)
				{
					if (string.Equals(incl.FileName, filePath, StringComparison.OrdinalIgnoreCase))
					{
						fileContent = incl.Content;
						break;
					}
				}
			}

			if (fileContent != null)
			{
				Util.CalcLineAndPosFromOffset(fileContent, fileSpan.Start, out lineNum, out linePos);

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

			Log.Debug("{0}({1},{2}) : {3} : {4} Span [{5}]", filePath, lineNum + 1, linePos + 1, type, message, fileSpan);

			var task = new ErrorTask(
				invokingFilePath: _codeModel.FileName,
				filePath: filePath,
				lineNum: lineNum,
				lineCol: linePos,
				message: message,
				type: ErrorType.CodeAnalysisError,
				source: ErrorTaskSource.CodeAnalysis,
				reportedSpan: fileSpan,
				snapshotSpan: null);

			_tasks.Add(task);
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

		public CodeModel.CodeModel CodeModel
		{
			get { return _codeModel; }
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

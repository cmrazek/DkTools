using DK.AppEnvironment;
using DK.Code;
using DK.CodeAnalysis.Statements;
using DK.CodeAnalysis.Values;
using DK.Definitions;
using DK.Diagnostics;
using DK.Modeling;
using DK.Preprocessing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace DK.CodeAnalysis
{
    public class CodeAnalyzer
    {
        private DkAppContext _app;
        private CodeModel _codeModel;
        private PreprocessorModel _prepModel;
        private string _fullSource;
        private WarningSuppressionTracker _warningSuppressions;
        private string _filePath;
        private CAOptions _options;
        private CancellationToken _cancel;

        private List<Statement> _stmts;
        private CAScope _scope;
        private ReadParams _read;
        private List<CAErrorTask> _mainFileTasks = new List<CAErrorTask>();
        private List<CAErrorTask> _includeFileTasks = new List<CAErrorTask>();
        private List<CAErrorMarker> _tags = new List<CAErrorMarker>();

        public CodeAnalyzer(DkAppContext app, CodeModel model)
        {
            _app = app ?? throw new ArgumentNullException(nameof(app));
            _codeModel = model ?? throw new ArgumentNullException(nameof(model));
            _filePath = model.FilePath;
        }

        public CodeModel CodeModel => _codeModel;
        public CAOptions Options => _options;

        public CodeAnalysisResults RunAndGetResults(CAOptions options, CancellationToken cancel)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _cancel = cancel;

            _app.Log.Debug("Starting code analysis on file: {0}", _codeModel.FilePath);
            var startTime = DateTime.Now;

            _prepModel = _codeModel.PreprocessorModel;
            _fullSource = _codeModel.Source.Text;
            _warningSuppressions = _codeModel.PreprocessorModel.WarningSuppressions;

            foreach (var err in _prepModel.Errors)
            {
                if (err.LocalFileName == null)
                {
                    ReportErrorAbsolute(err.Span, err.ErrorCode, log: true, err.Args);
                }
                else
                {
                    ReportError_Local(err.LocalFileName, err.Span, err.ErrorCode, log: true, err.Args);
                }
            }

            foreach (var func in _prepModel.LocalFunctions)
            {
                _cancel.ThrowIfCancellationRequested();

                AnalyzeFunction(func);

                var dupeFunc = _prepModel.LocalFunctions
                    .Where(x => x.Definition is FunctionDefinition
                        && x.Definition.Name == func.Definition.Name
                        && x.NameSpan.Start < func.NameSpan.Start)
                    .FirstOrDefault();
                if (dupeFunc != null)
                {
                    ReportErrorAbsolute(func.NameSpan, CAError.CA10190, log:true, func.Definition.Name);  // Function '{0}' has already been defined.
                }
            }

            _app.Log.Debug("Completed code analysis (elapsed: {0} msec)", DateTime.Now.Subtract(startTime).TotalMilliseconds);
            var tasks = _mainFileTasks.Concat(_includeFileTasks).Take(_options.MaxWarnings != 0 ? _options.MaxWarnings : int.MaxValue).ToList();
            return new CodeAnalysisResults(tasks, _tags);
        }

        private void AnalyzeFunction(PreprocessorModel.LocalFunction func)
        {
            var body = _fullSource.Substring(func.StartPos, func.EndPos - func.StartPos);
            if (body.EndsWith("}")) body = body.Substring(0, body.Length - 1);  // End pos is just after the closing brace

            _read = new ReadParams(
                codeAnalyzer: this,
                code: new CodeParser(body),
                statement: null,
                funcOffset: func.StartPos,
                funcDef: func.Definition,
                appSettings: _codeModel.AppSettings);

            _scope = new CAScope(this, func.Definition, func.StartPos, _codeModel.AppSettings);
            _stmts = new List<Statement>();

            // Parse the function body
            while (!_read.Code.EndOfFile)
            {
                _cancel.ThrowIfCancellationRequested();

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

                var dupeArg = func.Arguments
                    .Where(x => !string.IsNullOrEmpty(x.Definition.Name)
                        && x.Definition.Name == arg.Definition.Name
                        && x.RawSpan.Start < arg.RawSpan.Start)
                    .FirstOrDefault();
                if (dupeArg != null)
                {
                    ReportErrorAbsolute(arg.RawSpan, CAError.CA10191, log: true, arg.Definition.Name); // Argument '{0}' has already been declared.
                }
            }

            foreach (var v in func.Variables)
            {
                _scope.AddVariable(new Variable(
                    def: v.Definition,
                    name: v.Definition.Name,
                    dataType: v.Definition.DataType,
                    value: Value.CreateUnknownFromDataType(v.Definition.DataType),
                    isArg: false,
                    isInitialized: TriStateUtil.Create(v.Definition.DataType.IsVariableInitializedAutomatically),
                    isUsed: false,
                    rawSpan: v.RawSpan));

                var dupeVar = func.Variables
                    .Where(x => x.Definition.Name == v.Definition.Name && x.RawSpan.Start < v.RawSpan.Start)
                    .FirstOrDefault();
                if (dupeVar != null)
                {
                    ReportErrorAbsolute(v.RawSpan, CAError.CA10113, log: true, v.Definition.Name); // Variable '{0}' has already been declared.
                }
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
                ReportErrorAbsolute(func.NameSpan, CAError.CA10017, log: true);	// Function does not return a value.
            }

            foreach (var v in _scope.Variables)
            {
                if (!v.IsUsed)
                {
                    if (v.IsInitialized != TriState.False)
                    {
                        var def = v.Definition;
                        ReportErrorAbsolute(v.RawSpan, CAError.CA10111, log: true, v.Name); // Variable '{0}' is assigned a value, but is never used.
                    }
                    else
                    {
                        var def = v.Definition;
                        ReportErrorAbsolute(v.RawSpan, CAError.CA10112, log: true, v.Name); // Variable '{0}' is not used.
                    }
                }
            }
        }

        public void ReportError(CodeSpan span, CAError errorCode, params object[] args)
        {
            ReportErrorAbsolute(span.Offset(_read.FuncOffset), errorCode, log: true, args);
        }

        public CAErrorTask? ReportErrorButDontLog(CodeSpan span, CAError errorCode, params object[] args)
        {
            return ReportErrorAbsolute(span.Offset(_read.FuncOffset), errorCode, log: false, args);
        }

        private static readonly Regex _rxExclusion = new Regex(@"CA\d{4}");

        private CAErrorTask? ReportErrorAbsolute(CodeSpan span, CAError errorCode, bool log, params object[] args)
        {
            if (span.IsEmpty)
            {
                return null;
            }

            if (int.TryParse(errorCode.ToString().Substring(2), out int code))
            {
                if (_warningSuppressions.IsWarningSuppressed(code, span.Start))
                {
                    return null;
                }
            }

            CodeSpan fileSpan;
            string filePath;
            var mainFile = false;
            var errorType = errorCode.GetErrorType();
            if (errorType == CAErrorType.ReportOutputTag)
            {
                fileSpan = _prepModel.Source.GetPrimaryFileSpan(span);
                if (span.IsEmpty) return null;

                filePath = _filePath;
                mainFile = true;
            }
            else
            {
                if (_options.MaxWarnings != 0 && _options.MaxWarnings <= _mainFileTasks.Count) return null;

                fileSpan = _prepModel.Source.GetFileSpan(span, out filePath, out var _);

                if (filePath != null && _filePath.EqualsI(filePath))
                {
                    mainFile = true;
                }
                else
                {
                    if (_options.MaxWarnings != 0 && _options.MaxWarnings <= _includeFileTasks.Count) return null;
                }
            }

            string fileContent = null;
            foreach (var incl in _prepModel.IncludeDependencies)
            {
                if (incl.FileName.EqualsI(filePath))
                {
                    fileContent = incl.Content;
                    break;
                }
            }

            return ReportErrorLocal_Internal(filePath, fileSpan, mainFile, fileContent, errorCode, log, args);
        }

        public CAErrorTask? ReportError_Local(string localFileName, CodeSpan localSpan, CAError errorCode, bool log, params object[] args)
        {
            if (localSpan.IsEmpty) return null;

            bool mainFile = false;
            var errorType = errorCode.GetErrorType();
            if (errorType == CAErrorType.ReportOutputTag)
            {
                mainFile = true;
            }
            else
            {
                if (_options.MaxWarnings != 0 && _options.MaxWarnings <= _mainFileTasks.Count) return null;
                mainFile = string.Equals(localFileName, _filePath, StringComparison.OrdinalIgnoreCase);
            }

            string fileContent = null;
            foreach (var incl in _prepModel.IncludeDependencies)
            {
                if (incl.FileName.EqualsI(localFileName))
                {
                    fileContent = incl.Content;
                    break;
                }
            }

            return ReportErrorLocal_Internal(localFileName, localSpan, mainFile, fileContent, errorCode, log, args);
        }

        private CAErrorTask? ReportErrorLocal_Internal(string filePath, CodeSpan fileSpan, bool mainFile, string fileContent,
            CAError errorCode, bool log, params object[] args)
        {
            int lineNum = 0, linePos = 0;

            var type = errorCode.GetErrorType();

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
                StringHelper.CalcLineAndPosFromOffset(fileContent, fileSpan.Start, out lineNum, out linePos);

                if (fileSpan.IsEmpty && (type == CAErrorType.Error || type == CAErrorType.Warning))
                {
                    fileSpan = StringHelper.GetSpanForLine(fileContent, fileSpan.Start, excludeWhiteSpace: true);
                }

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
                            return null;
                        }
                    }
                }
            }

            if (mainFile)
            {
                if (_mainFileTasks.Any(x => x.ErrorCode == errorCode && x.Span == fileSpan)) return null;    // Duplicate error report
            }
            else
            {
                if (_includeFileTasks.Any(x => x.ErrorCode == errorCode && x.Span == fileSpan && x.FilePath.EqualsI(filePath))) return null; // Duplicate error report
            }

            var message = errorCode.GetText(args);

            if (type == CAErrorType.ReportOutputTag)
            {
                _tags.Add(new CAErrorMarker(filePath, fileSpan));
                return null;
            }

            var task = new CAErrorTask(errorCode, message, filePath, fileSpan, lineNum, linePos, _codeModel.FilePath, mainFile);
            if (log)
            {
                if (mainFile) _mainFileTasks.Add(task);
                else _includeFileTasks.Add(task);
            }

            return task;
        }

        public void LogError(CAErrorTask? task)
        {
            if (task == null) return;
            if (task.Value.MainFile) _mainFileTasks.Add(task.Value);
            else _includeFileTasks.Add(task.Value);
        }

        public void LogErrors(IEnumerable<CAErrorTask?> tasks)
        {
            if (tasks == null) return;
            foreach (var task in tasks) LogError(task);
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
        public CodeAnalyzer CodeAnalyzer { get; private set; }
        public CodeParser Code { get; private set; }
        public Statement Statement { get; private set; }
        public int FuncOffset { get; private set; }
        public FunctionDefinition FuncDef { get; private set; }
        public DkAppSettings AppSettings { get; private set; }

        public ReadParams(
            CodeAnalyzer codeAnalyzer,
            CodeParser code,
            Statement statement,
            int funcOffset,
            FunctionDefinition funcDef,
            DkAppSettings appSettings)
        {
            CodeAnalyzer = codeAnalyzer;
            Code = code;
            Statement = statement;
            FuncOffset = funcOffset;
            FuncDef = funcDef;
            AppSettings = appSettings;
        }

        private ReadParams() { }

        public ReadParams Clone(Statement stmt)
        {
            return new ReadParams
            {
                CodeAnalyzer = CodeAnalyzer,
                Code = Code,
                Statement = stmt,
                FuncOffset = FuncOffset,
                FuncDef = FuncDef,
                AppSettings = AppSettings
            };
        }
    }
}

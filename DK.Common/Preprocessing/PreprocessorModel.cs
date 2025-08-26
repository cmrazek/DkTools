using DK.AppEnvironment;
using DK.Code;
using DK.CodeAnalysis;
using DK.Definitions;
using DK.Modeling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace DK.Preprocessing
{
    public class PreprocessorModel
    {
        private readonly DkAppSettings _appSettings;
        private readonly CodeSource _source;
        private readonly CodeParser _code;
        private readonly DefinitionProvider _defProv;
        private readonly string _fileName;
        private readonly string _fileNameWithoutExtension;
        private readonly string _className;
        private readonly Dictionary<string, PrepVariable> _globalVars = new Dictionary<string, PrepVariable>();
        private readonly Dictionary<string, FunctionDefinition> _externFuncs = new Dictionary<string, FunctionDefinition>();
        private readonly List<LocalFunction> _localFuncs = new List<LocalFunction>();
        private readonly FileContext _fileContext;
        private readonly bool _visible;
        private readonly IncludeDependency[] _includeDependencies;
        private Preprocessor _prep;
        private readonly List<Definition> _globalDefs = new List<Definition>();
        private readonly CancellationToken _cancel;
        private readonly List<PrepError> _errors = new List<PrepError>();
        private readonly CodeScanMode _scanMode;

        internal PreprocessorModel(
            DkAppSettings appSettings,
            CodeSource source,
            DefinitionProvider definitionProvider,
            string fileName,
            bool visible,
            IEnumerable<IncludeDependency> includeDependencies,
            CodeScanMode scanMode,
            CancellationToken cancel)
        {
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _code = new CodeParser(source.Text);
            _defProv = definitionProvider ?? throw new ArgumentNullException(nameof(definitionProvider));
            _fileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
            _fileNameWithoutExtension = PathUtil.GetFileNameWithoutExtension(fileName);
            FileContextHelper.FileNameIsClass(_fileName, out _className);
            _fileContext = FileContextHelper.GetFileContextFromFileName(fileName);
            _visible = visible;
            _cancel = cancel;
            _scanMode = scanMode;

            if (includeDependencies != null) _includeDependencies = includeDependencies.ToArray();
            else _includeDependencies = IncludeDependency.EmptyArray;

            Parse();
        }

        public IEnumerable<PrepVariable> GlobalVariables => _globalVars.Values;
        public WarningSuppressionTracker WarningSuppressions => _prep.WarningSuppressions;

        public CodeSource Source
        {
            get { return _source; }
        }

        private void Parse()
        {
            while (!_code.EndOfFile)
            {
                _cancel.ThrowIfCancellationRequested();

                if (TryReadDataType(out DataType dataType, out int pos))
                {
                    AfterRootDataType(dataType, pos, FunctionPrivacy.Public, false, 0);
                    continue;
                }

                if (!_code.Read()) break;

                switch (_code.Type)
                {
                    case CodeType.Word:
                        switch (_code.Text)
                        {
                            case "static":
                                AfterRootStatic(_code.TokenStartPostion, 0);
                                break;
                            case "public":
                                AfterRootPrivacy(_code.TokenStartPostion, FunctionPrivacy.Public, 0);
                                break;
                            case "private":
                                AfterRootPrivacy(_code.TokenStartPostion, FunctionPrivacy.Private, 0);
                                break;
                            case "protected":
                                AfterRootPrivacy(_code.TokenStartPostion, FunctionPrivacy.Protected, 0);
                                break;
                            case "extern":
                                AfterRootExtern(0);
                                break;
                            default:
                                AfterRootIdentifier(_code.Text, _code.TokenStartPostion, _code.Span, false, 0);
                                break;
                        }
                        break;
                    case CodeType.Preprocessor:
                        switch (_code.Text)
                        {
                            case "#SQLWhereClauseCompatibleAttribute":
                                AfterRootSqlWherePreprocessor(FunctionFlags.SQLWhereClauseCompatibleAttribute);
                                break;
                            case "#SQLResultsFilteringAttribute":
                                AfterRootSqlWherePreprocessor(FunctionFlags.SQLResultsFilteringAttribute);
                                break;
                        }
                        break;
                }
            }
        }

        private DataTypeDefinition GlobalDataTypeCallback(string name)
        {
            var def = _defProv.GetGlobalFromAnywhere(name).FirstOrDefault();
            return def as DataTypeDefinition;
        }

        private VariableDefinition GlobalVariableCallback(string name)
        {
            if (_globalVars.TryGetValue(name, out var def)) return def.Definition;
            return null;
        }

        private void AfterRootSqlWherePreprocessor(FunctionFlags flags)
        {
            if (TryReadDataType(out var dataType, out var pos))
            {
                AfterRootDataType(dataType, pos, FunctionPrivacy.Public, false, flags);
                return;
            }

            if (!_code.Read()) return;

            switch (_code.Type)
            {
                case CodeType.Word:
                    switch (_code.Text)
                    {
                        case "static":
                            AfterRootStatic(_code.TokenStartPostion, flags);
                            break;
                        case "public":
                            AfterRootPrivacy(_code.TokenStartPostion, FunctionPrivacy.Public, flags);
                            break;
                        case "private":
                            AfterRootPrivacy(_code.TokenStartPostion, FunctionPrivacy.Private, flags);
                            break;
                        case "protected":
                            AfterRootPrivacy(_code.TokenStartPostion, FunctionPrivacy.Protected, flags);
                            break;
                        case "extern":
                            AfterRootExtern(flags);
                            break;
                        default:
                            AfterRootIdentifier(_code.Text, _code.TokenStartPostion, _code.Span, false, flags);
                            break;
                    }
                    break;
                case CodeType.Preprocessor:
                    switch (_code.Text)
                    {
                        case "#SQLWhereClauseCompatibleAttribute":
                            if (flags.HasFlag(FunctionFlags.SQLWhereClauseCompatibleAttribute))
                            {
                                ReportError(_code.Span, CAError.CA10078);   // Duplicate #SQLWhereClauseCompatibleAttribute.
                            }
                            else if (flags.HasFlag(FunctionFlags.SQLResultsFilteringAttribute))
                            {
                                ReportError(_code.Span, CAError.CA10079);   // #SQLResultsFilteringAttribute cannot be used with #SQLWhereClauseCompatibleAttribute.
                            }
                            AfterRootSqlWherePreprocessor(flags);
                            break;
                        case "#SQLResultsFilteringAttribute":
                            if (flags.HasFlag(FunctionFlags.SQLResultsFilteringAttribute))
                            {
                                ReportError(_code.Span, CAError.CA10080);   // Duplicate #SQLResultsFilteringAttribute.
                            }
                            else if (flags.HasFlag(FunctionFlags.SQLWhereClauseCompatibleAttribute))
                            {
                                ReportError(_code.Span, CAError.CA10081);   // #SQLWhereClauseCompatibleAttribute cannot be used with #SQLResultsFilteringAttribute.
                            }
                            AfterRootSqlWherePreprocessor(FunctionFlags.SQLResultsFilteringAttribute);
                            break;
                    }
                    break;
            }
        }

        private void AfterRootDataType(DataType dataType, int dataTypeStartPos, FunctionPrivacy privacy, bool isExtern, FunctionFlags flags)
        {
#if DEBUG
            if (dataType == null) throw new ArgumentNullException("dataType");
#endif
            if (_code.ReadWord())
            {
                var name = _code.Text;
                var nameSpan = _code.Span;

                var arrayLength = TryReadArrayDecl();

                if (arrayLength == null && _code.ReadExact('('))
                {
                    StartFunctionArgs(name, dataTypeStartPos, _code.TokenStartPostion, nameSpan, dataType, privacy, isExtern, flags);
                }
                else if (_code.ReadExact(';'))
                {
                    var localPos = _source.GetFilePosition(nameSpan.Start);
                    var def = new VariableDefinition(name, localPos, dataType, false, arrayLength, VariableType.Global, argPassByMethod: null);
                    _globalVars[name] = new PrepVariable(def, nameSpan);
                    AddGlobalDefinition(def);
                }
                else if (_code.ReadExact(','))
                {
                    var localPos = _source.GetFilePosition(nameSpan.Start);
                    var def = new VariableDefinition(name, localPos, dataType, false, arrayLength, VariableType.Global, argPassByMethod: null);
                    _globalVars[name] = new PrepVariable(def, nameSpan);
                    AddGlobalDefinition(def);
                    AfterRootDataType(dataType, dataTypeStartPos, privacy, isExtern, flags);
                }
            }
        }

        private void AfterRootStatic(int startPos, FunctionFlags flags)
        {
            var dataType = DataType.TryParse(new DataType.ParseArgs(_code, _appSettings)
            {
                DataTypeCallback = GlobalDataTypeCallback,
                VariableCallback = GlobalVariableCallback
            });
            if (dataType != null)
            {
                AfterRootDataType(dataType, startPos, FunctionPrivacy.Public, false, flags);
            }
        }

        private void AfterRootExtern(FunctionFlags flags)
        {
            var dataTypeStartPos = _code.Position;
            var dataType = DataType.TryParse(new DataType.ParseArgs(_code, _appSettings)
            {
                DataTypeCallback = GlobalDataTypeCallback,
                VariableCallback = GlobalVariableCallback
            });
            if (dataType != null)
            {
                AfterRootDataType(dataType, dataTypeStartPos, FunctionPrivacy.Public, true, flags);
            }
            else if (_code.ReadWord())
            {
                AfterRootIdentifier(_code.Text, _code.TokenStartPostion, _code.Span, true, flags);
            }
        }

        private void AfterRootIdentifier(string word, int startPos, CodeSpan wordSpan, bool isExtern, FunctionFlags flags)
        {
            if (_code.ReadExact('('))
            {
                // This function only gets called when no datatype was found. Assume int return type and public.
                StartFunctionArgs(word, startPos, _code.TokenStartPostion, wordSpan, DataType.Int, FunctionPrivacy.Public, isExtern, flags);
            }
        }

        private void AfterRootPrivacy(int startPos, FunctionPrivacy privacy, FunctionFlags flags)
        {
            _code.SkipWhiteSpace();
            var dataTypeStartPos = _code.Position;
            var dataType = DataType.TryParse(new DataType.ParseArgs(_code, _appSettings)
            {
                DataTypeCallback = GlobalDataTypeCallback,
                VariableCallback = GlobalVariableCallback
            });
            if (dataType != null)
            {
                AfterRootDataType(dataType, dataTypeStartPos, privacy, false, flags);
            }
            else if (_code.ReadWord())
            {
                var funcName = _code.Text;
                var funcNameSpan = _code.Span;
                if (_code.ReadExact('('))
                {
                    StartFunctionArgs(funcName, startPos, _code.TokenStartPostion, funcNameSpan, DataType.Int, privacy, false, flags);
                }
            }
        }

        private bool TryReadDataType(out DataType dataTypeOut, out int startPosOut)
        {
            _code.SkipWhiteSpace();
            var pos = _code.Position;
            var dataType = DataType.TryParse(new DataType.ParseArgs(_code, _appSettings)
            {
                DataTypeCallback = GlobalDataTypeCallback,
                VariableCallback = GlobalVariableCallback
            });
            if (dataType != null)
            {
                dataTypeOut = dataType;
                startPosOut = pos;
                return true;
            }

            dataTypeOut = null;
            startPosOut = 0;
            return false;
        }

        private void StartFunctionArgs(string funcName, int allStartPos, int argStartPos, CodeSpan nameSpan,
            DataType returnDataType, FunctionPrivacy privacy, bool isExtern, FunctionFlags flags)
        {
            var localArgStartPos = _source.GetFilePosition(argStartPos);
            var argScope = new CodeScope(localArgStartPos.Position);
            int argEndPos = 0;
            var args = new List<ArgumentDescriptor>();
            var argDefList = new List<PrepVariable>();

            // Read the arguments
            while (true)
            {
                if (_code.ReadExact(')'))
                {
                    argEndPos = _code.Position;
                    break;
                }
                if (_code.ReadExact(','))
                {
                    continue;
                }
                if (TryReadFunctionArgument(argScope, !_visible || localArgStartPos.PrimaryFile, args, argDefList)) continue;
                return;
            }

            if (!ReadFunctionAttributes(funcName, out int bodyStartPos, out string description, isExtern)) return;

            FunctionFlags funcFlags = flags;
            if (_scanMode == CodeScanMode.BackgroundScanner && _fileContext == FileContext.Function &&
                !string.Equals(funcName, _fileNameWithoutExtension, StringComparison.OrdinalIgnoreCase))
            {
                funcFlags |= FunctionFlags.NotGlobal;
            }

            if (isExtern)
            {
                var localPos = _source.GetFilePosition(nameSpan.Start);

                var funcSig = new FunctionSignature(
                    isExtern: true,
                    privacy: privacy,
                    returnDataType: returnDataType,
                    className: isExtern ? null : _className,
                    funcName: funcName,
                    devDesc: description,
                    args: args,
                    serverContext: _fileContext.ToServerContext(),
                    flags: funcFlags);
                funcSig.ApplyDocumentation(localPos.FileName);
                var def = new FunctionDefinition(
                    signature: funcSig,
                    filePos: localPos,
                    argsStartPos: 0,
                    argsEndPos: 0,
                    bodyStartPos: 0,
                    entireSpan: CodeSpan.Empty,
                    hasVariableArgumentCount: false,
                    flags: funcFlags);
                _externFuncs[funcName] = def;
                AddGlobalDefinition(def);
                return;
            }

            // Read the variables at the start of the function.
            var localBodyStartPos = _source.GetFilePosition(bodyStartPos);
            CodeScope funcScope = null;
            var varList = new List<PrepVariable>();
            if (!_visible || localBodyStartPos.PrimaryFile) // Don't worry about saving variables if this function isn't in this file anyway.
            {
                funcScope = new CodeScope(argScope, bodyStartPos);
                while (!_code.EndOfFile)
                {
                    if (!TryReadVariableDeclaration(funcScope, varList)) break;
                }
            }

            var statementsStartPos = _code.Position;

            ReadFunctionBody();
            var bodyEndPos = _code.Position;

            var bodyStartLocalPos = _source.GetPrimaryFilePosition(bodyStartPos);
            var nameActualPos = _source.GetFilePosition(nameSpan.Start);
            var argStartPrimaryPos = _source.GetPrimaryFilePosition(argStartPos);
            var argEndPrimaryPos = _source.GetPrimaryFilePosition(argEndPos);
            var entireSpan = _source.GetPrimaryFileSpan(new CodeSpan(allStartPos, bodyEndPos));

            funcFlags = flags;
            if (_scanMode == CodeScanMode.BackgroundScanner && _fileContext == FileContext.Function &&
                !string.Equals(funcName, _fileNameWithoutExtension, StringComparison.OrdinalIgnoreCase))
            {
                funcFlags |= FunctionFlags.NotGlobal;
            }

            var sig = new FunctionSignature(
                isExtern: false,
                privacy: privacy,
                returnDataType: returnDataType,
                className: _className,
                funcName: funcName,
                devDesc: description,
                args: args,
                serverContext: _fileContext.ToServerContext(),
                flags: funcFlags);
            var funcDef = new FunctionDefinition(
                signature: sig,
                filePos: nameActualPos,
                argsStartPos: argStartPrimaryPos,
                argsEndPos: argEndPrimaryPos,
                bodyStartPos: bodyStartLocalPos,
                entireSpan: entireSpan,
                hasVariableArgumentCount: false,
                flags: funcFlags);

            _localFuncs.Add(new LocalFunction(funcDef, nameSpan, statementsStartPos, bodyEndPos, argDefList, varList));
            AddGlobalDefinition(funcDef);

            // Add the definitions for the argument list
            CodeSpan argEffect;
            if (_visible) argEffect = new CodeSpan(localArgStartPos.Position, _source.GetPrimaryFilePosition(bodyEndPos));
            else argEffect = new CodeSpan(argStartPos, bodyEndPos);

            _defProv.AddLocal(argEffect, argDefList.Select(x => x.Definition));

            // Add the definitions for the declared variables
            if (varList != null)
            {
                CodeSpan varEffect;
                if (_visible) varEffect = new CodeSpan(bodyStartLocalPos, _source.GetPrimaryFilePosition(bodyEndPos));
                else varEffect = new CodeSpan(bodyStartPos, bodyEndPos);

                _defProv.AddLocal(varEffect, varList.Select(x => x.Definition));
            }
        }

        private void ReadFunctionBody()
        {
            ReadBraceScope();
        }

        private void ReadBraceScope()
        {
            // Read the contents of the function until '}'
            while (!_code.EndOfFile)
            {
                if (_code.ReadExact('}')) break;

                if (_code.ReadWord())
                {
                    switch (_code.Text)
                    {
                        case "extract":
                            ReadExtract();
                            break;
                        default:
                            // unknown word
                            break;
                    }
                    continue;
                }

                if (TryReadNestable()) continue;

                // Other tokens mean nothing to the preprocessor.
                _code.Read();
            }
        }

        private static readonly Regex _rxAccelWord = new Regex(@"^(CTRL|ALT|SHIFT|F\d{1,2}|[A-Za-z])$");

        private bool ReadFunctionAttributes(string funcName, out int bodyStartPos, out string devDesc, bool isExtern)
        {
            // Read the function attributes until '{'

            bodyStartPos = -1;
            devDesc = null;

            while (true)
            {
                if (!isExtern && _code.ReadExact('{'))
                {
                    bodyStartPos = _code.TokenStartPostion;
                    return true;
                }

                if (isExtern && _code.ReadExact(';'))
                {
                    bodyStartPos = 0;
                    return true;
                }

                if (_code.ReadWord())
                {
                    var word = _code.Text;
                    if (word == "description")
                    {
                        var sb = new StringBuilder();
                        while (_code.ReadStringLiteral())
                        {
                            if (sb.Length > 0) sb.AppendLine();
                            sb.Append(CodeParser.StringLiteralToString(_code.Text));
                        }
                        devDesc = sb.ToString();
                        continue;
                    }

                    if (word == "prompt")
                    {
                        if (!_code.ReadStringLiteral()) return false;
                        continue;
                    }

                    if (word == "comment")
                    {
                        if (!_code.ReadStringLiteral()) return false;
                        continue;
                    }

                    if (word == "nomenu")
                    {
                        continue;
                    }

                    if (word == "accel")
                    {
                        while (!_code.EndOfFile)
                        {
                            var resetPos = _code.Position;
                            if (_code.ReadExact('+')) { }
                            else if (_code.ReadWord())
                            {
                                if (!_rxAccelWord.IsMatch(_code.Text))
                                {
                                    _code.Position = resetPos;
                                    return false;
                                }
                            }
                            else if (_code.ReadNumber())
                            {
                                if (_code.Text.Length != 1 || !char.IsDigit(_code.Text[0]))
                                {
                                    _code.Position = resetPos;
                                    return false;
                                }
                            }
                            else break;
                        }
                        continue;
                    }

                    if (word == "BEGINHLP" && funcName == "main")
                    {
                        var sb = new StringBuilder();
                        while (_code.ReadStringLiteral())
                        {
                            if (sb.Length > 0) sb.AppendLine();
                            sb.Append(CodeParser.StringLiteralToString(_code.Text));
                        }
                        devDesc = sb.ToString();

                        _code.ReadExactWholeWord("ENDHLP");
                        continue;
                    }

                    if (word == "tag")
                    {
                        var resetPos = _code.Position;

                        if (_code.ReadTagName())
                        {
                            if (_code.ReadStringLiteral())
                            {
                                continue;
                            }
                        }

                        _code.Position = resetPos;
                        return false;
                    }

                    return false;
                }

                return false;
            }
        }

        private bool TryReadFunctionArgument(CodeScope scope, bool createDefinitions, List<ArgumentDescriptor> args, List<PrepVariable> newDefList)
        {
            var dataType = DataType.TryParse(new DataType.ParseArgs(_code, _appSettings)
            {
                DataTypeCallback = GlobalDataTypeCallback,
                VariableCallback = GlobalVariableCallback
            });
            if (dataType == null) return false;

            PassByMethod passByMethod = PassByMethod.Value;
            if (_code.ReadExact("&"))   // Optional reference
            {
                if (_code.ReadExact("+"))   // Optional &+
                {
                    passByMethod = PassByMethod.ReferencePlus;
                }
                else
                {
                    passByMethod = PassByMethod.Reference;
                }
            }

            string name = null;
            if (_code.ReadWord())
            {
                name = _code.Text;
            }

            args.Add(new ArgumentDescriptor(name, dataType, passByMethod));

            if (name != null && createDefinitions)  // Optional var name
            {
                var arrayLength = TryReadArrayDecl();

                var localPos = _source.GetFilePosition(_code.TokenStartPostion);
                var def = new VariableDefinition(_code.Text, localPos, dataType, true, arrayLength, VariableType.Argument, passByMethod);
                scope.AddDefinition(def);
                if (!_visible || localPos.PrimaryFile)
                {
                    newDefList.Add(new PrepVariable(def, _code.Span));
                }
            }

            return true;
        }

        private bool TryReadVariableDeclaration(CodeScope scope, List<PrepVariable> newDefList)
        {
            var dataType = DataType.TryParse(new DataType.ParseArgs(_code, _appSettings)
            {
                DataTypeCallback = GlobalDataTypeCallback,
                VariableCallback = GlobalVariableCallback
            });
            if (dataType == null) return false;

            var gotVars = false;

            while (!_code.EndOfFile)
            {
                if (!_code.ReadWord()) break;

                var varName = _code.Text;
                var varNameSpan = _code.Span;
                var localPos = _source.GetFilePosition(_code.TokenStartPostion);

                var arrayLength = TryReadArrayDecl();

                var def = new VariableDefinition(varName, localPos, dataType, false, arrayLength, VariableType.Local, argPassByMethod: null);
                scope.AddDefinition(def);
                if (!_visible || localPos.PrimaryFile)
                {
                    newDefList.Add(new PrepVariable(def, varNameSpan));
                }
                gotVars = true;

                if (_code.ReadExact(',')) continue;
                if (_code.ReadExact(';')) break;
                break;
            }

            return gotVars;
        }

        private bool TryReadNestable()
        {
            if (_code.ReadExact('{'))
            {
                ReadBraceScope();
                return true;
            }

            if (_code.ReadExact('('))
            {
                ReadNestable(")");
                return true;
            }

            if (_code.ReadExact('['))
            {
                ReadNestable("]");
                return true;
            }

            return false;
        }

        private void ReadNestable(string endToken)
        {
            string str;
            while (_code.Read())
            {
                str = _code.Text;
                if (str == endToken) return;
                else if (str == "{") ReadBraceScope();
                else if (str == "(") ReadNestable(")");
                else if (str == "[") ReadNestable("]");
            }
        }

        public string FileName
        {
            get { return _fileName; }
        }

        public DefinitionProvider DefinitionProvider
        {
            get { return _defProv; }
        }

        private void AddGlobalDefinition(Definition def)
        {
            _defProv.AddGlobalFromFile(def);
            _globalDefs.Add(def);
        }

        public IEnumerable<Definition> GlobalDefinitions
        {
            get { return _globalDefs; }
        }

        public IEnumerable<LocalFunction> LocalFunctions
        {
            get { return _localFuncs; }
        }

        private void ReadExtract()
        {
            var permanent = _code.ReadExact("permanent");

            // Table name
            if (!_code.ReadWord()) return;
            var name = _code.Text;
            ExtractTableDefinition exDef = null;
            if (!_defProv.GetGlobalFromFile<ExtractTableDefinition>(name).Any())    // Don't add a definition if it's already there (extracts can be called multiple times)
            {
                var localPos = _source.GetFilePosition(_code.TokenStartPostion);
                exDef = new ExtractTableDefinition(name, localPos, permanent);
            }

            string lastToken = null;
            var lastTokenPos = 0;
            var fields = new List<string>();

            if (exDef != null)
            {
                var rownoDef = new ExtractFieldDefinition("rowno", exDef.FilePosition, exDef);
                rownoDef.SetDataType(DataType.Unsigned9);
                exDef.AddField(rownoDef);
            }

            var done = false;
            while (!_code.EndOfFile && !done)
            {
                if (!_code.PeekExact("==") && _code.ReadExact('=') && lastToken != null)
                {
                    if (exDef != null)
                    {
                        var localPos = _source.GetFilePosition(lastTokenPos);
                        var fieldDef = new ExtractFieldDefinition(lastToken, localPos, exDef);
                        exDef.AddField(fieldDef);
                    }
                    if (fields == null) fields = new List<string>();
                    fields.Add(lastToken);
                    lastToken = null;
                }
                else if (_code.ReadWord())
                {
                    lastToken = _code.Text;
                    lastTokenPos = _code.TokenStartPostion;
                }
                else
                {
                    _code.Read();
                    switch (_code.Text)
                    {
                        case ";":
                            done = true;
                            break;
                        case "{":
                        case "}":
                            done = true;
                            break;
                        case "(":
                            ReadNestable(")");
                            break;
                        case "[":
                            ReadNestable("]");
                            break;
                    }
                }
            }

            if (exDef != null)
            {
                _defProv.AddGlobalFromFile(exDef);
            }
        }

        private int[] TryReadArrayDecl()
        {
            var resetPos = _code.Position;
            List<int> arrayLengths = null;

            while (!_code.EndOfFile)
            {
                if (_code.ReadExact('[') &&
                    _code.ReadNumber() &&
                    int.TryParse(_code.Text, out int len) &&
                    _code.ReadExact(']'))
                {
                    if (arrayLengths == null) arrayLengths = new List<int>();
                    arrayLengths.Add(len);
                    resetPos = _code.Position;
                }
                else
                {
                    _code.Position = resetPos;
                    break;
                }
            }

            return arrayLengths?.ToArray();
        }

        public IEnumerable<IncludeDependency> IncludeDependencies
        {
            get { return _includeDependencies; }
        }

        internal Preprocessor Preprocessor
        {
            get { return _prep; }
            set { _prep = value; }
        }

        private class CodeScope
        {
            private readonly CodeScope _parent;
            private readonly int _startPos;
            private readonly DefinitionCollection _defs = new DefinitionCollection();

            public CodeScope(int scopeTokenStartPos)
            {
                _parent = null;
                _startPos = scopeTokenStartPos;
            }

            public CodeScope(CodeScope parent, int scopeTokenStartPos)
            {
                _parent = parent ?? throw new ArgumentNullException("parent");
                _startPos = scopeTokenStartPos;
            }

            public void AddDefinition(Definition def)
            {
                _defs.Add(def);
            }

            public Definition GetDefinition(string name)
            {
                var def = _defs[name].FirstOrDefault();
                if (def == null && _parent != null)
                {
                    return _parent.GetDefinition(name);
                }
                return null;
            }

            public int StartPos
            {
                get { return _startPos; }
            }
        }

        public class LocalFunction
        {
            private readonly FunctionDefinition _def;
            private readonly int _startPos;
            private readonly int _endPos;
            private readonly PrepVariable[] _args;
            private readonly PrepVariable[] _vars;
            private readonly CodeSpan _nameSpan;

            public LocalFunction(FunctionDefinition def, CodeSpan nameSpan, int startPos, int endPos, IEnumerable<PrepVariable> args, IEnumerable<PrepVariable> vars)
            {
                _def = def;
                _startPos = startPos;
                _endPos = endPos;
                _args = args.ToArray();
                _vars = vars.ToArray();
                _nameSpan = nameSpan;
            }

            public PrepVariable[] Arguments => _args;
            public PrepVariable[] Variables => _vars;

            public FunctionDefinition Definition
            {
                get { return _def; }
            }

            /// <summary>
            /// Starting position of the function body after all variable declarations, in preprocessor coordinates.
            /// </summary>
            public int StartPos
            {
                get { return _startPos; }
            }

            /// <summary>
            /// Ending position of the function body, in preprocessor coordinates.
            /// </summary>
            public int EndPos
            {
                get { return _endPos; }
            }

            /// <summary>
            /// Span of the function name, in preprocessor coordinates.
            /// </summary>
            public CodeSpan NameSpan
            {
                get { return _nameSpan; }
            }
        }

        public class PrepVariable
        {
            public PrepVariable(VariableDefinition definition, CodeSpan rawSpan)
            {
                Definition = definition ?? throw new ArgumentNullException(nameof(definition));
                RawSpan = rawSpan;
            }

            public VariableDefinition Definition { get; private set; }
            public CodeSpan RawSpan { get; private set; }

#if DEBUG
            public string Dump() => Definition.Dump();
#endif
        }

        public List<PrepError> Errors { get => _errors; set => _errors.AddRange(value); }

        public void ReportError(CodeSpan span, CAError errorCode, params object[] args)
        {
            _errors.Add(new PrepError(span, errorCode, args));
        }

        public void ReportError_Local(string localFileName, CodeSpan localSpan, CAError errorCode, params object[] args)
        {
            _errors.Add(new PrepError(localFileName, localSpan, errorCode, args));
        }
    }

    public struct PrepError
    {
        public string LocalFileName { get;  private set; }
        public CodeSpan Span { get; private set; }
        public CAError ErrorCode { get; private set; }
        public object[] Args { get; private set; }

        public PrepError(CodeSpan absoluteSpan, CAError errorCode, params object[] args)
        {
            LocalFileName = null;
            Span = absoluteSpan;
            ErrorCode = errorCode;
            Args = args;
        }

        public PrepError(string localFileName, CodeSpan localSpan, CAError errorCode, params object[] args)
        {
            LocalFileName = localFileName;
            Span = localSpan;
            ErrorCode = errorCode;
            Args = args;
        }
    }
}

﻿using DK.AppEnvironment;
using DK.Code;
using DK.Definitions;
using DK.Modeling;
using DK.Preprocessing.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace DK.Preprocessing
{
    internal class Preprocessor
    {
        private DkAppSettings _appSettings;
        private FileStore _store;
        private Dictionary<string, PreprocessorDefine> _defines;
        private List<IncludeDependency> _includeDependencies = new List<IncludeDependency>();
        private List<Reference> _refs = new List<Reference>();
        private WarningSuppressionTracker _warningSuppressions = new WarningSuppressionTracker();

        public Preprocessor(DkAppSettings appSettings, FileStore store)
        {
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public IEnumerable<Reference> References => _refs;
        public WarningSuppressionTracker WarningSuppressions => _warningSuppressions;

        /// <summary>
        /// Preprocesses a document.
        /// </summary>
        /// <param name="reader">The document to be read.</param>
        /// <param name="writer">The output stream for the preprocessed document.</param>
        /// <param name="fileName">The name of the file being preprocessed.</param>
        /// <param name="parentFiles">A list of files which are to be considered parent files (to avoid cyclical #includes)</param>
        /// <param name="fileContext">The type of file.</param>
        /// <returns>True if the preprocessor changed a part of the document and the document should be re-run through this function again.
        /// False if the document has finished preprocessing.</returns>
        public PreprocessorResult Preprocess(
            IPreprocessorReader reader,
            IPreprocessorWriter writer,
            string fileName,
            IEnumerable<string> parentFiles,
            FileContext fileContext,
            CancellationToken cancel,
            string stopAtIncludeFile = null,
            IEnumerable<PreprocessorDefine> stdlibDefines = null)
        {
            if (reader == null) throw new ArgumentNullException("reader");

            return Preprocess(new PreprocessorParams(reader, writer, fileName, parentFiles, fileContext,
                ContentType.File, stopAtIncludeFile, cancel)
            {
                isMainSource = true,
                stdlibDefines = stdlibDefines
            });
        }

        private PreprocessorResult Preprocess(PreprocessorParams p)
        {
            // This function assumes the source has already been merged.

            if (_defines == null)
            {
                _defines = new Dictionary<string, PreprocessorDefine>();
                _defines["_WINDOWS"] = new PreprocessorDefine("_WINDOWS", string.Empty, null, FilePosition.Empty, _appSettings, ServerContext.Neutral);
            }

            if (p.stdlibDefines != null)
            {
                foreach (var def in p.stdlibDefines) _defines[def.Name] = def;
            }

            string str;
            var sb = new StringBuilder();
            var rdr = p.reader;
            p.reader.SetWriter(p.writer);

            while (!rdr.EOF && !p.result.IncludeFileReached)
            {
                p.cancel.ThrowIfCancellationRequested();

                str = rdr.PeekToken(false);
                if (string.IsNullOrEmpty(str)) continue;

                if (str[0] == '#')
                {
                    ProcessDirective(p, str);
                    continue;
                }

                if (p.suppress)
                {
                    rdr.Ignore(str.Length);
                    continue;
                }

                if (str[0].IsWordChar(true))
                {
                    if (p.args != null && p.args.Any(x => x.Name == str))
                    {
                        ProcessDefineUse(p, str);
                    }
                    else if (_defines.ContainsKey(str))
                    {
                        ProcessDefineUse(p, str);
                    }
                    else if (str == "STRINGIZE")
                    {
                        rdr.Ignore(str.Length);
                        p.result.DocumentAltered = true;
                        ProcessStringizeKeyword(p);
                    }
                    else if (str == "defined" && p.contentType == ContentType.Condition)
                    {
                        rdr.Ignore(str.Length);
                        p.result.DocumentAltered = true;
                        ProcessDefinedKeyword(p);
                    }
                    else
                    {
                        rdr.Use(str.Length);
                    }
                    continue;
                }

                if (str == "@")
                {
                    // This is the start of the name portion of a data type definition.
                    rdr.Use(str.Length);
                    var name = rdr.PeekToken(true);
                    if (name.IsWord()) rdr.Use(name.Length);
                    continue;
                }

                rdr.Use(str.Length);
            }

            p.writer.Flush();

            return p.result;
        }

        private void ProcessDirective(PreprocessorParams p, string directiveName)
        {
            // This function is called after the '#' has been read from the file.

            p.result.DocumentAltered = true;

            switch (directiveName)
            {
                case "#define":
                    p.reader.Ignore(directiveName.Length);
                    ProcessDefine(p);
                    break;
                case "#undef":
                    p.reader.Ignore(directiveName.Length);
                    ProcessUndef(p);
                    break;
                case "#include":
                    p.reader.Ignore(directiveName.Length);
                    if (!p.resolvingMacros) ProcessInclude(p);
                    break;
                case "#if":
                    ProcessIf(p, directiveName, false);
                    break;
                case "#elif":
                    ProcessIf(p, directiveName, true);
                    break;
                case "#ifdef":
                    p.reader.Ignore(directiveName.Length);
                    ProcessIfDef(p, true);
                    break;
                case "#ifndef":
                    p.reader.Ignore(directiveName.Length);
                    ProcessIfDef(p, false);
                    break;
                case "#else":
                    ProcessElse(p, directiveName);
                    break;
                case "#endif":
                    ProcessEndIf(p, directiveName);
                    break;
                case "#warndel":
                    p.reader.Ignore(directiveName.Length);
                    ProcessWarnAddDel(p, false);
                    break;
                case "#warnadd":
                    p.reader.Ignore(directiveName.Length);
                    ProcessWarnAddDel(p, true);
                    break;
                case "#replace":
                    ProcessReplace(p, directiveName);
                    break;
                case "#with":
                    ProcessWith(p, directiveName);
                    break;
                case "#endreplace":
                    ProcessEndReplace(p, directiveName);
                    break;
                case "#label":
                    ProcessLabel(p, directiveName);
                    break;
                default:
                    p.reader.Ignore(directiveName.Length);
                    break;
            }
        }

        private void ProcessDefine(PreprocessorParams p)
        {
            var rdr = p.reader;
            char ch;
            string str;

            // Get the define name
            rdr.IgnoreWhiteSpaceAndComments(true);
            var linkFilePos = rdr.FilePosition;
            var linkRawPos = rdr.Position;
            var name = rdr.PeekIdentifier();
            if (string.IsNullOrEmpty(name)) return;
            var nameFilePos = rdr.FilePosition;
            rdr.Ignore(name.Length);

            // Check if this is parameterized
            List<string> paramNames = null;
            if (rdr.Peek() == '(')
            {
                rdr.Ignore(1);

                while (!rdr.EOF)
                {
                    rdr.IgnoreWhiteSpaceAndComments(true);

                    str = rdr.PeekToken(false);
                    if (string.IsNullOrEmpty(str)) return;
                    if (str == ",")
                    {
                        rdr.Ignore(str.Length);
                    }
                    else if (str[0].IsWordChar(true))
                    {
                        rdr.Ignore(str.Length);
                        if (!p.suppress)
                        {
                            if (paramNames == null) paramNames = new List<string>();
                            paramNames.Add(str);
                        }
                    }
                    else if (str == ")")
                    {
                        rdr.Ignore(str.Length);
                        break;
                    }
                    else return;
                }
            }

            // Read the define value
            rdr.IgnoreWhiteSpaceAndComments(true);
            var insideBlock = false;
            var braceLevel = 0;
            ch = rdr.Peek();
            if (ch == '{')
            {
                insideBlock = true;
                braceLevel = 1;
                rdr.Ignore(1);
            }

            var sb = new StringBuilder();

            while (!rdr.EOF)
            {
                if (rdr.IgnoreComments()) continue;

                str = rdr.PeekToken(true);
                if (str == null)
                {
                    // End of line found

                    char endCh;
                    int index;
                    if (sb.GetLastNonWhiteChar(out endCh, out index))
                    {
                        if (endCh == '\\')
                        {
                            // define continues down to the next line, but don't include the slash in the resulting text.
                            sb.Remove(index, 1);
                            if (insideBlock) sb.Append("\r\n");
                            //rdr.IgnoreUntil(c => c == '\r' || c == '\n');
                            rdr.IgnoreWhile(PreprocessorReaderExtensions.LineEndChars);
                            continue;
                        }
                        else if (insideBlock)
                        {
                            //rdr.IgnoreUntil(c => c == '\r' || c == '\n');
                            rdr.IgnoreWhile(PreprocessorReaderExtensions.LineEndChars);
                            sb.Append("\r\n");
                            continue;
                        }
                        else break;
                    }
                    else
                    {
                        if (insideBlock)
                        {
                            //rdr.IgnoreUntil(c => c == '\r' || c == '\n');
                            rdr.IgnoreWhile(PreprocessorReaderExtensions.LineEndChars);
                            sb.Append("\r\n");
                            continue;
                        }
                        else break;
                    }
                }

                if (str == "{" && insideBlock)
                {
                    braceLevel++;
                    rdr.Ignore(str.Length);
                    sb.Append('{');
                    continue;
                }

                if (str == "}" && insideBlock)
                {
                    rdr.Ignore(str.Length);
                    if (--braceLevel <= 0)
                    {
                        break;
                    }
                    else
                    {
                        sb.Append('}');
                        continue;
                    }
                }

                rdr.Ignore(str.Length);
                sb.Append(str);
            }

            if (!p.suppress)
            {
                var define = new PreprocessorDefine(name, sb.ToString().Trim(), paramNames, linkFilePos, _appSettings, p.fileContext.ToServerContext());
                _defines[name] = define;
                if (nameFilePos.IsInFile) _refs.Add(new Reference(define.Definition, nameFilePos, rawPosition: linkRawPos));
            }
        }

        private void ProcessUndef(PreprocessorParams p)
        {
            p.reader.IgnoreWhiteSpaceAndComments(true);
            var name = p.reader.PeekIdentifier();
            if (string.IsNullOrEmpty(name)) return;
            var nameFilePos = p.reader.FilePosition;
            var nameRawPos = p.reader.Position;
            p.reader.Ignore(name.Length);

            if (!p.suppress)
            {
                PreprocessorDefine define;
                if (_defines.TryGetValue(name, out define))
                {
                    define.Disabled = true;
                    if (nameFilePos.IsInFile) _refs.Add(new Reference(define.Definition, nameFilePos, rawPosition: nameRawPos));
                }
            }
        }

        private void ProcessDefineUse(PreprocessorParams p, string name)
        {
            var rdr = p.reader;

            if (p.suppress)
            {
                rdr.Use(name.Length);
                return;
            }
            if (p.restrictedDefines != null && p.restrictedDefines.Contains(name))
            {
                rdr.Use(name.Length);
                return;
            }

            PreprocessorDefine define = null;
            if (p.args != null)
            {
                foreach (var arg in p.args)
                {
                    if (arg.Name == name)
                    {
                        define = arg;
                        break;
                    }
                }
            }
            if (define == null) _defines.TryGetValue(name, out define);
            if (define == null || define.Disabled)
            {
                rdr.Use(name.Length);
                return;
            }

            var nameFilePos = rdr.FilePosition;
            if (nameFilePos.IsInFile) _refs.Add(new Reference(define.Definition, nameFilePos, rawPosition: rdr.Position));

            if (define.DataType != null && name == define.DataType.Name)
            {
                // Insert the data type name before the data type, so that it's available in the quick info and database.
                rdr.Insert(string.Format("@{0} ", DataType.NormalizeEnumOption(name)));
            }
            rdr.Ignore(name.Length);

            p.result.DocumentAltered = true;

            List<string> paramList = null;
            if (define.ParamNames != null)
            {
                // This is a parameterized macro
                rdr.IgnoreWhiteSpaceAndComments(false);
                if (rdr.Peek() != '(') return;
                rdr.Ignore(1);

                char ch;
                var sb = new StringBuilder();
                paramList = new List<string>();

                rdr.IgnoreWhiteSpaceAndComments(false);
                while (!rdr.EOF)
                {
                    if (rdr.IgnoreComments()) continue;

                    ch = rdr.Peek();
                    if (ch == ',')
                    {
                        rdr.Ignore(1);

                        var argText = ApplySubstitutions(sb.ToString(), p.args);
                        var resolvedParamText = ResolveMacros(argText, p.restrictedDefines, null, p.fileContext, p.contentType, p.cancel);
                        paramList.Add(resolvedParamText.Trim());

                        sb.Clear();
                    }
                    else if (ch == ')')
                    {
                        rdr.Ignore(1);
                        break;
                    }
                    else if (ch == '(')
                    {
                        rdr.Ignore(1);
                        sb.Append('(');
                        sb.Append(rdr.ReadAndIgnoreNestableContent(")"));
                        sb.Append(')');
                    }
                    else if (ch == '{')
                    {
                        rdr.Ignore(1);
                        sb.Append('{');
                        sb.Append(rdr.ReadAndIgnoreNestableContent("}"));
                        sb.Append('}');
                    }
                    else if (ch == '[')
                    {
                        rdr.Ignore(1);
                        sb.Append('[');
                        sb.Append(rdr.ReadAndIgnoreNestableContent("]"));
                        sb.Append(']');
                    }
                    else if (ch == '\'' || ch == '\"')
                    {
                        sb.Append(rdr.ReadAndIgnoreStringLiteral());
                    }
                    else
                    {
                        rdr.Ignore(1);
                        sb.Append(ch);
                    }
                }

                if (sb.Length > 0)
                {
                    var argText = ApplySubstitutions(sb.ToString(), p.args);
                    var resolvedParamText = ResolveMacros(argText, p.restrictedDefines, null, p.fileContext, p.contentType, p.cancel);
                    paramList.Add(resolvedParamText.Trim());
                }

                if (define.ParamNames.Count != paramList.Count) return;
            }

            var oldArgs = p.args;
            
            List<PreprocessorDefine> args = null;
            if (paramList != null)
            {
                if (define.ParamNames == null || define.ParamNames.Count != paramList.Count) return;
                if (args == null) args = new List<PreprocessorDefine>();
                for (int i = 0, ii = paramList.Count; i < ii; i++)
                {
                    args.Add(new PreprocessorDefine(define.ParamNames[i], paramList[i], null, FilePosition.Empty, _appSettings, p.fileContext.ToServerContext()));
                }
            }
            if (p.args != null && p.args.Any())
            {
                if (args == null) args = new List<PreprocessorDefine>();
                args.AddRange(p.args);
            }

            string[] restrictedDefines = null;
            if (p.restrictedDefines != null) restrictedDefines = p.restrictedDefines.Concat(new string[] { name }).ToArray();
            else restrictedDefines = new string[] { name };

            var textToAdd = ApplySubstitutions(define.Content, args);
            textToAdd = ResolveMacros(textToAdd, restrictedDefines, null, p.fileContext, p.contentType, p.cancel);
            rdr.Insert(textToAdd);

            p.args = oldArgs;
        }

        private void ProcessStringizeKeyword(PreprocessorParams p)
        {
            var rdr = p.reader;
            rdr.IgnoreWhiteSpaceAndComments(false);
            if (rdr.EOF) return;

            if (rdr.Peek() != '(') return;
            rdr.Ignore(1);
            rdr.IgnoreWhiteSpaceAndComments(true);

            var content = rdr.ReadAndIgnoreNestableContent(")");
            content = ApplySubstitutions(content, p.args);
            content = ResolveMacros(content, p.restrictedDefines, null, p.fileContext, p.contentType, p.cancel);

            p.reader.Insert(EscapeString(content));
        }

        private void ProcessDefinedKeyword(PreprocessorParams p)
        {
            var rdr = p.reader;
            rdr.IgnoreWhiteSpaceAndComments(true);
            if (rdr.EOF) return;

            if (rdr.Peek() != '(') return;
            rdr.Ignore(1);
            rdr.IgnoreWhiteSpaceAndComments(true);

            var ident = rdr.PeekIdentifier();
            if (string.IsNullOrEmpty(ident)) return;
            rdr.Ignore(ident.Length);

            p.writer.Append(IsDefined(p, ident) ? "1" : "0", CodeAttributes.Empty);

            rdr.IgnoreWhiteSpaceAndComments(true);
            if (rdr.Peek() == ')') rdr.Ignore(1);
        }

        private string ResolveMacros(string source, IEnumerable<string> restrictedDefines, IEnumerable<PreprocessorDefine> args,
            FileContext serverContext, ContentType contentType, CancellationToken cancel)
        {
            var reader = new StringPreprocessorReader(source);
            var writer = new StringPreprocessorWriter();

            var parms = new PreprocessorParams(reader, writer, string.Empty, parentFiles: null, serverContext, contentType, stopAtIncludeFile: null, cancel);
            parms.restrictedDefines = restrictedDefines;
            parms.args = args;
            parms.resolvingMacros = true;

            var lastText = source;
            var counter = 32;	// To prevent infinite loop with recursive define

            while (Preprocess(parms).DocumentAltered && counter <= 32)
            {
                cancel.ThrowIfCancellationRequested();

                var newText = writer.Text;
                if (newText == lastText) break;
                lastText = newText;

                parms.result.DocumentAltered = false;
                parms.args = null;	// Only apply the arguments to the first round
                parms.reader = new StringPreprocessorReader(newText);
                parms.writer = writer = new StringPreprocessorWriter();

                counter++;
            }
            return writer.Text;
        }

        private string ApplySubstitutions(string source, IEnumerable<PreprocessorDefine> args)
        {
            if (args == null) return source;

            var code = new CodeParser(source);
            code.ReturnWhiteSpace = true;

            var sb = new StringBuilder();

            while (code.Read())
            {
                if (code.Type == CodeType.Word)
                {
                    var word = code.Text;
                    var found = false;
                    foreach (var arg in args)
                    {
                        if (arg.Name == word)
                        {
                            sb.Append(arg.Content);
                            found = true;
                            break;
                        }
                    }

                    if (!found) sb.Append(word);
                }
                else
                {
                    sb.Append(code.Text);
                }
            }

            return sb.ToString();
        }

        private void ProcessInclude(PreprocessorParams p)
        {
            string includeName = null;
            var searchSameDir = false;

            var rdr = p.reader;

            rdr.IgnoreWhiteSpaceAndComments(true);
            var ch = rdr.Peek();
            if (ch == '\"')
            {
                rdr.Ignore(1);
                includeName = rdr.PeekUntil(c => c != '\"' && c != '\r' && c != '\n');
                rdr.Ignore(includeName.Length);
                if (rdr.Peek() == '\"') rdr.Ignore(1);
                searchSameDir = true;
            }
            else if (ch == '<')
            {
                rdr.Ignore(1);
                includeName = rdr.PeekUntil(c => c != '>' && c != '\r' && c != '\n');
                rdr.Ignore(includeName.Length);
                if (rdr.Peek() == '>') rdr.Ignore(1);
                searchSameDir = false;
            }
            else return;
            if (string.IsNullOrEmpty(includeName)) return;

            if (!p.suppress) AppendIncludeFile(p, includeName, searchSameDir);
        }

        private void AppendIncludeFile(PreprocessorParams p, string fileName, bool searchSameDir)
        {
            // Load the include file
            string[] parentFiles;
            if (string.IsNullOrEmpty(p.fileName))
            {
                parentFiles = p.parentFiles;
                if (parentFiles == null) parentFiles = new string[0];
            }
            else
            {
                if (p.parentFiles != null) parentFiles = p.parentFiles.Concat(new string[] { p.fileName }).ToArray();
                else parentFiles = new string[0];
            }

            var includeNode = _store.GetIncludeFile(_appSettings, p.fileName, fileName, searchSameDir, parentFiles);
            if (includeNode == null) return;

            if (p.stopAtIncludeFile != null && includeNode.FullPathName.Equals(p.stopAtIncludeFile, StringComparison.OrdinalIgnoreCase))
            {
                p.result.IncludeFileReached = true;
                return;
            }

            var rawSource = includeNode.GetSource(_appSettings);
            if (rawSource == null) return;
            var reader = new CodeSource.CodeSourcePreprocessorReader(rawSource);

            // Run the preprocessor on the include file.
            var includeSource = new CodeSource();
            var parms = new PreprocessorParams(reader, includeSource, includeNode.FullPathName, parentFiles, p.fileContext, p.contentType, p.stopAtIncludeFile, p.cancel);
            p.result.Merge(Preprocess(parms));

            p.writer.Append(includeSource);
        }

        public void AddIncludeDependency(string fullPathName, bool include, bool localizedFile, string content)
        {
            _includeDependencies.Add(new IncludeDependency(fullPathName, include, localizedFile, content));
        }

        public void AddIncludeDependencies(IEnumerable<IncludeDependency> includeDependencies)
        {
            _includeDependencies.AddRange(includeDependencies);
        }

        public IEnumerable<IncludeDependency> IncludeDependencies
        {
            get { return _includeDependencies; }
        }

        private void ProcessIfDef(PreprocessorParams p, bool activeIfDefined)
        {
            var rdr = p.reader;
            rdr.IgnoreWhiteSpaceAndComments(true);

            var name = rdr.PeekIdentifier();
            if (string.IsNullOrEmpty(name)) return;

            var nameFilePos = rdr.FilePosition;
            if (nameFilePos.IsInFile)
            {
                PreprocessorDefine define;
                if (_defines.TryGetValue(name, out define))
                {
                    _refs.Add(new Reference(define.Definition, nameFilePos, rawPosition: rdr.Position));
                }
            }

            rdr.Ignore(name.Length);

            if (p.fileContext == FileContext.Include)
            {
                p.ifStack.Push(new ConditionScope(ConditionResult.Indeterminate, ConditionResult.Indeterminate, p.suppress));
            }
            else if (p.suppress)
            {
                p.ifStack.Push(new ConditionScope(ConditionResult.Negative, ConditionResult.Positive, true));
            }
            else
            {
                bool defined = IsDefined(p, name);
                if (!activeIfDefined) defined = !defined;
                var result = defined ? ConditionResult.Positive : ConditionResult.Negative;
                p.ifStack.Push(new ConditionScope(result, result, false));
                UpdateSuppress(p);
            }
            rdr.IgnoreWhiteSpaceAndComments(true);
        }

        private void ProcessEndIf(PreprocessorParams p, string directiveName)
        {
            if (p.ifStack.Count > 0) p.ifStack.Pop();
            UpdateSuppress(p);

            p.reader.Ignore(directiveName.Length);
            p.reader.IgnoreWhiteSpaceAndComments(true);
        }

        private void ProcessElse(PreprocessorParams p, string directiveName)
        {
            if (p.ifStack.Count == 0)
            {
                p.reader.Ignore(directiveName.Length);
                return;
            }

            var scope = p.ifStack.Peek();

            switch (scope.prevResult)
            {
                case ConditionResult.Positive:
                    scope.result = ConditionResult.Negative;
                    break;
                case ConditionResult.Negative:
                    scope.result = ConditionResult.Positive;
                    break;
                case ConditionResult.Indeterminate:
                    scope.result = ConditionResult.Indeterminate;
                    break;
            }

            if (p.suppress)
            {
                UpdateSuppress(p);
                p.reader.Ignore(directiveName.Length);
                p.reader.IgnoreWhiteSpaceAndComments(true);
            }
            else
            {
                p.reader.Ignore(directiveName.Length);
                p.reader.IgnoreWhiteSpaceAndComments(true);
                UpdateSuppress(p);
            }
        }

        private void ProcessIf(PreprocessorParams p, string directiveName, bool elif)
        {
            var rdr = p.reader;

            var conditionStr = rdr.PeekUntil(c => c != '\r' && c != '\n');
            conditionStr = conditionStr.Substring(directiveName.Length);

            // Ignore up to the last comment on the line (just in case it's a multi-line comment)
            var conditionFileName = rdr.FileName;
            var conditionPosition = rdr.Position;
            var parser = new CodeParser(conditionStr);
            parser.ReturnComments = true;

            var lastStartPos = -1;
            var lastType = CodeType.Unknown;
            while (parser.Read())
            {
                lastStartPos = parser.TokenStartPostion;
                lastType = parser.Type;
            }
            if (lastStartPos != -1 && lastType == CodeType.Comment)
            {
                conditionStr = conditionStr.Substring(0, lastStartPos);
            }

            if (elif)
            {
                if (p.ifStack.Count > 0)
                {
                    var ifLevel = p.ifStack.Peek();
                    if (ifLevel.outerSuppressed)
                    {
                        ifLevel.result = ConditionResult.Negative;
                    }
                    else
                    {
                        switch (ifLevel.prevResult)
                        {
                            case ConditionResult.Positive:
                                // A previous #if evaluated to true, so this will never be positive.
                                ifLevel.result = ConditionResult.Negative;
                                break;

                            case ConditionResult.Negative:
                                // No previous #if was true, so this could be the one...
                                ifLevel.result = EvaluateCondition(p, conditionStr, conditionFileName, conditionPosition, p.cancel);
                                if (ifLevel.result == ConditionResult.Positive) ifLevel.prevResult = ConditionResult.Positive;
                                break;

                            case ConditionResult.Indeterminate:
                                // An error on a previous #if
                                ifLevel.result = EvaluateCondition(p, conditionStr, conditionFileName, conditionPosition, p.cancel);
                                ifLevel.prevResult = ifLevel.result;
                                break;
                        }
                    }
                }
                else
                {
                    var result = EvaluateCondition(p, conditionStr, conditionFileName, conditionPosition, p.cancel);
                    p.ifStack.Push(new ConditionScope(result, result, p.suppress));
                }
            }
            else
            {
                if (p.suppress)
                {
                    p.ifStack.Push(new ConditionScope(ConditionResult.Negative, ConditionResult.Positive, p.suppress));
                }
                else if (p.fileContext == FileContext.Include)
                {
                    p.ifStack.Push(new ConditionScope(ConditionResult.Indeterminate, ConditionResult.Indeterminate, p.suppress));
                }
                else
                {
                    var result = EvaluateCondition(p, conditionStr, conditionFileName, conditionPosition, p.cancel);
                    p.ifStack.Push(new ConditionScope(result, result, p.suppress));
                }
            }

            if (!elif)
            {
                rdr.Ignore(directiveName.Length + conditionStr.Length);
            }

            UpdateSuppress(p);

            if (elif)
            {
                rdr.Ignore(directiveName.Length + conditionStr.Length);
            }
        }

        private void UpdateSuppress(PreprocessorParams p)
        {
            p.reader.Suppress = p.suppress = PeekSuppress(p);
        }

        private bool PeekSuppress(PreprocessorParams p)
        {
            if (p.replaceInEffect) return true;
            foreach (var scope in p.ifStack)
            {
                if (scope.result == ConditionResult.Negative) return true;
            }
            return false;
        }

        private string EscapeString(string str)
        {
            var sb = new StringBuilder();
            sb.Append("\"");

            foreach (var ch in str)
            {
                switch (ch)
                {
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    case '"':
                        sb.Append("\\\"");
                        break;
                    default:
                        if (ch < 0x20 || ch > 0x7f) sb.AppendFormat("\\x{0:X4}", (int)ch);
                        else sb.Append(ch);
                        break;
                }
            }

            sb.Append("\"");
            return sb.ToString();
        }

        private void ProcessWarnAddDel(PreprocessorParams p, bool add)
        {
            var rdr = p.reader;
            rdr.IgnoreWhiteSpaceAndComments(true);

            var warnStr = rdr.PeekUntil(c => c != '\r' && c != '\n');
            rdr.Ignore(warnStr.Length);

            if (!string.IsNullOrWhiteSpace(warnStr))
            {
                var numberString = ResolveMacros(warnStr, p.restrictedDefines, args: null, p.fileContext, p.contentType, p.cancel);
                if (int.TryParse(numberString, out int number))
                {
                    if (add) _warningSuppressions.OnWarnAdd(number, p.writer.Position);
                    else _warningSuppressions.OnWarnDel(number, p.writer.Position);
                }
            }
        }

        private bool IsDefined(PreprocessorParams p, string name)
        {
            if (p.args != null && p.args.Any(x => x.Name == name)) return true;

            PreprocessorDefine define;
            if (_defines.TryGetValue(name, out define) && !define.Disabled) return true;

            return false;
        }

        public void AddDefinitionsToProvider(DefinitionProvider defProv)
        {
            foreach (var define in _defines.Values)
            {
                defProv.AddGlobalFromFile(define.Definition);
            }
        }

        public IEnumerable<Definition> ActiveDefineDefinitions
        {
            get
            {
                foreach (var def in _defines.Values)
                {
                    if (!def.Disabled) yield return def.Definition;
                }
            }
        }

        public IEnumerable<PreprocessorDefine> Defines
        {
            get { return _defines.Values; }
        }

        private void ProcessReplace(PreprocessorParams p, string directiveName)
        {
            p.reader.Ignore(directiveName.Length);

            p.replaceInEffect = true;
            UpdateSuppress(p);
        }

        private void ProcessWith(PreprocessorParams p, string directiveName)
        {
            p.replaceInEffect = false;
            UpdateSuppress(p);

            p.reader.Ignore(directiveName.Length);
        }

        private void ProcessEndReplace(PreprocessorParams p, string directiveName)
        {
            p.reader.Ignore(directiveName.Length);
        }

        private void ProcessLabel(PreprocessorParams p, string directiveName)
        {
            p.reader.Ignore(directiveName.Length);
            p.reader.IgnoreWhiteSpaceAndComments(true);
            var name = p.reader.PeekToken(true);
            if (name.IsWord()) p.reader.Ignore(name.Length);
        }

        public enum ConditionResult
        {
            Negative,
            Positive,
            Indeterminate
        }

        private class ConditionScope
        {
            public ConditionResult result;
            public ConditionResult prevResult;
            public bool outerSuppressed;

            private ConditionScope()
            { }

            public ConditionScope(ConditionResult result, ConditionResult prevResult, bool outerSuppressed)
            {
                this.result = result;
                this.prevResult = prevResult;
                this.outerSuppressed = outerSuppressed;
            }
        }

        private enum ContentType
        {
            File,
            Condition
        }

        private class PreprocessorParams
        {
            public IPreprocessorReader reader;
            public IPreprocessorWriter writer;
            public string fileName;
            public string[] parentFiles;
            public bool allowDirectives = true;
            public Stack<ConditionScope> ifStack = new Stack<ConditionScope>();
            public bool suppress;
            public IEnumerable<PreprocessorDefine> args;
            public IEnumerable<string> restrictedDefines;
            public ContentType contentType;
            public bool replaceInEffect;
            public bool resolvingMacros;
            public FileContext fileContext;
            public bool isMainSource;
            public string stopAtIncludeFile;
            public PreprocessorResult result = new PreprocessorResult();
            public IEnumerable<PreprocessorDefine> stdlibDefines;
            public CancellationToken cancel;

            public PreprocessorParams(IPreprocessorReader reader, IPreprocessorWriter writer, string fileName,
                IEnumerable<string> parentFiles, FileContext serverContext, ContentType contentType, string stopAtIncludeFile,
                CancellationToken cancel)
            {
                this.reader = reader;
                this.writer = writer;
                this.fileName = fileName;
                if (parentFiles != null) this.parentFiles = parentFiles.ToArray();
                this.fileContext = serverContext;
                this.contentType = contentType;
                this.stopAtIncludeFile = stopAtIncludeFile;
                this.cancel = cancel;
            }
        }

        public class PreprocessorResult
        {
            public bool DocumentAltered { get; set; }
            public bool IncludeFileReached { get; set; }

            public void Merge(PreprocessorResult result)
            {
                if (result.DocumentAltered) DocumentAltered = true;
                if (result.IncludeFileReached) IncludeFileReached = true;
            }
        }

        private ConditionResult EvaluateCondition(PreprocessorParams p, string conditionStr, string fileName, int pos, CancellationToken cancel)
        {
            // Run preprocessor on the condition string
            var reader = new StringPreprocessorReader(conditionStr);
            var writer = new StringPreprocessorWriter();
            var parms = new PreprocessorParams(reader, writer, string.Empty, null, p.fileContext, ContentType.Condition, stopAtIncludeFile: null, cancel);
            parms.allowDirectives = false;
            parms.args = p.args;
            Preprocess(parms);

            // Evaluate the condition string
            var parser = new CodeParser(writer.Text);
            var tokenGroup = GroupToken.Parse(null, parser, null);
            var finalValue = tokenGroup.Value;

            ConditionResult ret;
            if (finalValue.HasValue)
            {
                if (finalValue.Value != 0) ret = ConditionResult.Positive;
                else ret = ConditionResult.Negative;
            }
            else
            {
                ret = ConditionResult.Indeterminate;
            }
            return ret;
        }

        public struct Reference
        {
            private Definition _def;
            private FilePosition _filePos;
            private int _rawPos;

            public Reference(Definition def, FilePosition filePos, int rawPosition)
            {
                _def = def;
                _filePos = filePos;
                _rawPos = rawPosition;
            }

            public Definition Definition => _def;
            public FilePosition FilePosition => _filePos;
            public int RawPosition => _rawPos;
        }
    }
}

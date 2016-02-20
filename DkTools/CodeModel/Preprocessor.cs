using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel.Definitions;
using DkTools.CodeModel.Tokens;

namespace DkTools.CodeModel
{
	internal class Preprocessor
	{
		private FileStore _store;
		private Dictionary<string, Define> _defines;
		private List<IncludeDependency> _includeDependencies = new List<IncludeDependency>();
		private List<Reference> _refs = new List<Reference>();

		public Preprocessor(FileStore store)
		{
			if (store == null) throw new ArgumentNullException("store");
			_store = store;
		}

		public void Preprocess(IPreprocessorReader reader, IPreprocessorWriter writer, string fileName, IEnumerable<string> parentFiles, FileContext fileContext)
		{
			if (reader == null) throw new ArgumentNullException("reader");

			Preprocess(new PreprocessorParams(reader, writer, fileName, parentFiles, fileContext, ContentType.File)
			{
				isMainSource = true
			});
		}

		private void Preprocess(PreprocessorParams p)
		{
			// This function assumes the source has already been merged.

			if (_defines == null)
			{
				_defines = new Dictionary<string, Define>();
				_defines["_WINDOWS"] = new Define("_WINDOWS", string.Empty, null, null, 0);
			}

			string str;
			var sb = new StringBuilder();
			var rdr = p.reader;
			p.reader.SetWriter(p.writer);

			while (!rdr.EOF)
			{
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
						ProcessStringizeKeyword(p);
					}
					else if (str == "defined" && p.contentType == ContentType.Condition)
					{
						rdr.Ignore(str.Length);
						ProcessDefinedKeyword(p);
					}
					else
					{
						rdr.Use(str.Length);
					}
					continue;
				}

				rdr.Use(str.Length);
			}

			p.writer.Flush();
		}

		private void ProcessDirective(PreprocessorParams p, string directiveName)
		{
			// This function is called after the '#' has been read from the file.

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
				case "#warnadd":
					p.reader.Ignore(directiveName.Length);
					ProcessWarnAddDel(p);
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
			var linkFileName = rdr.FileName;
			var linkPos = rdr.Position;
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
				var define = new Define(name, sb.ToString(), paramNames, linkFileName, linkPos);
				_defines[name] = define;
				if (nameFilePos.IsInFile) _refs.Add(new Reference(define.Definition, nameFilePos));
			}
		}

		private void ProcessUndef(PreprocessorParams p)
		{
			p.reader.IgnoreWhiteSpaceAndComments(true);
			var name = p.reader.PeekIdentifier();
			if (string.IsNullOrEmpty(name)) return;
			var nameFilePos = p.reader.FilePosition;
			p.reader.Ignore(name.Length);

			Define define;
			if (_defines.TryGetValue(name, out define))
			{
				define.Disabled = true;
				if (nameFilePos.IsInFile) _refs.Add(new Reference(define.Definition, nameFilePos));
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

			Define define = null;
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
			if (define == null)
			{
				rdr.Use(name.Length);
				return;
			}

			var nameFilePos = rdr.FilePosition;
			if (nameFilePos.IsInFile) _refs.Add(new Reference(define.Definition, nameFilePos));

			if (define.IsDataType)
			{
				// Insert the data type name before the data type, so that it's available in the quick info and database.
				rdr.Insert(string.Format("@{0} ", DataType.DecorateEnumOptionIfRequired(name)));
				rdr.Ignore(name.Length);
			}
			else
			{
				rdr.Ignore(name.Length);
			}

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
						paramList.Add(sb.ToString().Trim());
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
					else
					{
						rdr.Ignore(1);
						sb.Append(ch);
					}
				}
				if (sb.Length > 0) paramList.Add(sb.ToString().Trim());

				if (define.ParamNames.Count != paramList.Count) return;
			}
			
			List<Define> args = null;
			if (p.args != null)
			{
				args = new List<Define>();
				args.AddRange(p.args);
			}
			if (paramList != null)
			{
				if (define.ParamNames == null || define.ParamNames.Count != paramList.Count) return;
				if (args == null) args = new List<Define>();
				for (int i = 0, ii = paramList.Count; i < ii; i++)
				{
					args.Add(new Define(define.ParamNames[i], paramList[i], null, string.Empty, 0));
				}
			}

			string[] restrictedDefines = null;
			if (p.restrictedDefines != null) restrictedDefines = p.restrictedDefines.Concat(new string[] { name }).ToArray();
			else restrictedDefines = new string[] { name };

			var textToAdd = ResolveMacros(define.Content, restrictedDefines, args, p.fileContext, p.contentType);
			rdr.Insert(textToAdd);
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
			content = ResolveMacros(content, p.restrictedDefines, p.args, p.fileContext, p.contentType);

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

		private string ResolveMacros(string source, IEnumerable<string> restrictedDefines, IEnumerable<Define> args, FileContext serverContext, ContentType contentType)
		{
			var reader = new StringPreprocessorReader(source);
			var writer = new StringPreprocessorWriter();

			var parms = new PreprocessorParams(reader, writer, string.Empty, null, serverContext, contentType);
			parms.restrictedDefines = restrictedDefines;
			parms.args = args;
			parms.resolvingMacros = true;

			Preprocess(parms);
			return writer.Text;
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

			var includeNode = _store.GetIncludeFile(p.fileName, fileName, searchSameDir, parentFiles);
			if (includeNode == null) return;

			var rawSource = includeNode.Source;
			if (rawSource == null) return;
			var reader = new CodeSource.CodeSourcePreprocessorReader(rawSource);

			// Run the preprocessor on the include file.
			var includeSource = new CodeSource();
			var parms = new PreprocessorParams(reader, includeSource, includeNode.FullPathName, parentFiles, p.fileContext, p.contentType);
			Preprocess(parms);

			p.writer.Append(includeSource);

			AddIncludeDependency(includeNode.FullPathName, true, false);
		}

		public void AddIncludeDependency(string fullPathName, bool include, bool localizedFile)
		{
			_includeDependencies.Add(new IncludeDependency(fullPathName, include, localizedFile));
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
				Define define;
				if (_defines.TryGetValue(name, out define))
				{
					_refs.Add(new Reference(define.Definition, nameFilePos));
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
								ifLevel.result = EvaluateCondition(p, conditionStr, conditionFileName, conditionPosition);
								if (ifLevel.result == ConditionResult.Positive) ifLevel.prevResult = ConditionResult.Positive;
								break;

							case ConditionResult.Indeterminate:
								// An error on a previous #if
								ifLevel.result = EvaluateCondition(p, conditionStr, conditionFileName, conditionPosition);
								ifLevel.prevResult = ifLevel.result;
								break;
						}
					}
				}
				else
				{
					var result = EvaluateCondition(p, conditionStr, conditionFileName, conditionPosition);
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
					var result = EvaluateCondition(p, conditionStr, conditionFileName, conditionPosition);
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

		private void ProcessWarnAddDel(PreprocessorParams p)
		{
			var rdr = p.reader;
			rdr.IgnoreWhiteSpaceAndComments(true);

			var number = rdr.PeekToken(true);
			if (!string.IsNullOrEmpty(number) && char.IsNumber(number[0])) rdr.Ignore(number.Length);
		}

		private bool IsDefined(PreprocessorParams p, string name)
		{
			if (p.args != null && p.args.Any(x => x.Name == name)) return true;

			Define define;
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

		public IEnumerable<Reference> References
		{
			get { return _refs; }
		}

		private class Define
		{
			private string _name;
			private string _content;
			private List<string> _paramNames;
			private string _fileName;
			private int _pos;
			private bool _disabled;
			private DataType _dataType;
			private Definition _def;

			public Define(string name, string content, List<string> paramNames, string fileName, int pos)
			{
				_name = name;
				_content = content;
				_paramNames = paramNames;
				_fileName = fileName;
				_pos = pos;

				if (_paramNames == null)
				{
					var parser = new CodeParser(_content);
					var dataType = DataType.TryParse(new DataType.ParseArgs
					{
						Code = parser,
						TypeName = _name
					});
					if (dataType != null)
					{
						// If the data type does not consume the entire string, then this is not a data type definition.
						if (parser.Read()) dataType = null;
					}

					_dataType = dataType;
				}
			}

			public string Name
			{
				get { return _name; }
			}

			public string Content
			{
				get { return _content; }
			}

			public List<string> ParamNames
			{
				get { return _paramNames; }
			}

			public string FileName
			{
				get { return _fileName; }
			}

			public int Position
			{
				get { return _pos; }
			}

			public bool Disabled
			{
				get { return _disabled; }
				set { _disabled = value; }
			}

			public Definition Definition
			{
				get
				{
					if (_def == null)
					{
						if (_paramNames == null)
						{
							if (_dataType != null)
							{
								_def = new Definitions.DataTypeDefinition(_name, _fileName, _pos, _dataType);
							}
							else
							{
								_def = new Definitions.ConstantDefinition(_name, _fileName, _pos, CodeParser.NormalizeText(_content));
							}
						}
						else
						{
							var sig = new StringBuilder();
							sig.Append(_name);
							sig.Append('(');
							var firstParam = true;
							foreach (var paramName in _paramNames)
							{
								if (firstParam) firstParam = false;
								else sig.Append(", ");
								sig.Append(paramName);
							}
							sig.Append(')');

							_def = new Definitions.MacroDefinition(_name, _fileName, _pos, sig.ToString(), CodeParser.NormalizeText(_content));
						}
					}

					return _def;
				}
			}

			public bool IsDataType
			{
				get { return _dataType != null; }
			}
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
			public IEnumerable<Define> args;
			public IEnumerable<string> restrictedDefines;
			public ContentType contentType;
			public bool replaceInEffect;
			public bool resolvingMacros;
			public FileContext fileContext;
			public bool isMainSource;

			public PreprocessorParams(IPreprocessorReader reader, IPreprocessorWriter writer, string fileName,
				IEnumerable<string> parentFiles, FileContext serverContext, ContentType contentType)
			{
				this.reader = reader;
				this.writer = writer;
				this.fileName = fileName;
				if (parentFiles != null) this.parentFiles = parentFiles.ToArray();
				this.fileContext = serverContext;
				this.contentType = contentType;
			}
		}

		private ConditionResult EvaluateCondition(PreprocessorParams p, string conditionStr, string fileName, int pos)
		{
			// Run preprocessor on the condition string
			var reader = new StringPreprocessorReader(conditionStr);
			var writer = new StringPreprocessorWriter();
			var parms = new PreprocessorParams(reader, writer, string.Empty, null, p.fileContext, ContentType.Condition);
			parms.allowDirectives = false;
			parms.args = p.args;
			Preprocess(parms);

			// Evaluate the condition string
			var parser = new CodeParser(writer.Text);
			var tokenGroup = PreprocessorTokens.GroupToken.Parse(null, parser, null);
			var finalValue = tokenGroup.Value;

			ConditionResult ret;
			if (finalValue.HasValue)
			{
				if (finalValue.Value != 0) ret = ConditionResult.Positive;
				else ret = ConditionResult.Negative;
			}
			else
			{
#if DEBUG
				Log.WriteDebug("Condition returned indeterminate: {0}\r\n  File Name: {1}\r\n  Position: {2}", conditionStr, fileName, pos);
#endif
				ret = ConditionResult.Indeterminate;
			}
			return ret;
		}

		public struct IncludeDependency
		{
			private string _fileName;
			private bool _include;
			private bool _localizedFile;

			public IncludeDependency(string fileName, bool include, bool localizedFile)
			{
				_fileName = fileName;
				_include = include;
				_localizedFile = localizedFile;
			}

			public string FileName
			{
				get { return _fileName; }
			}

			public bool Include
			{
				get { return _include; }
			}

			public bool LocalizedFile
			{
				get { return _localizedFile; }
			}
		}

		public struct Reference
		{
			private Definition _def;
			private FilePosition _filePos;

			public Reference(Definition def, FilePosition filePos)
			{
				_def = def;
				_filePos = filePos;
			}

			public Definition Definition
			{
				get { return _def; }
			}

			public FilePosition FilePosition
			{
				get { return _filePos; }
			}
		}
	}
}

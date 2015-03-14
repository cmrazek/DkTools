using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DkTools.CodeModel.Definitions;
using DkTools.ErrorTagging;
using DkTools.CodeModel.Tokens;

namespace DkTools.CodeModel
{
	internal class PreprocessorModel
	{
		private CodeSource _source;
		private TokenParser.Parser _code;
		private DefinitionProvider _defProv;
		private string _fileName;
		private string _className;
		private Dictionary<string, VariableDefinition> _globalVars = new Dictionary<string, VariableDefinition>();
		private Dictionary<string, FunctionDefinition> _externFuncs = new Dictionary<string, FunctionDefinition>();
		private List<FunctionDefinition> _localFuncs = new List<FunctionDefinition>();
		private FileContext _fileContext;
		private bool _visible;
		private Preprocessor.IncludeDependency[] _includeDependencies;
#if DEBUG
		private List<KeyValuePair<int, Definition>> _localDefs = new List<KeyValuePair<int, Definition>>();
#endif
		private List<Definition> _globalDefs = new List<Definition>();
#if REPORT_ERRORS
		private bool _reportErrors;
		ErrorTagging.ErrorProvider _errProv = new ErrorTagging.ErrorProvider();
#endif

		public PreprocessorModel(CodeSource source, DefinitionProvider defProv, string fileName, bool visible, IEnumerable<Preprocessor.IncludeDependency> includeDependencies)
		{
#if DEBUG
			if (source == null) throw new ArgumentNullException("source");
			if (defProv == null) throw new ArgumentNullException("defProv");
#endif
			_source = source;
			_code = new TokenParser.Parser(source.Text);
			_defProv = defProv;
			_fileName = fileName;
			FunctionFileScanning.FFUtil.FileNameIsClass(_fileName, out _className);
			_fileContext = FileContextUtil.GetFileContextFromFileName(fileName);
			_visible = visible;

#if REPORT_ERRORS
			_reportErrors = visible && _fileContext != FileContext.Include && ProbeToolsPackage.Instance.EditorOptions.ShowErrors;
#endif

			if (includeDependencies != null) _includeDependencies = includeDependencies.ToArray();
			else _includeDependencies = new Preprocessor.IncludeDependency[0];

			Parse();
		}

		public CodeSource Source
		{
			get { return _source; }
		}

		private void Parse()
		{
			DataType dataType;
			int pos;

			while (!_code.EndOfFile)
			{
				if (TryReadDataType(out dataType, out pos))
				{
					AfterRootDataType(dataType, pos, FunctionPrivacy.Public, false);
					continue;
				}

				if (!_code.Read()) break;

				switch (_code.TokenType)
				{
					case TokenParser.TokenType.Word:
						switch (_code.TokenText)
						{
							case "static":
								AfterRootStatic(_code.TokenStartPostion);
								break;
							case "public":
								AfterRootPrivacy(_code.TokenText, _code.TokenStartPostion, FunctionPrivacy.Public);
								break;
							case "private":
								AfterRootPrivacy(_code.TokenText, _code.TokenStartPostion, FunctionPrivacy.Private);
								break;
							case "protected":
								AfterRootPrivacy(_code.TokenText, _code.TokenStartPostion, FunctionPrivacy.Protected);
								break;
							case "extern":
								AfterRootExtern(_code.TokenStartPostion);
								break;
#if REPORT_ERRORS
							case "create":
								AfterRootCreate(_code.TokenSpan);
								break;
#endif
							default:
								AfterRootIdentifier(_code.TokenText, _code.TokenStartPostion, _code.TokenSpan, false);
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
			VariableDefinition def;
			if (_globalVars.TryGetValue(name, out def)) return def;
			return null;
		}

		private void AfterRootDataType(DataType dataType, int dataTypeStartPos, FunctionPrivacy privacy, bool isExtern)
		{
#if DEBUG
			if (dataType == null) throw new ArgumentNullException("dataType");
#endif
			if (_code.ReadWord())
			{
				var name = _code.TokenText;
				var nameSpan = _code.TokenSpan;

				var arrayLength = TryReadArrayDecl();

				if (arrayLength == null && _code.ReadExact('('))
				{
					StartFunctionArgs(name, dataTypeStartPos, _code.TokenStartPostion, nameSpan, dataType, privacy, isExtern);
				}
				else if (_code.ReadExact(';'))
				{
					var localPos = _source.GetFilePosition(nameSpan.Start);
					var def = new VariableDefinition(name, localPos.FileName, localPos.Position, dataType, false, arrayLength);
					_globalVars[name] = def;
					AddGlobalDefinition(def);
				}
				else if (_code.ReadExact(','))
				{
					var localPos = _source.GetFilePosition(nameSpan.Start);
					var def = new VariableDefinition(name, localPos.FileName, localPos.Position, dataType, false, arrayLength);
					_globalVars[name] = def;
					AddGlobalDefinition(def);
					AfterRootDataType(dataType, dataTypeStartPos, privacy, isExtern);
				}
			}
			else
			{
#if REPORT_ERRORS
				_code.Peek();
				ReportError(_code.TokenSpan, ErrorCode.Root_UnknownAfterDataType);
#endif
			}
		}

		private void AfterRootStatic(int startPos)
		{
			var dataType = DataType.Parse(new DataType.ParseArgs
			{
				Code = _code,
				DataTypeCallback = GlobalDataTypeCallback,
				VariableCallback = GlobalVariableCallback
			});
			if (dataType != null)
			{
				AfterRootDataType(dataType, startPos, FunctionPrivacy.Public, false);
			}
			else
			{
#if REPORT_ERRORS
				_code.Peek();
				ReportError(_code.TokenSpan, ErrorCode.Root_UnknownAfterStatic);
#endif
			}
		}

		private void AfterRootExtern(int startPos)
		{
			var dataTypeStartPos = _code.Position;
			var dataType = DataType.Parse(new DataType.ParseArgs
			{
				Code = _code,
				DataTypeCallback = GlobalDataTypeCallback,
				VariableCallback = GlobalVariableCallback
			});
			if (dataType != null)
			{
				AfterRootDataType(dataType, dataTypeStartPos, FunctionPrivacy.Public, true);
			}
			else if (_code.ReadWord())
			{
				AfterRootIdentifier(_code.TokenText, _code.TokenStartPostion, _code.TokenSpan, true);
			}
			else
			{
#if REPORT_ERRORS
				_code.Peek();
				ReportError(_code.TokenSpan, ErrorCode.Root_UnknownAfterExtern);
#endif
			}
		}

		private void AfterRootIdentifier(string word, int startPos, Span wordSpan, bool isExtern)
		{
			if (_code.ReadExact('('))
			{
				// This function only gets called when no datatype was found. Assume int return type and public.
				StartFunctionArgs(word, startPos, _code.TokenStartPostion, wordSpan, DataType.Int, FunctionPrivacy.Public, isExtern);
			}
			else
			{
#if REPORT_ERRORS
				ReportError(wordSpan, ErrorCode.Root_UnknownIdent, word);
#endif
			}
		}

		private void AfterRootPrivacy(string word, int startPos, FunctionPrivacy privacy)
		{
			_code.SkipWhiteSpaceAndCommentsIfAllowed();
			var dataTypeStartPos = _code.Position;
			var dataType = DataType.Parse(new DataType.ParseArgs
			{
				Code = _code,
				DataTypeCallback = GlobalDataTypeCallback,
				VariableCallback = GlobalVariableCallback
			});
			if (dataType != null)
			{
				AfterRootDataType(dataType, dataTypeStartPos, privacy, false);
			}
			else if (_code.ReadWord())
			{
				var funcName = _code.TokenText;
				var funcNameSpan = _code.TokenSpan;
				if (_code.ReadExact('('))
				{
					StartFunctionArgs(funcName, startPos, _code.TokenStartPostion, funcNameSpan, DataType.Int, privacy, false);
				}
				else
				{
#if REPORT_ERRORS
					_code.Peek();
					ReportError(_code.TokenSpan, ErrorCode.Func_NoArgBracket);
#endif
				}
			}
			else
			{
#if REPORT_ERRORS
				_code.Peek();
				ReportError(_code.TokenSpan, ErrorCode.Func_UnknownAfterPrivacy, privacy.ToString().ToLower());
#endif
			}
		}

		private bool TryReadDataType(out DataType dataTypeOut, out int startPosOut)
		{
			_code.SkipWhiteSpaceAndCommentsIfAllowed();
			var pos = _code.Position;
			var dataType = DataType.Parse(new DataType.ParseArgs
			{
				Code = _code,
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

		private void StartFunctionArgs(string funcName, int allStartPos, int argStartPos, Span nameSpan,
			DataType returnDataType, FunctionPrivacy privacy, bool isExtern)
		{
			var localArgStartPos = _source.GetFilePosition(argStartPos);
			var argScope = new CodeScope(localArgStartPos.Position);
			int argEndPos = 0;
			var argDefList = localArgStartPos.PrimaryFile ? new List<Definition>() : null;

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
				if (TryReadFunctionArgument(argScope, localArgStartPos.PrimaryFile, argDefList)) continue;

#if REPORT_ERRORS
				_code.Peek();
				ReportError(_code.TokenSpan, ErrorCode.Func_NoArg);
#endif
				return;
			}

			int bodyStartPos;
			string description;
			if (!ReadFunctionAttributes(funcName, out bodyStartPos, out description, isExtern)) return;

			if (isExtern)
			{
				var localPos = _source.GetFilePosition(nameSpan.Start);
				var sig = TokenParser.Parser.NormalizeText(_code.GetText(allStartPos, argEndPos - allStartPos));

				var def = new FunctionDefinition(_className, funcName, localPos.FileName, localPos.Position,
					returnDataType, sig, 0, 0, 0, Span.Empty, privacy, true, description);
				_externFuncs[funcName] = def;
				AddGlobalDefinition(def);
				return;
			}

			// Read the variables at the start of the function.
			var localBodyStartPos = _source.GetFilePosition(bodyStartPos);
			CodeScope funcScope = null;
			List<Definition> varList = null;
			if (localBodyStartPos.PrimaryFile)	// Don't worry about saving variables if this function isn't in this file anyway.
			{
				funcScope = new CodeScope(argScope, bodyStartPos);
				varList = new List<Definition>();
				while (!_code.EndOfFile)
				{
					if (!TryReadVariableDeclaration(funcScope, varList)) break;
				}

				//// Add the arguments to the body as well, so they are accessible inside the function.
				//foreach (var def in argDefList)
				//{
				//	_defProv.AddLocalDefinition(localBodyStartPos.Position, def);
				//}
			}

			ReadFunctionBody();
			var bodyEndPos = _code.Position;

			var bodyStartLocalPos = _source.GetPrimaryFilePosition(bodyStartPos);
			//var nameLocalPos = _source.GetPrimaryFilePosition(nameSpan.Start);
			var nameActualPos = _source.GetFilePosition(nameSpan.Start);
			var argStartPrimaryPos = _source.GetPrimaryFilePosition(argStartPos);
			var argEndPrimaryPos = _source.GetPrimaryFilePosition(argEndPos);
			var entireSpan = _source.GetPrimaryFileSpan(new Span(allStartPos, bodyEndPos));

			var funcSig = TokenParser.Parser.NormalizeText(_code.GetText(allStartPos, argEndPos - allStartPos));
			var funcDef = new FunctionDefinition(_className, funcName, nameActualPos.FileName, nameActualPos.Position, returnDataType, funcSig, argStartPrimaryPos, argEndPrimaryPos, bodyStartLocalPos, entireSpan, privacy, isExtern, description);
			_localFuncs.Add(funcDef);
			AddGlobalDefinition(funcDef);

			// Add the definitions for the argument list
			if (argDefList != null)
			{
				Span argEffect;
				if (_visible) argEffect = new Span(localArgStartPos.Position, _source.GetPrimaryFilePosition(bodyEndPos));
				else argEffect = new Span(argStartPos, bodyEndPos);

				_defProv.AddLocal(argEffect, argDefList);
			}

			// Add the definitions for the declared variables
			if (varList != null)
			{
				Span varEffect;
				if (_visible) varEffect = new Span(bodyStartLocalPos, _source.GetPrimaryFilePosition(bodyEndPos));
				else varEffect = new Span(bodyStartPos, bodyEndPos);

				_defProv.AddLocal(varEffect, varList);
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
					switch (_code.TokenText)
					{
						case "extract":
							ReadExtract(_code.TokenSpan);
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
					// TODO: report an error if the first token is not a string literal
					var word = _code.TokenText;
					if (word == "description")
					{
						var sb = new StringBuilder();
						while (_code.ReadStringLiteral())
						{
							if (sb.Length > 0) sb.AppendLine();
							sb.Append(TokenParser.Parser.StringLiteralToString(_code.TokenText));
						}
						devDesc = sb.ToString();
						continue;
					}

					if (word == "prompt")
					{
						if (!_code.ReadStringLiteral())
						{
							// TODO: report error
							return false;
						}
						continue;
					}

					if (word == "comment")
					{
						if (!_code.ReadStringLiteral())
						{
							// TODO: report error
							return false;
						}
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
								if (!_rxAccelWord.IsMatch(_code.TokenText))
								{
									// TODO: report error
									_code.Position = resetPos;
									return false;
								}
							}
							else if (_code.ReadNumber())
							{
								if (_code.TokenText.Length != 1 || !char.IsDigit(_code.TokenText[0]))
								{
									// TODO: report error
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
							sb.Append(TokenParser.Parser.StringLiteralToString(_code.TokenText));
						}
						devDesc = sb.ToString();

						if (!_code.ReadExact("ENDHLP"))
						{
#if REPORT_ERRORS
							ReportError(_code.TokenSpan, ErrorCode.Func_NoEndHlp);
#endif
						}
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

#if REPORT_ERRORS
					ReportError(_code.TokenSpan, ErrorCode.Func_NoBodyStart);
#endif
					return false;
				}

#if REPORT_ERRORS
				_code.Peek();
				ReportError(_code.TokenSpan, ErrorCode.Func_NoBodyStart);
#endif
				return false;
			}
		}

		private bool TryReadFunctionArgument(CodeScope scope, bool createDefinitions, List<Definition> newDefList)
		{
			var dataType = DataType.Parse(new DataType.ParseArgs
			{
				Code = _code,
				DataTypeCallback = GlobalDataTypeCallback,
				VariableCallback = GlobalVariableCallback
			});
			if (dataType == null) return false;

			_code.ReadExact("&");	// Optional reference
			_code.ReadExact("+");	// Optional &+
			if (_code.ReadWord() && createDefinitions)	// Optional var name
			{
				var arrayLength = TryReadArrayDecl();

				var localPos = _source.GetFilePosition(_code.TokenStartPostion);
				var def = new VariableDefinition(_code.TokenText, localPos.FileName, localPos.Position, dataType, true, arrayLength);
				scope.AddDefinition(def);
				if (localPos.PrimaryFile)
				{
					newDefList.Add(def);
#if DEBUG
					_localDefs.Add(new KeyValuePair<int, Definition>(scope.StartPos, def));
#endif
				}
			}

			return true;
		}

		private bool TryReadVariableDeclaration(CodeScope scope, List<Definition> newDefList)
		{
			var dataType = DataType.Parse(new DataType.ParseArgs
			{
				Code = _code,
				DataTypeCallback = GlobalDataTypeCallback,
				VariableCallback = GlobalVariableCallback
			});
			if (dataType == null) return false;

			var gotVars = false;

			while (!_code.EndOfFile)
			{
				if (!_code.ReadWord()) break;

				var varName = _code.TokenText;
				var localPos = _source.GetFilePosition(_code.TokenStartPostion);

				var arrayLength = TryReadArrayDecl();

				var def = new VariableDefinition(varName, localPos.FileName, localPos.Position, dataType, false, arrayLength);
				scope.AddDefinition(def);
				if (localPos.PrimaryFile)
				{
					newDefList.Add(def);
#if DEBUG
					_localDefs.Add(new KeyValuePair<int, Definition>(scope.StartPos, def));
#endif
				}
				gotVars = true;

				if (_code.ReadExact(',')) continue;
				if (_code.ReadExact(';')) break;

#if REPORT_ERRORS
				ReportError(_code.TokenSpan, ErrorCode.VarDecl_UnknownAfterName);
#endif
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
				str = _code.TokenText;
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

		public IEnumerable<FunctionDefinition> LocalFunctions
		{
			get { return _localFuncs; }
		}

		private void ReadExtract(Span extractWordSpan)
		{
			var errorSpan = extractWordSpan;
			var permanent = _code.ReadExact("permanent");
			if (permanent) errorSpan = _code.TokenSpan;

			// Table name
			if (!_code.ReadWord())
			{
#if REPORT_ERRORS
				_code.Peek();
				ReportError(errorSpan, ErrorCode.Extract_NoName);
#endif
				return;
			}
			var name = _code.TokenText;
			ExtractTableDefinition exDef = null;
			if (!_defProv.GetGlobalFromFile<ExtractTableDefinition>(name).Any())	// Don't add a definition if it's already there (extracts can be called multiple times)
			{
				var localPos = _source.GetFilePosition(_code.TokenStartPostion);
				exDef = new ExtractTableDefinition(name, localPos.FileName, localPos.Position, permanent);
			}

			string lastToken = null;
			var fields = new List<string>();

			var done = false;
			while (!_code.EndOfFile && !done)
			{
				if (_code.ReadExact('=') && lastToken != null)
				{
					if (exDef != null)
					{
						var localPos = _source.GetFilePosition(_code.TokenStartPostion);
						var fieldDef = new ExtractFieldDefinition(lastToken, localPos.FileName, localPos.Position, exDef);
						exDef.AddField(fieldDef);
					}
					if (fields == null) fields = new List<string>();
					fields.Add(lastToken);
					lastToken = null;
				}
				else if (_code.ReadWord())
				{
					lastToken = _code.TokenText;
				}
				else
				{
					_code.Read();
					switch (_code.TokenText)
					{
						case ";":
							done = true;
							break;
						case "{":
						case "}":
#if REPORT_ERRORS
							ReportError(_code.TokenSpan, ErrorCode.Extract_NoTerminator);
#endif
							done = true;
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
			int len;

			while (!_code.EndOfFile)
			{
				if (_code.ReadExact('[') &&
					_code.ReadNumber() &&
					int.TryParse(_code.TokenText, out len) &&
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

			return arrayLengths != null ? arrayLengths.ToArray() : null;
		}

		public IEnumerable<Preprocessor.IncludeDependency> IncludeDependencies
		{
			get { return _includeDependencies; }
		}

#if REPORT_ERRORS
		private void ReportError(Span rawSpan, ErrorCode errCode, params object[] args)
		{
			if (!_reportErrors) return;
			var primarySpan = _source.GetPrimaryFileSpan(rawSpan);
			if (primarySpan.Length > 0)
			{
				_errProv.ReportError(_code, primarySpan, errCode, args);
			}
		}

		public bool ReportErrors
		{
			get { return _reportErrors; }
		}

		public ErrorProvider ErrorProvider
		{
			get { return _errProv; }
		}
#endif

#if DEBUG
		public string Dump()
		{
			var sb = new StringBuilder();
			if (!string.IsNullOrEmpty(_fileName))
			{
				sb.Append("File Name: ");
				sb.AppendLine(_fileName);
			}
			if (!string.IsNullOrEmpty(_className))
			{
				sb.Append("Class Name: ");
				sb.AppendLine(_className);
			}

			sb.AppendLine();
			sb.AppendLine("Global Variables:");
			var count = 0;
			foreach (var def in _globalVars.Values)
			{
				sb.AppendLine(def.Dump());
				count++;
			}
			sb.AppendFormat("{0} global variable(s)", count);
			sb.AppendLine();
			sb.AppendLine();

			sb.AppendLine("Extern Functions:");
			count = 0;
			foreach (var def in _externFuncs.Values)
			{
				sb.AppendLine(def.Dump());
				count++;
			}
			sb.AppendFormat("{0} extern func(s)", count);
			sb.AppendLine();
			sb.AppendLine();

			sb.AppendLine("Local Functions:");
			count = 0;
			foreach (var def in _localFuncs)
			{
				sb.AppendLine(def.Dump());
				count++;
			}
			sb.AppendFormat("{0} local func(s)", count);
			sb.AppendLine();
			sb.AppendLine();

			sb.AppendLine("Local Definitions:");
			count = 0;
			foreach (var pair in _localDefs)
			{
				sb.Append(pair.Key);
				sb.Append(": ");
				sb.AppendLine(pair.Value.Dump());
				count++;
			}
			sb.AppendFormat("{0} local definition(s)", count);
			sb.AppendLine();
			sb.AppendLine();

			sb.AppendLine("--- Definition Provider ---");
			sb.Append(_defProv.DumpDefinitions());

			sb.AppendLine();
			sb.AppendLine();

			sb.Append(_source.Text);
			return sb.ToString();
		}
#endif

		private class CodeScope
		{
			private CodeScope _parent;
			private int _startPos;
			private DefinitionCollection _defs = new DefinitionCollection();

			public CodeScope(int scopeTokenStartPos)
			{
				_parent = null;
				_startPos = scopeTokenStartPos;
			}

			public CodeScope(CodeScope parent, int scopeTokenStartPos)
			{
#if DEBUG
				if (parent == null) throw new ArgumentNullException("parent");
#endif
				_parent = parent;
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

#if REPORT_ERRORS
		private void AfterRootCreate(Span createSpan)
		{
			if (_code.ReadExact("table"))
			{
				AfterCreateTable(createSpan, _code.TokenSpan);
			}
			else
			{
				ReportError(_code.TokenSpan, ErrorCode.Root_UnknownAfterCreate, _code.TokenText);
			}
		}

		private void AfterCreateTable(Span createSpan, Span tableSpan)
		{
			// Table name
			if (!_code.ReadWord())
			{
				ReportError(tableSpan, ErrorCode.CreateTable_NoTableName);
				return;
			}
			var tableName = _code.TokenText;
			var tableNameSpan = _code.TokenSpan;

			// Schema number
			if (!_code.ReadNumber())
			{
				ReportError(tableNameSpan, ErrorCode.CreateTable_NoTableNumber, tableName);
				return;
			}

			// Schema number + 1
			_code.ReadNumber();

			// Read the attributes before the columns
			while (!_code.EndOfFile)
			{
				if (!_code.Read()) break;

				if (_code.TokenType == TokenParser.TokenType.Operator)
				{
					if (_code.TokenText == "(")
					{
						break;
					}
					else if (_code.TokenText == "{")
					{
						ReportError(_code.TokenSpan, ErrorCode.CreateTable_UsingOpenBraceInsteadOfBracket);
						break;
					}
					else
					{
						ReportError(_code.TokenSpan, ErrorCode.CreateTable_NoOpenBrace);
						break;
					}
				}
				else if (_code.TokenType == TokenParser.TokenType.Word)
				{
					var word = _code.TokenText;
					var wordSpan = _code.TokenSpan;
					switch (word)
					{
						case "updates":
						case "display":
						case "modal":
						case "pick":
						case "nopick":
							// No trailing values required.
							break;
						case "database":
							if (!_code.ReadNumber())
							{

								ReportError(wordSpan, ErrorCode.CreateTable_NoDatabaseNumber);
							}
							break;
						case "snapshot":
							if (!_code.ReadNumber())
							{
								ReportError(wordSpan, ErrorCode.CreateTable_NoFrequencyNumber);
							}
							break;
						case "prompt":
							if (!_code.ReadStringLiteral())
							{
								ReportError(wordSpan, ErrorCode.CreateTable_NoPromptString);
							}
							break;
						case "comment":
							if (!_code.ReadStringLiteral())
							{
								ReportError(wordSpan, ErrorCode.CreateTable_NoCommentString);
							}
							break;
						case "image":
							if (!_code.ReadStringLiteral())
							{
								ReportError(wordSpan, ErrorCode.CreateTable_NoImageString);
							}
							break;
						case "description":
							{
								var gotString = false;
								while (_code.ReadStringLiteral())
								{
									gotString = true;
								}
								if (!gotString)
								{
									ReportError(wordSpan, ErrorCode.CreateTable_NoDescriptionString);
								}
							}
							break;
						case "tag":
							{
								if (!_code.ReadTagName())
								{
									ReportError(wordSpan, ErrorCode.CreateTable_NoTagName);
								}
								var tagName = _code.TokenText;

								switch (tagName)
								{
									case "probeform:nobuttonbar":
									case "probeform:stayloaded":
									case "cols":
									case "rows":
									case "formposition":
										break;
									default:
										ReportError(_code.TokenSpan, ErrorCode.CreateTable_InvalidTagName, tagName);
										break;
								}

								if (!_code.ReadStringLiteral())
								{
									ReportError(_code.TokenSpan, ErrorCode.CreateTable_NoTagValue, tagName);
									return;
								}
							}
							break;
					}
				}
				else
				{
					ReportError(_code.TokenSpan, ErrorCode.CreateTable_NoOpenBrace);
					break;
				}
			}

			// Column definitions
			var gotUpdates = false;
			while (!_code.EndOfFile)
			{
				if (_code.ReadExact("updates") && !gotUpdates)
				{
					gotUpdates = true;
					if (!_code.ReadWord())
					{
						ReportError(_code.TokenSpan, ErrorCode.CreateTable_NoUpdatesTableName);
						return;
					}
					if (_code.TokenText.Length > Constants.MaxTableNameLength)
					{
						ReportError(_code.TokenSpan, ErrorCode.CreateTable_UpdatesTableNameTooLong, Constants.MaxTableNameLength);
					}
				}

				if (TryReadColumnDefinition()) continue;

				if (!_code.ReadExact(')'))
				{
					if (_code.ReadExact('}'))
					{
						ReportError(_code.TokenSpan, ErrorCode.CreateTable_UsingCloseBraceInsteadOfBracket);
					}
					else
					{
						ReportError(_code.TokenSpan, ErrorCode.CreateTable_NoCloseBrace);
						return;
					}
				}
				else break;
			}
		}

		private static Regex _rxIntensityKeyword = new Regex(@"^INTENSITY_\d+$");

		private bool TryReadColumnDefinition()
		{
			if (!_code.ReadWord()) return false;

			var colName = _code.TokenText;
			var colNameSpan = _code.TokenSpan;

			var dataType = DataType.Parse(_code, dataTypeCallback: GlobalDataTypeCallback, flags: DataType.ParseFlag.Strict, errorProv: _errProv);
			if (dataType == null)
			{
				ReportError(colNameSpan, ErrorCode.ColDef_NoDataType, colName);
				return false;
			}

			_code.ReadStringLiteral();	// mask

			var stopColDef = false;
			while (!_code.EndOfFile && !stopColDef)
			{
				if (_code.ReadExact(','))
				{
					stopColDef = true;
				}
				else if (_code.ReadWord())
				{
					var word = _code.TokenText;
					var wordSpan = _code.TokenSpan;
					switch (word)
					{
						case "ALLCAPS":
						case "AUTOCAP":
						case "form":
						case "formonly":
						case "audit":
						case "noaudit":
						case "tool":
						case "endgroup":
							// These keywords don't have any trailing values.
							break;
						case "INPUT":
						case "NOINPUT":
						case "NOECHO":
						case "NODISPLAY":
						case "NOCHANGE":
						case "NOUSE":
						case "REQUIRED":
							if (_code.ReadExact('+'))
							{
								if (!_code.ReadWord())
								{
									if (!_rxIntensityKeyword.IsMatch(_code.TokenText))
									{
										ReportError(_code.TokenSpan, ErrorCode.ColDef_NoIntensity);
									}
								}
							}
							break;
						case "accel":
							if (!TryReadAccelSequence())
							{
								ReportError(wordSpan, ErrorCode.ColDef_NoAccelSequence);
							}
							break;
						case "zoom":
							_code.ReadExact("nopersist");
							break;
						case "image":
							if (!_code.ReadStringLiteral())
							{
								ReportError(wordSpan, ErrorCode.ColDef_NoImageFileName);
							}
							break;
						case "prompt":
							if (!_code.ReadStringLiteral())
							{
								ReportError(wordSpan, ErrorCode.ColDef_NoPromptString);
							}
							break;
						case "comment":
							if (!_code.ReadStringLiteral())
							{
								ReportError(wordSpan, ErrorCode.ColDef_NoCommentString);
							}
							break;
						case "description":
							if (_code.ReadStringLiteral())
							{
								while (_code.ReadStringLiteral()) ;
							}
							else
							{
								ReportError(wordSpan, ErrorCode.ColDef_NoDescriptionStrings);
							}
							break;
						case "row":
						case "col":
							if (_code.ReadExact('+') || _code.ReadExact('-'))
							{
								if (!_code.ReadNumber())
								{
									ReportError(_code.TokenSpan, ErrorCode.ColDef_NoCoordinateOffset, _code.TokenText);
								}
							}
							else if (!_code.ReadNumber())
							{
								ReportError(_code.TokenSpan, ErrorCode.ColDef_NoCoordinate, word);
							}
							break;
						case "rows":
						case "cols":
							if (!_code.ReadNumber())
							{
								ReportError(wordSpan, ErrorCode.ColDef_NoCoordinate, word == "rows" ? "width" : "height", word);
							}
							break;
						case "group":
							if (!_code.ReadStringLiteral())
							{
								ReportError(wordSpan, ErrorCode.ColDef_NoGroupTitle);
							}
							break;
						default:
							stopColDef = true;
							break;
					}
				}
				else
				{
					_code.Read();
					ReportError(_code.TokenSpan, ErrorCode.ColDef_UnknownAttribute, _code.TokenText);
				}
			}

			return true;
		}

		private static readonly Regex _rxFKey = new Regex(@"\G(ALT|CTRL|SHIFT|F\d{1,2}|[A-Za-z0-9])");

		private bool TryReadAccelSequence()
		{
			var expectingPlus = false;
			var gotItem = false;
			var plusSpan = Span.Empty;

			while (!_code.EndOfFile)
			{
				if (expectingPlus)
				{
					if (!_code.ReadExact('+')) break;
					plusSpan = _code.TokenSpan;
					expectingPlus = false;
				}
				else
				{
					if (_code.ReadPattern(_rxFKey))
					{
						gotItem = true;
						expectingPlus = true;
					}
					else if (gotItem)
					{
						ReportError(plusSpan, ErrorCode.ColDef_NoKeyCodeAfterPlus);
						break;
					}
					else break;
				}
			}

			return gotItem;
		}
#endif
	}
}

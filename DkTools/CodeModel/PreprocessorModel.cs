﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DkTools.CodeModel.Definitions;
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
#if DEBUG
		private List<KeyValuePair<int, Definition>> _localDefs = new List<KeyValuePair<int, Definition>>();
#endif
		private List<Definition> _globalDefs = new List<Definition>();
#if REPORT_ERRORS
		private List<ErrorTagging.ErrorInfo> _errors = new List<ErrorTagging.ErrorInfo>();
		private bool _reportErrors;
#endif

		public PreprocessorModel(CodeSource source, DefinitionProvider defProv, string fileName, bool visible)
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

#if REPORT_ERRORS
			_reportErrors = visible && ProbeToolsPackage.Instance.EditorOptions.ShowErrors;
#endif

			Parse();
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
			var def = _defProv.GetGlobal(name).FirstOrDefault();
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
				ReportError(_code.TokenSpan, "Expected function or variable name to follow data type on root.");
#endif
			}
		}

		private void AfterRootStatic(int startPos)
		{
			var dataType = DataType.Parse(_code, null, GlobalDataTypeCallback, GlobalVariableCallback);
			if (dataType != null)
			{
				AfterRootDataType(dataType, startPos, FunctionPrivacy.Public, false);
			}
			else
			{
#if REPORT_ERRORS
				_code.Peek();
				ReportError(_code.TokenSpan, "Expected data type to follow 'static'.");
#endif
			}
		}

		private void AfterRootExtern(int startPos)
		{
			var dataType = DataType.Parse(_code, null, GlobalDataTypeCallback, GlobalVariableCallback);
			if (dataType != null)
			{
				AfterRootDataType(dataType, startPos, FunctionPrivacy.Public, true);
			}
			else if (_code.ReadWord())
			{
				AfterRootIdentifier(_code.TokenText, startPos, _code.TokenSpan, true);
			}
			else
			{
#if REPORT_ERRORS
				_code.Peek();
				ReportError(_code.TokenSpan, "Expected data type or function name to follow 'extern'.");
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
				ReportError(wordSpan, string.Format("Unknown identifier '{0}'.", word));
#endif
			}
		}

		private void AfterRootPrivacy(string word, int startPos, FunctionPrivacy privacy)
		{
			_code.SkipWhiteSpaceAndCommentsIfAllowed();
			var dataTypeStartPos = _code.Position;
			var dataType = DataType.Parse(_code, null, GlobalDataTypeCallback, GlobalVariableCallback);
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
					ReportError(_code.TokenSpan, string.Format("Expected '(' to follow function name."));
#endif
				}
			}
			else
			{
#if REPORT_ERRORS
				_code.Peek();
				ReportError(_code.TokenSpan, string.Format("Expected function name or data type to follow '{0}'.", privacy.ToString().ToLower()));
#endif
			}
		}

		private bool TryReadDataType(out DataType dataTypeOut, out int startPosOut)
		{
			_code.SkipWhiteSpaceAndCommentsIfAllowed();
			var pos = _code.Position;
			var dataType = DataType.Parse(_code, null, GlobalDataTypeCallback, GlobalVariableCallback);
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
				ReportError(_code.TokenSpan, "Expected function argument.");
#endif
				return;
			}

			int bodyStartPos;
			string description;
			if (!ReadFunctionAttributes(funcName, out bodyStartPos, out description, isExtern)) return;

			if (isExtern)
			{
				var localPos = _source.GetFilePosition(nameSpan.Start);
				var sig = Tokens.Token.NormalizePlainText(_code.GetText(allStartPos, argEndPos - allStartPos));

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
			var nameLocalPos = _source.GetPrimaryFilePosition(nameSpan.Start);
			var argStartPrimaryPos = _source.GetPrimaryFilePosition(argStartPos);
			var argEndPrimaryPos = _source.GetPrimaryFilePosition(argEndPos);
			var entireSpan = _source.GetPrimaryFileSpan(new Span(allStartPos, bodyEndPos));

			var funcSig = Tokens.Token.NormalizePlainText(_code.GetText(allStartPos, argEndPos - allStartPos));
			var funcDef = new FunctionDefinition(_className, funcName, _fileName, nameLocalPos, returnDataType, funcSig, argStartPrimaryPos, argEndPrimaryPos, bodyStartLocalPos, entireSpan, privacy, isExtern, description);
			_localFuncs.Add(funcDef);
			AddGlobalDefinition(funcDef);

			// Add the definitions for the argument list
			if (argDefList != null)
			{
				var argEffect = new Span(localArgStartPos.Position, _source.GetPrimaryFilePosition(bodyEndPos));
				_defProv.AddLocal(argEffect, argDefList);
			}

			// Add the definitions for the declared variables
			if (varList != null)
			{
				var varEffect = new Span(bodyStartLocalPos, _source.GetPrimaryFilePosition(bodyEndPos));
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
							ReportError(_code.TokenSpan, "Expected 'ENDHLP'.");
#endif
						}
						continue;
					}

#if REPORT_ERRORS
					ReportError(_code.TokenSpan, "Expected '{'.");
#endif
					return false;
				}

#if REPORT_ERRORS
				_code.Peek();
				ReportError(_code.TokenSpan, "Expected '{'.");
#endif
				return false;
			}
		}

		private bool TryReadFunctionArgument(CodeScope scope, bool createDefinitions, List<Definition> newDefList)
		{
			var dataType = DataType.Parse(_code, null, GlobalDataTypeCallback, GlobalVariableCallback);
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
			var dataType = DataType.Parse(_code, null, GlobalDataTypeCallback, GlobalVariableCallback);
			if (dataType == null) return false;

			var gotVars = false;

			while (!_code.EndOfFile)
			{
				if (!_code.ReadWord()) break;

				var arrayLength = TryReadArrayDecl();

				var localPos = _source.GetFilePosition(_code.TokenStartPostion);
				var def = new VariableDefinition(_code.TokenText, localPos.FileName, localPos.Position, dataType, false, arrayLength);
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
				ReportError(_code.TokenSpan, "Expected either ',' or ';' to follow a variable declaration.");
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
			_defProv.AddGlobal(def);
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
				ReportError(errorSpan, "Expected extract name.");
#endif
				return;
			}
			var name = _code.TokenText;
			ExtractTableDefinition exDef = null;
			if (!_defProv.GetGlobal<ExtractTableDefinition>(name).Any())	// Don't add a definition if it's already there (extracts can be called multiple times)
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
						var fieldDef = new ExtractFieldDefinition(lastToken, localPos.FileName, localPos.Position);
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
							ReportError(_code.TokenSpan, "Expected ';' to end extract.");
#endif
							done = true;
							break;
					}
				}
			}

			if (exDef != null)
			{
				_defProv.AddGlobal(exDef);
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

#if REPORT_ERRORS
		private void ReportError(Span rawSpan, string message)
		{
			if (!_reportErrors) return;
			var primarySpan = _source.GetPrimaryFileSpan(rawSpan);
			if (primarySpan.Length > 0)
			{
				_errors.Add(new ErrorTagging.ErrorInfo(message, primarySpan));
			}
		}

		public IEnumerable<ErrorTagging.ErrorInfo> Errors
		{
			get { return _errors; }
		}

		public IEnumerable<ErrorTagging.ErrorInfo> GetErrorsForPos(int pos)
		{
			foreach (var error in _errors)
			{
				if (error.Span.Contains(pos)) yield return error;
			}
		}

		public bool ReportErrors
		{
			get { return _reportErrors; }
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
	}
}

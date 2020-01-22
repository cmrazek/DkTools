using System;
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
		private CodeParser _code;
		private DefinitionProvider _defProv;
		private string _fileName;
		private string _className;
		private Dictionary<string, VariableDefinition> _globalVars = new Dictionary<string, VariableDefinition>();
		private Dictionary<string, FunctionDefinition> _externFuncs = new Dictionary<string, FunctionDefinition>();
		private List<LocalFunction> _localFuncs = new List<LocalFunction>();
		private FileContext _fileContext;
		private bool _visible;
		private Preprocessor.IncludeDependency[] _includeDependencies;
		private Preprocessor _prep;
		private List<Definition> _globalDefs = new List<Definition>();

		public PreprocessorModel(CodeSource source, DefinitionProvider definitionProvider, string fileName, bool visible, IEnumerable<Preprocessor.IncludeDependency> includeDependencies)
		{
			_source = source ?? throw new ArgumentNullException(nameof(source));
			_code = new CodeParser(source.Text);
			_defProv = definitionProvider ?? throw new ArgumentNullException(nameof(definitionProvider));
			_fileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
			FunctionFileScanning.FFUtil.FileNameIsClass(_fileName, out _className);
			_fileContext = FileContextUtil.GetFileContextFromFileName(fileName);
			_visible = visible;

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

				switch (_code.Type)
				{
					case CodeType.Word:
						switch (_code.Text)
						{
							case "static":
								AfterRootStatic(_code.TokenStartPostion);
								break;
							case "public":
								AfterRootPrivacy(_code.Text, _code.TokenStartPostion, FunctionPrivacy.Public);
								break;
							case "private":
								AfterRootPrivacy(_code.Text, _code.TokenStartPostion, FunctionPrivacy.Private);
								break;
							case "protected":
								AfterRootPrivacy(_code.Text, _code.TokenStartPostion, FunctionPrivacy.Protected);
								break;
							case "extern":
								AfterRootExtern(_code.TokenStartPostion);
								break;
							default:
								AfterRootIdentifier(_code.Text, _code.TokenStartPostion, _code.Span, false);
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
				var name = _code.Text;
				var nameSpan = _code.Span;

				var arrayLength = TryReadArrayDecl();

				if (arrayLength == null && _code.ReadExact('('))
				{
					StartFunctionArgs(name, dataTypeStartPos, _code.TokenStartPostion, nameSpan, dataType, privacy, isExtern);
				}
				else if (_code.ReadExact(';'))
				{
					var localPos = _source.GetFilePosition(nameSpan.Start);
					var def = new VariableDefinition(name, localPos, dataType, false, arrayLength, VariableType.Global);
					_globalVars[name] = def;
					AddGlobalDefinition(def);
				}
				else if (_code.ReadExact(','))
				{
					var localPos = _source.GetFilePosition(nameSpan.Start);
					var def = new VariableDefinition(name, localPos, dataType, false, arrayLength, VariableType.Global);
					_globalVars[name] = def;
					AddGlobalDefinition(def);
					AfterRootDataType(dataType, dataTypeStartPos, privacy, isExtern);
				}
			}
		}

		private void AfterRootStatic(int startPos)
		{
			var dataType = DataType.TryParse(new DataType.ParseArgs
			{
				Code = _code,
				DataTypeCallback = GlobalDataTypeCallback,
				VariableCallback = GlobalVariableCallback
			});
			if (dataType != null)
			{
				AfterRootDataType(dataType, startPos, FunctionPrivacy.Public, false);
			}
		}

		private void AfterRootExtern(int startPos)
		{
			var dataTypeStartPos = _code.Position;
			var dataType = DataType.TryParse(new DataType.ParseArgs
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
				AfterRootIdentifier(_code.Text, _code.TokenStartPostion, _code.Span, true);
			}
		}

		private void AfterRootIdentifier(string word, int startPos, Span wordSpan, bool isExtern)
		{
			if (_code.ReadExact('('))
			{
				// This function only gets called when no datatype was found. Assume int return type and public.
				StartFunctionArgs(word, startPos, _code.TokenStartPostion, wordSpan, DataType.Int, FunctionPrivacy.Public, isExtern);
			}
		}

		private void AfterRootPrivacy(string word, int startPos, FunctionPrivacy privacy)
		{
			_code.SkipWhiteSpace();
			var dataTypeStartPos = _code.Position;
			var dataType = DataType.TryParse(new DataType.ParseArgs
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
				var funcName = _code.Text;
				var funcNameSpan = _code.Span;
				if (_code.ReadExact('('))
				{
					StartFunctionArgs(funcName, startPos, _code.TokenStartPostion, funcNameSpan, DataType.Int, privacy, false);
				}
			}
		}

		private bool TryReadDataType(out DataType dataTypeOut, out int startPosOut)
		{
			_code.SkipWhiteSpace();
			var pos = _code.Position;
			var dataType = DataType.TryParse(new DataType.ParseArgs
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
			var args = new List<ArgumentDescriptor>();
			var argDefList = new List<Definition>();

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

			int bodyStartPos;
			string description;
			if (!ReadFunctionAttributes(funcName, out bodyStartPos, out description, isExtern)) return;

			if (isExtern)
			{
				var localPos = _source.GetFilePosition(nameSpan.Start);

				var sig = new FunctionSignature(true, privacy, returnDataType, _className, funcName, description, args);
				sig.ApplyDocumentation(localPos.FileName);
				var def = new FunctionDefinition(sig, localPos, 0, 0, 0, Span.Empty);
				_externFuncs[funcName] = def;
				AddGlobalDefinition(def);
				return;
			}

			// Read the variables at the start of the function.
			var localBodyStartPos = _source.GetFilePosition(bodyStartPos);
			CodeScope funcScope = null;
			var varList = new List<Definition>();
			if (!_visible || localBodyStartPos.PrimaryFile)	// Don't worry about saving variables if this function isn't in this file anyway.
			{
				funcScope = new CodeScope(argScope, bodyStartPos);
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

			var statementsStartPos = _code.Position;

			ReadFunctionBody();
			var bodyEndPos = _code.Position;

			var bodyStartLocalPos = _source.GetPrimaryFilePosition(bodyStartPos);
			var nameActualPos = _source.GetFilePosition(nameSpan.Start);
			var argStartPrimaryPos = _source.GetPrimaryFilePosition(argStartPos);
			var argEndPrimaryPos = _source.GetPrimaryFilePosition(argEndPos);
			var entireSpan = _source.GetPrimaryFileSpan(new Span(allStartPos, bodyEndPos));

			var funcDef = new FunctionDefinition(new FunctionSignature(false, privacy, returnDataType, _className, funcName, description, args),
				nameActualPos, argStartPrimaryPos, argEndPrimaryPos, bodyStartLocalPos, entireSpan);
			_localFuncs.Add(new LocalFunction(funcDef, nameSpan, statementsStartPos, bodyEndPos, argDefList, varList));
			AddGlobalDefinition(funcDef);

			// Add the definitions for the argument list
			Span argEffect;
			if (_visible) argEffect = new Span(localArgStartPos.Position, _source.GetPrimaryFilePosition(bodyEndPos));
			else argEffect = new Span(argStartPos, bodyEndPos);

			_defProv.AddLocal(argEffect, argDefList);

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
					switch (_code.Text)
					{
						case "extract":
							ReadExtract(_code.Span);
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

		private bool TryReadFunctionArgument(CodeScope scope, bool createDefinitions, List<ArgumentDescriptor> args, List<Definition> newDefList)
		{
			var dataType = DataType.TryParse(new DataType.ParseArgs
			{
				Code = _code,
				DataTypeCallback = GlobalDataTypeCallback,
				VariableCallback = GlobalVariableCallback
			});
			if (dataType == null) return false;

			PassByMethod passByMethod = PassByMethod.Value;
			if (_code.ReadExact("&"))	// Optional reference
			{
				if (_code.ReadExact("+"))	// Optional &+
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

			if (name != null && createDefinitions)	// Optional var name
			{
				var arrayLength = TryReadArrayDecl();

				var localPos = _source.GetFilePosition(_code.TokenStartPostion);
				var def = new VariableDefinition(_code.Text, localPos, dataType, true, arrayLength, VariableType.Argument);
				scope.AddDefinition(def);
				if (!_visible || localPos.PrimaryFile)
				{
					newDefList.Add(def);
				}
			}

			return true;
		}

		private bool TryReadVariableDeclaration(CodeScope scope, List<Definition> newDefList)
		{
			var dataType = DataType.TryParse(new DataType.ParseArgs
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

				var varName = _code.Text;
				var localPos = _source.GetFilePosition(_code.TokenStartPostion);

				var arrayLength = TryReadArrayDecl();

				var def = new VariableDefinition(varName, localPos, dataType, false, arrayLength, VariableType.Local);
				scope.AddDefinition(def);
				if (!_visible || localPos.PrimaryFile)
				{
					newDefList.Add(def);
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

		private void ReadExtract(Span extractWordSpan)
		{
			var errorSpan = extractWordSpan;
			var permanent = _code.ReadExact("permanent");
			if (permanent) errorSpan = _code.Span;

			// Table name
			if (!_code.ReadWord()) return;
			var name = _code.Text;
			ExtractTableDefinition exDef = null;
			if (!_defProv.GetGlobalFromFile<ExtractTableDefinition>(name).Any())	// Don't add a definition if it's already there (extracts can be called multiple times)
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
			int len;

			while (!_code.EndOfFile)
			{
				if (_code.ReadExact('[') &&
					_code.ReadNumber() &&
					int.TryParse(_code.Text, out len) &&
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

		public Preprocessor Preprocessor
		{
			get { return _prep; }
			set { _prep = value; }
		}

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
				sb.AppendLine(def.Definition.Dump());
				count++;
			}
			sb.AppendFormat("{0} local func(s)", count);
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

		public class LocalFunction
		{
			private FunctionDefinition _def;
			private int _startPos;
			private int _endPos;
			private Definition[] _args;
			private Definition[] _vars;
			private Span _nameSpan;

			public LocalFunction(FunctionDefinition def, Span nameSpan, int startPos, int endPos, IEnumerable<Definition> args, IEnumerable<Definition> vars)
			{
				_def = def;
				_startPos = startPos;
				_endPos = endPos;
				_args = args.ToArray();
				_vars = vars.ToArray();
				_nameSpan = nameSpan;
			}

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

			public Definition[] Arguments
			{
				get { return _args; }
			}

			public Definition[] Variables
			{
				get { return _vars; }
			}

			/// <summary>
			/// Span of the function name, in preprocessor coordinates.
			/// </summary>
			public Span NameSpan
			{
				get { return _nameSpan; }
			}
		}
	}
}

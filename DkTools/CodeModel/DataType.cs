using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DkTools.CodeModel.Definitions;
using DkTools.CodeModel.Tokens;

namespace DkTools.CodeModel
{
	internal class DataType
	{
		private string _name;
		private string _infoText;
		private Definition[] _completionOptions;
		private CompletionOptionsType _completionOptionsType;
		private Definition[] _methods;
		private Definition[] _properties;
		private ValType _valueType;

		public enum CompletionOptionsType
		{
			None,
			EnumOptionsList,
			Tables,
			RelInds,
			InterfaceMembers
		}

		public static readonly DataType Boolean_t = new DataType(ValType.Enum, "Boolean_t")
		{
			_completionOptionsType = CompletionOptionsType.EnumOptionsList,
			_completionOptions = new Definition[]
			{
				new EnumOptionDefinition("TRUE"),
				new EnumOptionDefinition("FALSE")
			}
		};
		public static readonly DataType Char = new DataType(ValType.Char, "char");
		public static readonly DataType Char255 = new DataType(ValType.String, "char(255)");
		public static readonly DataType Command = new DataType(ValType.Command, "command");
		public static readonly DataType Date = new DataType(ValType.Date, "date");
		public static readonly DataType Enum = new DataType(ValType.Enum, "enum");
		public static readonly DataType IndRel = new DataType(ValType.IndRel, "indrel") { _completionOptionsType = CompletionOptionsType.RelInds };
		public static readonly DataType Int = new DataType(ValType.Numeric, "int");
		public static readonly DataType Numeric = new DataType(ValType.Numeric, "numeric");
		public static readonly DataType OleObject = new DataType(ValType.Interface, "oleobject");
		public static readonly DataType String = new DataType(ValType.String, "string");
		public static readonly DataType StringVarying = new DataType(ValType.String, "string varying");
		public static readonly DataType Table = new DataType(ValType.Table, "table") { _completionOptionsType = CompletionOptionsType.Tables };
		public static readonly DataType Ulong = new DataType(ValType.Numeric, "ulong");
		public static readonly DataType Unsigned = new DataType(ValType.Numeric, "unsigned");
		public static readonly DataType Variant = new DataType(ValType.Interface, "variant");
		public static readonly DataType Void = new DataType(ValType.Void, "void");

		public delegate DataTypeDefinition GetDataTypeDelegate(string name);
		public delegate VariableDefinition GetVariableDelegate(string name);

		/// <summary>
		/// Creates a new data type object.
		/// </summary>
		/// <param name="name">The name or visible text of the data type. This will also be used as the info text.</param>
		public DataType(ValType valueType, string name)
		{
			_valueType = valueType;
			_name = name;
			_infoText = name;
		}

		/// <summary>
		/// Creates a new data type object.
		/// </summary>
		/// <param name="name">(required) name of the data type</param>
		/// <param name="infoText">(required) Help text to be displayed to the user.</param>
		/// <param name="sourceToken">(optional) The token that created the data type.</param>
		public DataType(ValType valueType, string name, string infoText)
		{
			_valueType = valueType;

			if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(infoText))
			{
				_name = name;
				_infoText = infoText;
			}
			else if (!string.IsNullOrEmpty(name))
			{
				_name = name;
			}
			else if (!string.IsNullOrEmpty(infoText))
			{
				_name = infoText;
			}
			else throw new ArgumentNullException("name and infoText");
		}

		/// <summary>
		/// Creates a new data type object.
		/// </summary>
		/// <param name="name">(required) Name of the data type</param>
		/// <param name="completionOptions">(optional) A list of hardcoded options for this data type.</param>
		/// <param name="infoText">(required) Help text to be displayed to the user.</param>
		/// <param name="sourceToken">(optional) The token that created the data type.</param>
		public DataType(ValType valueType, string name, IEnumerable<Definition> completionOptions, CompletionOptionsType optionsType, string infoText)
		{
			_valueType = valueType;
			_name = name;
			_infoText = infoText;

			if (completionOptions != null)
			{
				_completionOptions = completionOptions.ToArray();
				if (_completionOptions.Length > 0) _completionOptionsType = optionsType;
			}
		}

		// TODO: remove
		//public static DataType FromString(string str)
		//{
		//	return new DataType(str, str);
		//}

		public DataType CloneWithNewName(string newName)
		{
			return new DataType(_valueType, newName, _infoText)
			{
				_completionOptions = _completionOptions,
				_completionOptionsType = _completionOptionsType
			};
		}

		public string Name
		{
			get { return _name; }
			//set { _name = value; }		TODO: remove
		}

		public bool HasCompletionOptions
		{
			get { return _completionOptionsType != CompletionOptionsType.None; }
		}

		public IEnumerable<Definition> CompletionOptions
		{
			get
			{
				switch (_completionOptionsType)
				{
					case CompletionOptionsType.EnumOptionsList:
						if (_completionOptions != null)
						{
							foreach (var opt in _completionOptions) yield return opt;
						}
						break;

					case CompletionOptionsType.Tables:
						foreach (var table in ProbeEnvironment.Tables)
						{
							foreach (var def in table.Definitions) yield return def;
						}
						break;

					case CompletionOptionsType.RelInds:
						yield return RelIndDefinition.Physical;
						foreach (var r in ProbeEnvironment.RelInds) yield return r.Definition;
						break;
				}
			}
		}

		public static string[] DataTypeStartingKeywords = new string[] { "char", "date", "enum", "int", "indrel", "like", "numeric", "string", "table", "time", "unsigned", "void" };

		[Flags]
		public enum ParseFlag
		{
			Strict,
			FromRepo,
			InterfaceType
		}

		public class ParseArgs
		{
			/// <summary>
			/// The token parser to read from.
			/// </summary>
			public TokenParser.Parser Code { get; set; }

			/// <summary>
			/// (optional) Flags to control the parsing behaviour.
			/// </summary>
			public ParseFlag Flags { get; set; }

			/// <summary>
			/// (optional) A callback function used to look up existing data types.
			/// </summary>
			public GetDataTypeDelegate DataTypeCallback { get; set; }

			/// <summary>
			/// (optional) A callback function used to look up existing variables.
			/// </summary>
			public GetVariableDelegate VariableCallback { get; set; }

			/// <summary>
			/// (optional) A name to be given to the data type. If null or blank, the actual text will be used as the name.
			/// </summary>
			public string TypeName { get; set; }

#if REPORT_ERRORS
			/// <summary>
			/// (optional) An ErrorProvider to receive errors detected by this parsing function.
			/// </summary>
			public ErrorTagging.ErrorProvider ErrorProvider { get; set; }
#endif
		}

		/// <summary>
		/// Parses a data type from a string.
		/// </summary>
		/// <param name="args">Contains arguments that control the parsing.</param>
		/// <returns>A data type object, if a data type could be parsed; otherwise null.</returns>
		public static DataType Parse(ParseArgs args)
		{
			var code = args.Code;

			var startPos = code.Position;
			if (!code.ReadWord()) return null;
			var startWord = code.TokenText;

			if ((args.Flags & ParseFlag.FromRepo) != 0 && code.TokenText == "__SYSTEM__")
			{
				if (!code.ReadWord())
				{
					code.Position = startPos;
					return null;
				}
				startWord = code.TokenText;
			}

			DataType dataType = null;

			switch (code.TokenText)
			{
				case "void":
					dataType = DataType.Void;
					break;

				case "numeric":
				case "decimal":
					dataType = ProcessNumeric(args, code.TokenText);
					break;

				case "unsigned":
				case "signed":
					dataType = ProcessSignedUnsigned(args, code.TokenText);
					break;

				case "int":
				case "short":
				case "long":
				case "ulong":
				case "number":
				case "unumber":
					dataType = ProcessInt(args, code.TokenText);
					break;

				case "char":
				case "character":
				case "varchar":
				case "CHAR":
					dataType = ProcessChar(args, code.TokenText);
					break;

				case "string":
					dataType = ProcessString(args, code.TokenText);
					break;

				case "date":
					dataType = ProcessDate(args, code.TokenText);
					break;

				case "time":
					dataType = ProcessTime(args, code.TokenText);
					break;

				case "enum":
					dataType = ProcessEnum(args);
					break;

				case "like":
					dataType = ProcessLike(args);
					break;

				case "table":
					dataType = DataType.Table;
					break;

				case "indrel":
					dataType = DataType.IndRel;
					break;

				case "command":
					dataType = DataType.Command;
					break;

				case "Section":
					dataType = ProcessSection(args);
					break;

				case "scroll":
					dataType = ProcessScroll(args);
					break;

				case "graphic":
					dataType = ProcessGraphic(args);
					break;

				case "interface":
					dataType = ProcessInterface(args);
					break;

				case "variant":
					dataType = DataType.Variant;
					break;

				case "oleobject":
					dataType = DataType.OleObject;
					break;

				case "Boolean_t":
					dataType = DataType.Boolean_t;
					break;

				default:
					if (args.DataTypeCallback != null)
					{
						var def = args.DataTypeCallback(code.TokenText);
						if (def != null) dataType = def.DataType;
					}
					break;
			}

			if (dataType != null)
			{
				if ((args.Flags & ParseFlag.FromRepo) != 0) IgnoreRepoWords(code, startWord);

				if (!string.IsNullOrEmpty(args.TypeName))
				{
					dataType = dataType.CloneWithNewName(args.TypeName);
				}
			}
			else
			{
				code.Position = code.TokenStartPostion;
			}

			return dataType;
		}

		private static DataType ProcessNumeric(ParseArgs args, string tokenText)
		{
			var sb = new StringBuilder();
			sb.Append(tokenText);

			var code = args.Code;

			if (code.ReadExact('('))
			{
				sb.Append('(');
				if (code.ReadNumber())
				{
					sb.Append(code.TokenText);
					if (code.ReadExact(','))
					{
						sb.Append(',');
						if (code.ReadNumber())
						{
							sb.Append(code.TokenText);
						}
					}
				}
				if (code.ReadExact(')')) sb.Append(')');
				else return new DataType(ValType.Numeric, sb.ToString());
			}

			var done = false;
			var gotMask = false;
			while (!done && !code.EndOfFile)
			{
				if (ReadAttribute(code, sb, "unsigned", "currency", "local_currency")) { }
				else if (!gotMask && code.ReadStringLiteral())
				{
					sb.Append(' ');
					sb.Append(code.TokenText);
					gotMask = true;
				}
				else break;
			}

			return new DataType(ValType.Numeric, sb.ToString());
		}

		private static DataType ProcessSignedUnsigned(ParseArgs args, string tokenText)
		{
			var sb = new StringBuilder();
			sb.Append(tokenText);

			var code = args.Code;

			if (code.ReadNumber())
			{
				// width
				sb.Append(' ');
				sb.Append(code.TokenType);
			}
			else if (code.ReadExact('('))
			{
				sb.Append('(');
				if (code.ReadNumber())
				{
					sb.Append(code.TokenText);
					if (code.ReadExact(','))
					{
						sb.Append(',');
						if (code.ReadNumber())
						{
							sb.Append(code.TokenText);
						}
					}
				}
				if (code.ReadExact(')')) sb.Append(')');
				else return new DataType(ValType.Numeric, sb.ToString());
			}

			if (code.ReadExact("int")) sb.Append(" int");
			else if (code.ReadExact("short")) sb.Append(" short");
			else if (code.ReadExact("long")) sb.Append(" long");
			else if (code.ReadExact("char")) sb.Append(" char");

			var gotMask = false;
			while (!code.EndOfFile)
			{
				if (ReadAttribute(code, sb)) { }
				else if (!gotMask && code.ReadStringLiteral())
				{
					gotMask = true;
					sb.Append(' ');
					sb.Append(code.TokenText);
				}
				else break;
			}

			return new DataType(ValType.Numeric, sb.ToString());
		}

		private static DataType ProcessInt(ParseArgs args, string tokenText)
		{
			var sb = new StringBuilder();
			sb.Append(tokenText);

			var code = args.Code;

			if (code.ReadExact("unsigned")) sb.Append(" unsigned");
			else if (code.ReadExact("signed")) sb.Append(" signed");

			if (code.ReadNumber())
			{
				// width
				sb.Append(' ');
				sb.Append(code.TokenText);
			}

			while (!code.EndOfFile)
			{
				if (!ReadAttribute(code, sb)) break;
			}

			return new DataType(ValType.Numeric, sb.ToString());
		}

		private static DataType ProcessChar(ParseArgs args, string tokenText)
		{
			var code = args.Code;
			if (!code.ReadExact('(')) return DataType.Char;
			if (code.ReadNumber())
			{
				var sb = new StringBuilder();
				sb.Append(tokenText);
				sb.Append('(');
				sb.Append(code.TokenText);
				if (code.ReadExact(')')) sb.Append(')');
				else return new DataType(ValType.String, sb.ToString());

				var done = false;
				var gotMask = false;
				while (!done && !code.EndOfFile)
				{
					if (ReadAttribute(code, sb)) { }
					else if (!gotMask && code.ReadStringLiteral())
					{
						gotMask = true;
						sb.Append(' ');
						sb.Append(code.TokenText);
					}
					else break;
				}

				return new DataType(ValType.String, sb.ToString());
			}
			else
			{
				return new DataType(ValType.String, tokenText);
			}
		}

		private static DataType ProcessString(ParseArgs args, string tokenText)
		{
			var sb = new StringBuilder();
			sb.Append(tokenText);

			var code = args.Code;

			if (code.ReadExact("varying"))
			{
				sb.Append(" varying");
			}
			else if (code.ReadNumber())
			{
				sb.Append(' ');
				sb.Append(code.TokenText);
			}

			while (!code.EndOfFile)
			{
				if (ReadAttribute(code, sb)) { }
				else if (code.ReadStringLiteral())
				{
					sb.Append(' ');
					sb.Append(code.TokenText);
				}
				else break;
			}

			return new DataType(ValType.String, sb.ToString());
		}

		private static DataType ProcessDate(ParseArgs args, string tokenText)
		{
			var sb = new StringBuilder();
			sb.Append(tokenText);

			var code = args.Code;

			if (code.ReadNumber())
			{
				// width
				sb.Append(' ');
				sb.Append(code.TokenText);
			}

			var gotMask = false;
			var done = false;
			while (!done && !code.EndOfFile)
			{
				if (ReadAttribute(code, sb, "shortform", "longform", "alternate")) { }
				else if (!gotMask && code.ReadStringLiteral())
				{
					sb.Append(' ');
					sb.Append(code.TokenText);
				}
				else break;
			}

			return new DataType(ValType.Date, sb.ToString());
		}

		private static DataType ProcessTime(ParseArgs args, string tokenText)
		{
			var sb = new StringBuilder();
			sb.Append(tokenText);

			var code = args.Code;

			var gotMask = false;
			while (!code.EndOfFile)
			{
				if (ReadAttribute(code, sb)) { }
				else if (!gotMask && code.ReadNumber())
				{
					gotMask = true;
					sb.Append(' ');
					sb.Append(code.TokenText);
				}
				else break;
			}

			return new DataType(ValType.Time, sb.ToString());
		}

		private static DataType ProcessEnum(ParseArgs args)
		{
			var options = new List<Definition>();
			var sb = new StringBuilder();
			sb.Append("enum");

			var code = args.Code;

			// Read tokens before the option list
			var gotWidth = false;
			while (!code.EndOfFile)
			{
				if (code.ReadExact('{')) break;
				else if (ReadAttribute(code, sb, "alterable", "required", "nowarn", "numeric")) { }
				else if (!gotWidth && code.ReadNumber())
				{
					sb.Append(' ');
					sb.Append(code.TokenText);
					gotWidth = true;
				}
				else return new DataType(ValType.Enum, sb.ToString());
			}

			// Read the option list
			if ((args.Flags & ParseFlag.FromRepo) != 0)
			{
				// The WBDK repository doesn't put quotes around the strings that need them.
				code.SkipWhiteSpaceAndCommentsIfAllowed();
				var optionStartPos = code.Position;
				var gotComma = false;

				while (!code.EndOfFile)
				{
					if (!code.Read()) break;

					if (code.TokenText == "}")
					{
						if (gotComma)
						{
							var str = code.GetText(optionStartPos, code.TokenStartPostion - optionStartPos).Trim();
							options.Add(new EnumOptionDefinition(DecorateEnumOptionIfRequired(str)));
							optionStartPos = code.Position;
						}
						break;
					}

					if (code.TokenText == ",")
					{
						var str = code.GetText(optionStartPos, code.TokenStartPostion - optionStartPos).Trim();
						options.Add(new EnumOptionDefinition(DecorateEnumOptionIfRequired(str)));
						optionStartPos = code.Position;
						gotComma = true;
					}
				}
			}
			else if ((args.Flags & ParseFlag.Strict) != 0)
			{
				var expectingComma = false;
				while (!code.EndOfFile)
				{
					if (!code.Read())
					{
#if REPORT_ERRORS
						if (args.ErrorProvider != null) args.ErrorProvider.ReportError(code, code.TokenSpan, ErrorTagging.ErrorCode.Enum_UnexpectedEndOfFile);
#endif
						break;
					}

					if (code.TokenType == TokenParser.TokenType.Operator)
					{
						if (code.TokenText == "}") break;
						if (code.TokenText == ",")
						{
							if (!expectingComma)
							{
#if REPORT_ERRORS
								if (args.ErrorProvider != null) args.ErrorProvider.ReportError(code, code.TokenSpan, ErrorTagging.ErrorCode.Enum_UnexpectedComma);
#endif
							}
							else
							{
								expectingComma = false;
							}
						}
					}
					else if (code.TokenType == TokenParser.TokenType.StringLiteral || code.TokenType == TokenParser.TokenType.Word)
					{
						var str = code.TokenText;
						if (expectingComma)
						{
#if REPORT_ERRORS
							if (args.ErrorProvider != null) args.ErrorProvider.ReportError(code, code.TokenSpan, ErrorTagging.ErrorCode.Enum_NoComma);
#endif
						}
						else if (options.Any(x => x.Name == str))
						{
#if REPORT_ERRORS
							if (args.ErrorProvider != null) args.ErrorProvider.ReportError(code, code.TokenSpan, ErrorTagging.ErrorCode.Enum_DuplicateOption, str);
#endif
						}
						else
						{
							options.Add(new EnumOptionDefinition(str));
						}
					}
				}
			}
			else
			{
				while (!code.EndOfFile)
				{
					if (!code.Read()) break;
					if (code.TokenText == "}") break;
					if (code.TokenText == ",") continue;
					switch (code.TokenType)
					{
						case TokenParser.TokenType.Word:
						case TokenParser.TokenType.StringLiteral:
							options.Add(new EnumOptionDefinition(code.TokenText));
							break;
					}
				}
			}

			sb.Append(" {");
			var first = true;
			foreach (var option in options)
			{
				if (first)
				{
					first = false;
					sb.Append(' ');
				}
				else
				{
					sb.Append(", ");
				}
				sb.Append(option.Name);
			}
			sb.Append(" }");

			return new DataType(ValType.Enum, sb.ToString())
			{
				_completionOptions = options.ToArray(),
				_completionOptionsType = CompletionOptionsType.EnumOptionsList
			};
		}

		private static DataType ProcessLike(ParseArgs args)
		{
			var code = args.Code;
			if (code.ReadWord())
			{
				var word1 = code.TokenText;
				if (code.ReadExact('.'))
				{
					if (code.ReadWord())
					{
						var word2 = code.TokenText;

						// TODO: this should be able to handle more than just tables
						var table = ProbeEnvironment.GetTable(word1);
						if (table != null)
						{
							var field = table.GetField(word2);
							if (field != null) return field.DataType;
						}

						return new DataType(ValType.Unknown, string.Concat("like ", word1, ".", word2));
					}
					else
					{
						return new DataType(ValType.Unknown, string.Concat("like ", word1, "."));
					}
				}

				if (args.VariableCallback != null)
				{
					var def = args.VariableCallback(word1);
					if (def != null) return def.DataType;
				}

				return new DataType(ValType.Unknown, string.Concat("like ", word1));
			}
			else return null;
		}

		private static DataType ProcessSection(ParseArgs args)
		{
			var sb = new StringBuilder();
			sb.Append("Section");

			var code = args.Code;

			if (code.ReadExact("Level"))
			{
				sb.Append(" Level");
				if (code.ReadNumber())
				{
					sb.Append(' ');
					sb.Append(code.TokenText);
				}
			}

			return new DataType(ValType.Section, sb.ToString());
		}

		private static DataType ProcessScroll(ParseArgs args)
		{
			var sb = new StringBuilder();
			sb.Append("scroll");

			if (args.Code.ReadNumber())
			{
				sb.Append(' ');
				sb.Append(args.Code.TokenText);
			}

			return new DataType(ValType.Scroll, sb.ToString());
		}

		private static DataType ProcessGraphic(ParseArgs args)
		{
			var sb = new StringBuilder();
			sb.Append("graphic");

			if (args.Code.ReadNumber())	// rows
			{
				sb.Append(' ');
				sb.Append(args.Code.TokenText);

				if (args.Code.ReadNumber())	// columns
				{
					sb.Append(' ');
					sb.Append(args.Code.TokenText);

					if (args.Code.ReadNumber())	// bytes
					{
						sb.Append(' ');
						sb.Append(args.Code.TokenText);
					}
				}
			}

			return new DataType(ValType.Graphic, sb.ToString());
		}

		private static DataType ProcessInterface(ParseArgs args)
		{
			var sb = new StringBuilder();
			sb.Append("interface");

			var code = args.Code;

			if ((args.Flags & ParseFlag.InterfaceType) == 0)
			{
				if (code.ReadWord())
				{
					sb.Append(' ');
					sb.Append(code.TokenText);

					var completionOptions = new List<Definition>();
					Definition[] methods = null;
					Definition[] properties = null;

					var intType = ProbeEnvironment.GetInterfaceType(code.TokenText);
					if (intType != null)
					{
						methods = intType.MethodDefinitions.ToArray();
						completionOptions.AddRange(methods);
						properties = intType.PropertyDefinitions.ToArray();
						completionOptions.AddRange(properties);
					}

					return new DataType(ValType.Interface, sb.ToString())
					{
						_completionOptions = completionOptions.ToArray(),
						_completionOptionsType = CompletionOptionsType.InterfaceMembers,
						_methods = methods,
						_properties = properties
					};
				}
			}
			else
			{
				// Parsing the text from a WBDK interface, which could contain .NET or COM specific formatting.

				if (code.ReadWord())
				{
					sb.Append(' ');
					sb.Append(code.TokenText);

					while (code.ReadExact('.'))
					{
						sb.Append('.');
						if (code.ReadWord()) sb.Append(code.TokenText);
						else break;
					}

					if (code.ReadExact('*'))
					{
						sb.Append('*');
					}
					else if (code.ReadExact('&'))
					{
						sb.Append('&');
					}
					else if (code.ReadExact('['))
					{
						sb.Append('[');
						if (code.ReadExact(']')) sb.Append(']');
					}
				}
			}

			return new DataType(ValType.Interface, sb.ToString());
		}

		private static bool ReadAttribute(TokenParser.Parser code, StringBuilder sb, params string[] extraTokens)
		{
			var startPos = code.Position;
			if (code.ReadWord())
			{
				var word = code.TokenText;
				switch (code.TokenText)
				{
					case "ALLCAPS":
					case "AUTOCAPS":
					case "LEADINGZEROS":
					case "NOCHANGE":
					case "NODISPLAY":
					case "NOECHO":
					case "NOINPUT":
					case "NOPICK":
					case "NOUSE":
					case "REQUIRED":
					case "PROBE":
						sb.Append(' ');
						sb.Append(word);
						return true;

					case "tag":
						sb.Append(" tag");
						if (code.ReadWord())
						{
							sb.Append(' ');
							sb.Append(code.TokenText);
							if (code.ReadStringLiteral() || code.ReadWord())
							{
								sb.Append(' ');
								sb.Append(code.TokenText);
							}
						}
						return true;

					default:
						if (word.StartsWith("INTENSITY_") || extraTokens.Contains(word))
						{
							sb.Append(' ');
							sb.Append(word);
							return true;
						}
						else
						{
							code.Position = startPos;
							return false;
						}
				}
			}
			else if (code.ReadExact("@neutral"))
			{
				sb.Append(" @neutral");
				return true;
			}

			return false;
		}

		private static readonly Regex _rxRepoWords = new Regex(@"\G((nomask|(NO)?PROBE|[%@]undefined|@neutral|(NO)?PICK)\b|&)");

		private static void IgnoreRepoWords(TokenParser.Parser code, string type)
		{
			while (!code.EndOfFile)
			{
				code.SkipWhiteSpaceAndCommentsIfAllowed();

				if (code.ReadPattern(_rxRepoWords)) continue;

				if (type == "enum" && code.ReadStringLiteral()) continue;

				break;
			}
		}

		public void DumpTree(System.Xml.XmlWriter xml)
		{
			xml.WriteStartElement("dataType");
			xml.WriteAttributeString("name", _name);
			if (_completionOptions != null && _completionOptions.Length > 0)
			{
				var sb = new StringBuilder();
				foreach (var opt in _completionOptions)
				{
					if (sb.Length > 0) sb.Append(" ");
					sb.Append(opt);
				}
				xml.WriteAttributeString("completionOptions", sb.ToString());
			}
			xml.WriteEndElement();
		}

		public string InfoText
		{
			get { return _infoText; }
		}

		public System.Windows.UIElement QuickInfoWpf
		{
			get
			{
				if (!string.IsNullOrEmpty(_infoText) && _infoText != _name)
				{
					return Definition.WpfDivs(
						Definition.WpfMainLine(_name),
						Definition.WpfInfoLine(_infoText));
				}
				else
				{
					return Definition.WpfMainLine(_name);
				}
			}
		}

		public static string DecorateEnumOptionIfRequired(string option)
		{
			if (string.IsNullOrEmpty(option)) return "\" \"";

			if (option.StartsWith("\"") && option.EndsWith("\"")) return option;

			if (!option.IsWord())
			{
				return string.Concat("\"", option, "\"");
			}

			return option;
		}

		public bool HasMethodsOrProperties
		{
			get { return _methods != null || _properties != null; }
		}

		public IEnumerable<Definition> GetMethods(string name)
		{
			if (_methods != null)
			{
				foreach (var method in _methods)
				{
					if (method.Name == name) yield return method;
				}
			}
		}

		public IEnumerable<Definition> GetProperties(string name)
		{
			if (_properties != null)
			{
				foreach (var prop in _properties)
				{
					if (prop.Name == name) yield return prop;
				}
			}
		}

		public IEnumerable<Definition> MethodsAndProperties
		{
			get
			{
				if (_methods != null)
				{
					foreach (var meth in _methods) yield return meth;
				}

				if (_properties != null)
				{
					foreach (var prop in _properties) yield return prop;
				}
			}
		}

#if DEBUG
		public static void CheckDataTypeParsing(string dataTypeText, TokenParser.Parser usedParser, DataType dataType)
		{
			if (dataType == null)
			{
				Log.WriteDebug("WARNING: DataType.Parse was unable to parse [{0}]", dataTypeText);
			}
			else if (usedParser.Read())
			{
				Log.WriteDebug("WARNING: DataType.Parse stopped before end of text [{0}] got [{1}]", dataTypeText, dataType.Name);
			}
		}
#endif

		public bool HasEnumOptions
		{
			get { return _completionOptionsType == CompletionOptionsType.EnumOptionsList; }
		}

		public bool IsValidEnumOption(string optionText)
		{
			if (_completionOptionsType != CompletionOptionsType.EnumOptionsList) return false;
			if (_completionOptions == null) return false;

			foreach (var opt in _completionOptions)
			{
				if (opt.Name == optionText) return true;
			}

			return false;
		}

		public ValType ValueType
		{
			get { return _valueType; }
		}
	}
}

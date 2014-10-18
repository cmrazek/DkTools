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

		private enum CompletionOptionsType
		{
			None,
			List,
			Tables,
			RelInds
		}

		public static readonly DataType Boolean_t = new DataType("Boolean_t")
		{
			_completionOptionsType = CompletionOptionsType.List,
			_completionOptions = new Definition[]
			{
				new EnumOptionDefinition("TRUE"),
				new EnumOptionDefinition("FALSE")
			}
		};
		public static readonly DataType Char = new DataType("char");
		public static readonly DataType Char255 = new DataType("char(255)");
		public static readonly DataType Command = new DataType("command");
		public static readonly DataType Date = new DataType("date");
		public static readonly DataType Enum = new DataType("enum");
		public static readonly DataType IndRel = new DataType("indrel") { _completionOptionsType = CompletionOptionsType.RelInds };
		public static readonly DataType Int = new DataType("int");
		public static readonly DataType Numeric = new DataType("numeric");
		public static readonly DataType OleObject = new DataType("oleobject");
		public static readonly DataType String = new DataType("string");
		public static readonly DataType StringVarying = new DataType("string varying");
		public static readonly DataType Table = new DataType("table") { _completionOptionsType = CompletionOptionsType.Tables };
		public static readonly DataType Ulong = new DataType("ulong");
		public static readonly DataType Unsigned = new DataType("unsigned");
		public static readonly DataType Variant = new DataType("variant");
		public static readonly DataType Void = new DataType("void");

		public delegate DataTypeDefinition GetDataTypeDelegate(string name);
		public delegate VariableDefinition GetVariableDelegate(string name);

		/// <summary>
		/// Creates a new data type object.
		/// </summary>
		/// <param name="name">The name or visible text of the data type. This will also be used as the info text.</param>
		public DataType(string name)
		{
			_name = name;
			_infoText = name;
		}

		/// <summary>
		/// Creates a new data type object.
		/// </summary>
		/// <param name="name">(required) name of the data type</param>
		/// <param name="infoText">(required) Help text to be displayed to the user.</param>
		/// <param name="sourceToken">(optional) The token that created the data type.</param>
		public DataType(string name, string infoText)
		{
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
		public DataType(string name, IEnumerable<Definition> completionOptions, string infoText)
		{
			_name = name;
			_infoText = infoText;

			if (completionOptions != null)
			{
				_completionOptions = completionOptions.ToArray();
				if (_completionOptions.Length > 0) _completionOptionsType = CompletionOptionsType.List;
			}
		}

		public static DataType FromString(string str)
		{
			return new DataType(str, str);
		}

		public DataType CloneWithNewName(string newName)
		{
			return new DataType(newName, _infoText)
			{
				_completionOptions = _completionOptions,
				_completionOptionsType = _completionOptionsType
			};
		}

		public string Name
		{
			get { return _name; }
			set { _name = value; }
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
					case CompletionOptionsType.List:
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
			FromRepo
		}

		/// <summary>
		/// Parses a data type from a string.
		/// </summary>
		/// <param name="code">The token parser to read from.</param>
		/// <param name="typeName">(optional) A name to be given to the data type. If null or blank, the actual text will be used as the name.</param>
		/// <param name="dataTypeCallback">(optional) A callback function used to look up existing data types.</param>
		/// <param name="varCallback">(optional) A callback function used to look up existing variables.</param>
		/// <param name="flags">(optional) Flags to control the parsing behaviour.</param>
		/// <param name="errorProv">(optional) An ErrorProvider to receive errors detected by this parsing function.</param>
		/// <returns>A data type object, if a data type could be parsed; otherwise null.</returns>
		public static DataType Parse(TokenParser.Parser code, string typeName = null, GetDataTypeDelegate dataTypeCallback = null,
			GetVariableDelegate varCallback = null, ParseFlag flags = 0
#if REPORT_ERRORS
			, ErrorTagging.ErrorProvider errorProv = null
#endif
			)
		{
			var startPos = code.Position;
			if (!code.ReadWord()) return null;
			var startWord = code.TokenText;

			if ((flags & ParseFlag.FromRepo) != 0 && code.TokenText == "__SYSTEM__")
			{
				if (!code.ReadWord())
				{
					code.Position = startPos;
					return null;
				}
				startWord = code.TokenText;
			}

			switch (code.TokenText)
			{
				#region void
				case "void":
					if ((flags & ParseFlag.FromRepo) != 0) IgnoreRepoWords(code, startWord);
					return DataType.Void;
				#endregion

				#region numeric
				case "numeric":
					{
						var sb = new StringBuilder();
						sb.Append("numeric");

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
							else return new DataType(typeName, sb.ToString());
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

						if ((flags & ParseFlag.FromRepo) != 0) IgnoreRepoWords(code, startWord);

						return new DataType(typeName, sb.ToString());
					}
				#endregion

				#region unsigned
				case "unsigned":
					{
						var sb = new StringBuilder();
						sb.Append("unsigned");

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
							else return new DataType(typeName, sb.ToString());
						}

						if (code.ReadExact("int")) sb.Append(" int");
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

						if ((flags & ParseFlag.FromRepo) != 0) IgnoreRepoWords(code, startWord);

						return new DataType(typeName, sb.ToString());
					}
				#endregion

				#region int, short
				case "int":
				case "short":
					{
						var sb = new StringBuilder();
						sb.Append(code.TokenText);

						if (code.ReadExact("unsigned")) sb.Append(" unsigned");

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

						if ((flags & ParseFlag.FromRepo) != 0) IgnoreRepoWords(code, startWord);

						return new DataType(typeName, sb.ToString());
					}
				#endregion

				#region long
				case "long":
					{
						var sb = new StringBuilder();
						sb.Append("long");

						if (code.ReadExact("unsigned")) sb.Append(" unsigned");

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

						if ((flags & ParseFlag.FromRepo) != 0) IgnoreRepoWords(code, startWord);

						return new DataType(typeName, sb.ToString());
					}
				#endregion

				#region ulong
				case "ulong":
					if ((flags & ParseFlag.FromRepo) != 0) IgnoreRepoWords(code, startWord);
					return DataType.Ulong;
				#endregion

				#region char
				case "char":
					if (!code.ReadExact('(')) return DataType.Char;
					if (code.ReadNumber())
					{
						var sb = new StringBuilder();
						sb.Append("char(");
						sb.Append(code.TokenText);
						if (code.ReadExact(')')) sb.Append(')');
						else return new DataType(typeName, sb.ToString());

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

						if ((flags & ParseFlag.FromRepo) != 0) IgnoreRepoWords(code, startWord);

						return new DataType(typeName, sb.ToString());
					}
					else
					{
						if ((flags & ParseFlag.FromRepo) != 0) IgnoreRepoWords(code, startWord);

						return new DataType(typeName, "char");
					}
				#endregion

				#region string
				case "string":
					{
						var sb = new StringBuilder();
						sb.Append("string");
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

						if ((flags & ParseFlag.FromRepo) != 0) IgnoreRepoWords(code, startWord);

						return new DataType(typeName, sb.ToString());
					}
				#endregion

				#region date
				case "date":
					{
						var sb = new StringBuilder();
						sb.Append("date");

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

						if ((flags & ParseFlag.FromRepo) != 0) IgnoreRepoWords(code, startWord);

						return new DataType(typeName, sb.ToString());
					}
				#endregion

				#region time
				case "time":
					{
						var sb = new StringBuilder();
						sb.Append("time");

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

						if ((flags & ParseFlag.FromRepo) != 0) IgnoreRepoWords(code, startWord);

						return new DataType(typeName, sb.ToString());
					}
				#endregion

				#region enum
				case "enum":
					{
						var options = new List<Definition>();
						var sb = new StringBuilder();
						sb.Append("enum");

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
							else return new DataType(typeName, sb.ToString());
						}

						// Read the option list
						if ((flags & ParseFlag.FromRepo) != 0)
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
						else if ((flags & ParseFlag.Strict) != 0)
						{
							var expectingComma = false;
							while (!code.EndOfFile)
							{
								if (!code.Read())
								{
#if REPORT_ERRORS
									if (errorProv != null) errorProv.ReportError(code, code.TokenSpan, ErrorTagging.ErrorCode.Enum_UnexpectedEndOfFile);
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
											if (errorProv != null) errorProv.ReportError(code, code.TokenSpan, ErrorTagging.ErrorCode.Enum_UnexpectedComma);
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
										if (errorProv != null) errorProv.ReportError(code, code.TokenSpan, ErrorTagging.ErrorCode.Enum_NoComma);
#endif
									}
									else if (options.Any(x => x.Name == str))
									{
#if REPORT_ERRORS
										if (errorProv != null) errorProv.ReportError(code, code.TokenSpan, ErrorTagging.ErrorCode.Enum_DuplicateOption, str);
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

						if ((flags & ParseFlag.FromRepo) != 0) IgnoreRepoWords(code, startWord);

						return new DataType(typeName, sb.ToString())
						{
							_completionOptions = options.ToArray(),
							_completionOptionsType = CompletionOptionsType.List
						};
					}
				#endregion

				#region like
				case "like":
					if (code.ReadWord())
					{
						var word1 = code.TokenText;
						if (code.ReadExact('.'))
						{
							if (code.ReadWord())
							{
								var word2 = code.TokenText;

								var table = ProbeEnvironment.GetTable(word1);
								if (table != null)
								{
									var field = table.GetField(word2);
									if (field != null) return !string.IsNullOrEmpty(typeName) ? field.DataType.CloneWithNewName(typeName) : field.DataType;
								}

								return new DataType(typeName, string.Concat("like ", word1, ".", word2));
							}
							else
							{
								return new DataType(typeName, string.Concat("like ", word1, "."));
							}
						}

						if (varCallback != null)
						{
							var def = varCallback(word1);
							if (def != null) return !string.IsNullOrEmpty(typeName) ? def.DataType.CloneWithNewName(typeName) : def.DataType;
						}

						if ((flags & ParseFlag.FromRepo) != 0) IgnoreRepoWords(code, startWord);

						return new DataType(typeName, string.Concat("like ", word1));
					}
					else return null;
				#endregion

				#region table/indrel
				case "table":
					if ((flags & ParseFlag.FromRepo) != 0) IgnoreRepoWords(code, startWord);
					return DataType.Table;

				case "indrel":
					if ((flags & ParseFlag.FromRepo) != 0) IgnoreRepoWords(code, startWord);
					return DataType.IndRel;
				#endregion

				#region command
				case "command":
					if ((flags & ParseFlag.FromRepo) != 0) IgnoreRepoWords(code, startWord);
					return DataType.Command;
				#endregion

				#region section
				case "Section":
					{
						var sb = new StringBuilder();
						sb.Append("Section");
						if (code.ReadExact("Level"))
						{
							sb.Append(" Level");
							if (code.ReadNumber())
							{
								sb.Append(' ');
								sb.Append(code.TokenText);
							}
						}

						if ((flags & ParseFlag.FromRepo) != 0) IgnoreRepoWords(code, startWord);

						return new DataType(typeName, sb.ToString());
					}
				#endregion

				#region scroll
				case "scroll":
					{
						var sb = new StringBuilder();
						sb.Append("scroll");

						if (code.ReadNumber())
						{
							sb.Append(' ');
							sb.Append(code.TokenText);
						}

						if ((flags & ParseFlag.FromRepo) != 0) IgnoreRepoWords(code, startWord);

						return new DataType(typeName, sb.ToString());
					}
				#endregion

				#region graphic
				case "graphic":
					{
						var sb = new StringBuilder();
						sb.Append("graphic");

						if (code.ReadNumber())	// rows
						{
							sb.Append(' ');
							sb.Append(code.TokenText);

							if (code.ReadNumber())	// columns
							{
								sb.Append(' ');
								sb.Append(code.TokenText);

								if (code.ReadNumber())	// bytes
								{
									sb.Append(' ');
									sb.Append(code.TokenText);
								}
							}
						}

						if ((flags & ParseFlag.FromRepo) != 0) IgnoreRepoWords(code, startWord);

						return new DataType(typeName, sb.ToString());
					}
				#endregion

				#region interface
				case "interface":
					{
						var sb = new StringBuilder();
						sb.Append("interface");

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

							if ((flags & ParseFlag.FromRepo) != 0) IgnoreRepoWords(code, startWord);

							return new DataType(typeName, sb.ToString())
							{
								_completionOptions = completionOptions.ToArray(),
								_completionOptionsType = CompletionOptionsType.List,
								_methods = methods,
								_properties = properties
							};
						}

						return new DataType(typeName, sb.ToString());
					}
				#endregion

				#region variant
				case "variant":
					if ((flags & ParseFlag.FromRepo) != 0) IgnoreRepoWords(code, startWord);
					return DataType.Variant;
				#endregion

				#region oleobject
				case "oleobject":
					if ((flags & ParseFlag.FromRepo) != 0) IgnoreRepoWords(code, startWord);
					return DataType.OleObject;
				#endregion

				#region Boolean_t
				case "Boolean_t":
					if ((flags & ParseFlag.FromRepo) != 0) IgnoreRepoWords(code, startWord);
					return DataType.Boolean_t;
				#endregion

				default:
					if (dataTypeCallback != null)
					{
						var def = dataTypeCallback(code.TokenText);
						if (def != null) return !string.IsNullOrEmpty(typeName) ? def.DataType.CloneWithNewName(typeName) : def.DataType;
					}
					code.Position = code.TokenStartPostion;
					return null;
			}
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

		public Definition GetMethod(string name)
		{
			if (_methods == null) return null;
			foreach (var method in _methods)
			{
				if (method.Name == name) return method;
			}
			return null;
		}

		public Definition GetProperty(string name)
		{
			if (_properties == null) return null;
			foreach (var prop in _properties)
			{
				if (prop.Name == name) return prop;
			}
			return null;
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
		public static void CheckDataTypeParsing(string dataTypeText)
		{
			var parser = new TokenParser.Parser(dataTypeText);
			var dataType = DataType.Parse(parser, flags: DataType.ParseFlag.FromRepo);
			if (dataType == null)
			{
				Log.WriteDebug("WARNING: DataType.Parse was unable to parse [{0}]", dataTypeText);
			}
			else if (parser.Read())
			{
				Log.WriteDebug("WARNING: DataType.Parse stopped before end of text [{0}] got [{1}]", dataTypeText, dataType.Name);
			}
		}
#endif
	}
}

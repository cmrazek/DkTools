using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

		private enum CompletionOptionsType
		{
			None,
			List,
			Tables,
			RelInds
		}

		public static readonly DataType Int = new DataType("int");
		public static readonly DataType Void = new DataType("void");
		public static readonly DataType Table = new DataType("table") { _completionOptionsType = CompletionOptionsType.Tables };
		public static readonly DataType IndRel = new DataType("indrel") { _completionOptionsType = CompletionOptionsType.RelInds };
		public static readonly DataType Numeric = new DataType("numeric");
		public static readonly DataType Unsigned = new DataType("unsigned");
		public static readonly DataType Char = new DataType("char");
		public static readonly DataType String = new DataType("string");
		public static readonly DataType StringVarying = new DataType("string varying");
		public static readonly DataType Date = new DataType("date");
		public static readonly DataType Enum = new DataType("enum");

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
				_name = _infoText = name;
			}
			else if (!string.IsNullOrEmpty(infoText))
			{
				_name = _infoText = infoText;
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

		/// <summary>
		/// Returns a data type for a specific token.
		/// </summary>
		/// <param name="token">The token for which the data type is to be retrieved.</param>
		/// <returns>If the token already has an associated data type then, this will be returned; otherwise a new data type will be created with the token's text as the name.</returns>
		public static DataType FromToken(Token token)
		{
			if (token == null) return DataType.Int;

			// Check if this token has a data type built in.
			if (token is IDataTypeToken) return (token as IDataTypeToken).DataType;

			if (token is DataTypeToken) return (token as DataTypeToken).DataType;

			// Try to find a previously defined data type.
			var def = token.GetDefinitions<DataTypeDefinition>(token.Text).FirstOrDefault();
			if (def != null) return def.DataType;

			// Create a new data type using the token text as the definition.
			var dataType = new DataType(token.Text, token.Text);
			
			return dataType;
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
						return _completionOptions != null ? _completionOptions : new Definition[0];
					case CompletionOptionsType.Tables:
						return (from t in ProbeEnvironment.Tables select t.Definition);
					case CompletionOptionsType.RelInds:
						return (from r in ProbeEnvironment.RelInds select r.Definition);
					default:
						return new Definition[0];
				}
			}
		}

		/// <summary>
		/// Parses a data type from a string.
		/// </summary>
		/// <param name="code">The token parser to read from.</param>
		/// <returns>A data type object, if a data type could be parsed; otherwise null.</returns>
		public static DataType Parse(TokenParser.Parser code, string typeName, CodeModel model, GroupToken parent)
		{
			var startPos = code.Position;
			if (!code.ReadWord()) return null;

			if (code.TokenText == "__SYSTEM__")
			{
				if (!code.ReadWord())
				{
					code.Position = startPos;
					return null;
				}
			}

			switch (code.TokenText)
			{
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

						return new DataType(typeName, sb.ToString());
					}
				#endregion

				#region int
				case "int":
					{
						var sb = new StringBuilder();
						sb.Append("int");

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

						return new DataType(typeName, sb.ToString());
					}
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

						return new DataType(typeName, sb.ToString());
					}
					else return new DataType(typeName, "char");
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
						var optionSB = new StringBuilder();
						while (!code.EndOfFile)
						{
							if (code.ReadExact('}'))
							{
								if (optionSB.Length > 0) options.Add(new EnumOptionDefinition(optionSB.ToString()));
								optionSB.Clear();
								break;
							}
							else if (code.ReadExact(','))
							{
								if (optionSB.Length > 0) options.Add(new EnumOptionDefinition(optionSB.ToString()));
								optionSB.Clear();
							}
							else if (code.ReadStringLiteral())
							{
								options.Add(new EnumOptionDefinition(code.TokenText));
								optionSB.Clear();
							}
							else if (code.Read())
							{
								optionSB.Append(code.TokenText);
							}
							else break;
						}

						if (optionSB.Length > 0) options.Add(new EnumOptionDefinition(optionSB.ToString()));

						sb.Append(" {");
						var first = true;
						foreach (var opt in options)
						{
							if (first) first = false;
							else sb.Append(',');
							sb.Append(' ');
							sb.Append(opt.Name);
						}
						sb.Append(" }");

						if (code.ReadExact("PICK")) sb.Append(" PICK");

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
						else
						{
							if (parent != null)
							{
								var def = parent.GetDefinitions<VariableDefinition>().FirstOrDefault();
								if (def != null) return !string.IsNullOrEmpty(typeName) ? def.DataType.CloneWithNewName(typeName) : def.DataType;
							}
							return new DataType(typeName, string.Concat("like ", word1));
						}
					}
					else return null;
				#endregion

				#region table/indrel
				case "table":
					return DataType.Table;

				case "indrel":
					return DataType.IndRel;
				#endregion

				default:
					if (parent != null)
					{
						var def = parent.GetDefinitions<DataTypeDefinition>(code.TokenText).FirstOrDefault();
						if (def != null) return !string.IsNullOrEmpty(typeName) ? def.DataType.CloneWithNewName(typeName) : def.DataType;
					}
					else if (model != null)
					{
						// This could be a name of a pre-defined data type.
						var def = model.GetDefinitions<DataTypeDefinition>(code.TokenText).FirstOrDefault();
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
			get { return string.IsNullOrWhiteSpace(_infoText) ? _name : _infoText; }
		}
	}
}

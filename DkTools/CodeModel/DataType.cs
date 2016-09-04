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
		private string _source;
		private Definition[] _completionOptions;
		private CompletionOptionsType _completionOptionsType;
		private ValType _valueType;
		private DkDict.Interface _intf;

		public enum CompletionOptionsType
		{
			None,
			EnumOptionsList,
			Tables,
			RelInds,
			InterfaceMembers
		}

		public static readonly DataType Boolean_t = new DataType(ValType.Enum, null, "Boolean_t", new string[] { "FALSE", "TRUE" });
		public static readonly DataType Char = new DataType(ValType.Char, null, "char");
		public static readonly DataType Char255 = new DataType(ValType.String, null, "char(255)");
		public static readonly DataType Command = new DataType(ValType.Command, null, "command");
		public static readonly DataType Date = new DataType(ValType.Date, null, "date");
		public static readonly DataType Enum = new DataType(ValType.Enum, null, "enum");
		public static readonly DataType IndRel = new DataType(ValType.IndRel, null, "indrel") { _completionOptionsType = CompletionOptionsType.RelInds };
		public static readonly DataType Int = new DataType(ValType.Numeric, null, "int");
		public static readonly DataType Numeric = new DataType(ValType.Numeric, null, "numeric");
		public static readonly DataType OleObject = new DataType(ValType.Interface, null, "oleobject");
		public static readonly DataType String = new DataType(ValType.String, null, "string");
		public static readonly DataType StringVarying = new DataType(ValType.String, null, "string varying");
		public static readonly DataType Table = new DataType(ValType.Table, null, "table") { _completionOptionsType = CompletionOptionsType.Tables };
		public static readonly DataType Ulong = new DataType(ValType.Numeric, null, "ulong");
		public static readonly DataType Unknown = new DataType(ValType.Unknown, null, string.Empty);
		public static readonly DataType Unsigned = new DataType(ValType.Numeric, null, "unsigned");
		public static readonly DataType Unsigned2 = new DataType(ValType.Numeric, null, "numeric(2) unsigned");
		public static readonly DataType Unsigned9 = new DataType(ValType.Numeric, null, "numeric(9) unsigned");
		public static readonly DataType Variant = new DataType(ValType.Interface, null, "variant");
		public static readonly DataType Void = new DataType(ValType.Void, null, "void");

		public delegate DataTypeDefinition GetDataTypeDelegate(string name);
		public delegate VariableDefinition GetVariableDelegate(string name);
		public delegate void TokenCreateDelegate(Token token);

		/// <summary>
		/// Creates a new data type object.
		/// </summary>
		/// <param name="name">(optional) name of the data type</param>
		/// <param name="source">(required) source code that defines the data type.</param>
		public DataType(ValType valueType, string name, string source)
		{
			if (string.IsNullOrWhiteSpace(source) && valueType != ValType.Unknown) throw new ArgumentNullException("source");

			_valueType = valueType;
			_name = name;
			_source = source;
		}

		/// <summary>
		/// Creates a new data type object.
		/// </summary>
		/// <param name="name">(optional) Name of the data type</param>
		/// <param name="source">(required) Source code that defines the data type</param>
		/// <param name="completionOptions">(required) A list of hardcoded options for this data type.</param>
		/// <param name="optionsType">(required) The type of completion options</param>
		public DataType(ValType valueType, string name, string source, IEnumerable<Definition> completionOptions, CompletionOptionsType optionsType)
		{
			if (string.IsNullOrWhiteSpace(source)) throw new ArgumentNullException("source");
			if (completionOptions == null) throw new ArgumentNullException("completionOptions");

			_valueType = valueType;
			_name = name;
			_source = source;
			_completionOptions = completionOptions.ToArray();
			_completionOptionsType = optionsType;
		}

		/// <summary>
		/// Creates a new data type object.
		/// </summary>
		/// <param name="name">(optional) Name of the data type</param>
		/// <param name="source">(required) Source code that defines the data type</param>
		/// <param name="enumOptions">A list of enum options for this data type.</param>
		public DataType(ValType valueType, string name, string source, IEnumerable<string> enumOptions)
		{
			if (string.IsNullOrWhiteSpace(source)) throw new ArgumentNullException("source");
			if (enumOptions == null) throw new ArgumentNullException("completionOptions");

			_valueType = valueType;
			_name = name;
			_source = source;
			_completionOptions = (from o in enumOptions select new EnumOptionDefinition(o, this)).ToArray();
			_completionOptionsType = CompletionOptionsType.EnumOptionsList;
		}

		public string Name
		{
			get { return _name; }
			set { _name = value; }
		}

		public string Source
		{
			get { return _source; }
			set { _source = value; }
		}

		public override string ToString()
		{
			if (!string.IsNullOrEmpty(_name)) return string.Concat(_name, " (", _source, ")");
			else return _source;
		}

		public DkDict.Interface Interface
		{
			get { return _intf; }
			set { _intf = value; }
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
						foreach (var table in DkDict.Dict.Tables)
						{
							foreach (var def in table.Definitions) yield return def;
						}
						break;

					case CompletionOptionsType.RelInds:
						yield return RelIndDefinition.Physical;
						foreach (var r in DkDict.Dict.RelInds) yield return r.Definition;
						break;

					case CompletionOptionsType.InterfaceMembers:
						if (_intf != null)
						{
							foreach (var def in _intf.MethodDefinitions) yield return def;
							foreach (var def in _intf.PropertyDefinitions) yield return def;
						}
						break;

				}
			}
		}

		public static string[] DataTypeStartingKeywords = new string[] { "char", "date", "enum", "int", "indrel", "like", "numeric", "string", "table", "time", "unsigned", "void" };

		[Flags]
		public enum ParseFlag
		{
			Strict
		}

		public class ParseArgs
		{
			/// <summary>
			/// The token parser to read from.
			/// </summary>
			public CodeParser Code { get; set; }

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

			/// <summary>
			/// (optional) A callback which triggers creation of tokens for use in a code model.
			/// </summary>
			public TokenCreateDelegate TokenCreateCallback { get; set; }

			/// <summary>
			/// (optional) The scope to use when creating tokens.
			/// This is required if CreateTokens is true.
			/// </summary>
			public Scope Scope { get; set; }

			/// <summary>
			/// (optional) Set to true if this is for a visible model which is not preprocessed.
			/// </summary>
			public bool VisibleModel { get; set; }

			/// <summary>
			/// (out) The first token parsed by the data type
			/// </summary>
			public Token FirstToken { get; set; }

#if REPORT_ERRORS
			/// <summary>
			/// (optional) An ErrorProvider to receive errors detected by this parsing function.
			/// </summary>
			public ErrorTagging.ErrorProvider ErrorProvider { get; set; }
#endif

			public void OnKeyword(Span span, string text)
			{
				if (TokenCreateCallback != null)
				{
					var tok = new KeywordToken(Scope, span, text);
					if (FirstToken == null) FirstToken = tok;
					TokenCreateCallback(tok);
				}
			}

			public void OnDataTypeKeyword(Span span, string text, Definition def)
			{
				if (TokenCreateCallback != null)
				{
					var tok = new DataTypeKeywordToken(Scope, span, text, def);
					if (FirstToken == null) FirstToken = tok;
					TokenCreateCallback(tok);
				}
			}

			public void OnIdentifier(Span span, string text, Definition def)
			{
				if (TokenCreateCallback != null)
				{
					var tok = new IdentifierToken(Scope, span, text, def);
					if (FirstToken == null) FirstToken = tok;
					TokenCreateCallback(tok);
				}
			}

			public void OnOperator(Span span, string text)
			{
				if (TokenCreateCallback != null)
				{
					var tok = new OperatorToken(Scope, span, text);
					if (FirstToken == null) FirstToken = tok;
					TokenCreateCallback(tok);
				}
			}

			public void OnStringLiteral(Span span, string text)
			{
				if (TokenCreateCallback != null)
				{
					var tok = new StringLiteralToken(Scope, span, text);
					if (FirstToken == null) FirstToken = tok;
					TokenCreateCallback(tok);
				}
			}

			public void OnNumber(Span span, string text)
			{
				if (TokenCreateCallback != null)
				{
					var tok = new NumberToken(Scope, span, text);
					if (FirstToken == null) FirstToken = tok;
					TokenCreateCallback(tok);
				}
			}

			public void OnUnknown(Span span, string text)
			{
				if (TokenCreateCallback != null)
				{
					var tok = new UnknownToken(Scope, span, text);
					if (FirstToken == null) FirstToken = tok;
					TokenCreateCallback(tok);
				}
			}

			public void OnToken(Token token)
			{
				if (FirstToken == null) FirstToken = token;
				if (TokenCreateCallback != null) TokenCreateCallback(token);
			}
		}

		/// <summary>
		/// Parses a data type from a string.
		/// </summary>
		/// <param name="a">Contains arguments that control the parsing.</param>
		/// <returns>A data type object, if a data type could be parsed; otherwise null.</returns>
		public static DataType TryParse(ParseArgs a)
		{
			var code = a.Code;
			var startPos = code.Position;

			// Check if there is a data-type name embedded before the source.
			string name = null;
			if (code.ReadExact('@'))
			{
				var tildeSpan = code.Span;
				if (code.ReadWord()) name = code.Text;
				else if (code.ReadStringLiteral()) name = CodeParser.StringLiteralToString(code.Text);
				else
				{
					code.Position = startPos;
					return null;
				}
			}
			if (!string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(a.TypeName)) a.TypeName = name;

			if (!code.ReadWord()) return null;
			var startWord = code.Text;

			DataType dataType = null;

			switch (code.Text)
			{
				case "void":
					a.OnDataTypeKeyword(code.Span, code.Text, null);
					dataType = DataType.Void;
					break;

				case "numeric":
				case "decimal":
				case "NUMERIC":
					a.OnDataTypeKeyword(code.Span, code.Text, null);
					dataType = ProcessNumeric(a, code.Text);
					break;

				case "unsigned":
				case "signed":
					a.OnDataTypeKeyword(code.Span, code.Text, null);
					dataType = ProcessSignedUnsigned(a, code.Text);
					break;

				case "int":
				case "short":
				case "long":
				case "ulong":
				case "number":
				case "unumber":
					a.OnDataTypeKeyword(code.Span, code.Text, null);
					dataType = ProcessInt(a, code.Text);
					break;

				case "char":
				case "character":
				case "varchar":
				case "CHAR":
					a.OnDataTypeKeyword(code.Span, code.Text, null);
					dataType = ProcessChar(a, code.Text);
					break;

				case "string":
					a.OnDataTypeKeyword(code.Span, code.Text, null);
					dataType = ProcessString(a, code.Text);
					break;

				case "date":
					a.OnDataTypeKeyword(code.Span, code.Text, null);
					dataType = ProcessDate(a, code.Text);
					break;

				case "time":
					a.OnDataTypeKeyword(code.Span, code.Text, null);
					dataType = ProcessTime(a, code.Text);
					break;

				case "enum":
					a.OnDataTypeKeyword(code.Span, code.Text, null);
					dataType = ProcessEnum(a);
					break;

				case "like":
					a.OnDataTypeKeyword(code.Span, code.Text, null);
					dataType = ProcessLike(a);
					break;

				case "table":
					a.OnDataTypeKeyword(code.Span, code.Text, null);
					dataType = DataType.Table;
					break;

				case "indrel":
					a.OnDataTypeKeyword(code.Span, code.Text, null);
					dataType = DataType.IndRel;
					break;

				case "command":
					a.OnDataTypeKeyword(code.Span, code.Text, null);
					dataType = DataType.Command;
					break;

				case "Section":
					a.OnDataTypeKeyword(code.Span, code.Text, null);
					dataType = ProcessSection(a);
					break;

				case "scroll":
					a.OnDataTypeKeyword(code.Span, code.Text, null);
					dataType = ProcessScroll(a);
					break;

				case "graphic":
					a.OnDataTypeKeyword(code.Span, code.Text, null);
					dataType = ProcessGraphic(a);
					break;

				case "interface":
					a.OnDataTypeKeyword(code.Span, code.Text, null);
					dataType = ProcessInterface(a);
					break;

				case "variant":
					a.OnDataTypeKeyword(code.Span, code.Text, null);
					dataType = DataType.Variant;
					break;

				case "oleobject":
					a.OnDataTypeKeyword(code.Span, code.Text, null);
					dataType = DataType.OleObject;
					break;

				case "Boolean_t":
					a.OnDataTypeKeyword(code.Span, code.Text, null);
					dataType = DataType.Boolean_t;
					break;

				default:
					if (a.DataTypeCallback != null)
					{
						var word = code.Text;
						var wordSpan = code.Span;

						var def = a.DataTypeCallback(word);
						if (def != null)
						{
							a.OnDataTypeKeyword(wordSpan, word, def);
							dataType = def.DataType;
						}
					}
					break;
			}

			if (dataType == null)
			{
				code.Position = code.TokenStartPostion;
			}

			// Give the first keyword in the data type an inferred data type, to get casting to work
			var dtToken = a.FirstToken as DataTypeKeywordToken;
			if (dtToken != null)
			{
				dtToken.InferredDataType = dataType;
			}

			return dataType;
		}

		private static readonly string[] _bracketsEndTokens = new string[] { ")" };

		private static DataType ProcessNumeric(ParseArgs a, string tokenText)
		{
			var sb = new StringBuilder();
			sb.Append(tokenText);

			var code = a.Code;

			if (code.ReadExact('('))
			{
				sb.Append('(');

				BracketsToken brackets = null;
				if (a.TokenCreateCallback != null)
				{
					brackets = new BracketsToken(a.Scope);
					brackets.AddOpen(code.Span);
				}

				if (code.ReadNumber())
				{
					sb.Append(code.Text);
					if (brackets != null) brackets.AddToken(new NumberToken(a.Scope, code.Span, code.Text));

					if (code.ReadExact(','))
					{
						sb.Append(',');
						if (brackets != null) brackets.AddToken(new DelimiterToken(a.Scope, code.Span));

						if (code.ReadNumber())
						{
							sb.Append(code.Text);
							if (brackets != null) brackets.AddToken(new NumberToken(a.Scope, code.Span, code.Text));
						}
					}
				}
				else if (a.VisibleModel)
				{
					var exp = ExpressionToken.TryParse(a.Scope, _bracketsEndTokens);
					if (exp != null) a.OnToken(exp);
				}
				if (code.ReadExact(')'))
				{
					sb.Append(')');
					if (brackets != null)
					{
						brackets.AddClose(code.Span);
						a.OnToken(brackets);
					}
				}
				else return new DataType(ValType.Numeric, a.TypeName, sb.ToString());
			}

			var done = false;
			var gotMask = false;
			while (!done && !code.EndOfFile)
			{
				if (ReadAttribute(a, code, sb, "unsigned", "currency", "local_currency")) { }
				else if (!gotMask && code.ReadStringLiteral())
				{
					sb.Append(' ');
					sb.Append(code.Text);
					a.OnStringLiteral(code.Span, code.Text);
					gotMask = true;
				}
				else break;
			}

			return new DataType(ValType.Numeric, a.TypeName, sb.ToString());
		}

		private static DataType ProcessSignedUnsigned(ParseArgs a, string tokenText)
		{
			var sb = new StringBuilder();
			sb.Append(tokenText);

			var code = a.Code;

			if (code.ReadNumber())
			{
				// width
				sb.Append(' ');
				sb.Append(code.Type);
				a.OnNumber(code.Span, code.Text);
			}
			else if (code.ReadExact('('))
			{
				sb.Append('(');

				BracketsToken brackets = null;
				if (a.TokenCreateCallback != null)
				{
					brackets = new BracketsToken(a.Scope);
					brackets.AddOpen(code.Span);
				}

				if (code.ReadNumber())
				{
					sb.Append(code.Text);
					if (brackets != null) brackets.AddToken(new NumberToken(a.Scope, code.Span, code.Text));

					if (code.ReadExact(','))
					{
						sb.Append(',');
						if (brackets != null) brackets.AddToken(new DelimiterToken(a.Scope, code.Span));

						if (code.ReadNumber())
						{
							sb.Append(code.Text);
							if (brackets != null) brackets.AddToken(new NumberToken(a.Scope, code.Span, code.Text));
						}
					}
				}
				else if (a.VisibleModel)
				{
					var exp = ExpressionToken.TryParse(a.Scope, _bracketsEndTokens);
					if (exp != null) a.OnToken(exp);
				}

				if (code.ReadExact(')'))
				{
					sb.Append(')');
					if (brackets != null)
					{
						brackets.AddClose(code.Span);
						a.OnToken(brackets);
					}
				}
				else return new DataType(ValType.Numeric, a.TypeName, sb.ToString());
			}

			if (code.ReadExact("int"))
			{
				sb.Append(" int");
				a.OnDataTypeKeyword(code.Span, code.Text, null);
			}
			else if (code.ReadExact("short"))
			{
				sb.Append(" short");
				a.OnDataTypeKeyword(code.Span, code.Text, null);
			}
			else if (code.ReadExact("long"))
			{
				sb.Append(" long");
				a.OnDataTypeKeyword(code.Span, code.Text, null);
			}
			else if (code.ReadExact("char"))
			{
				sb.Append(" char");
				a.OnDataTypeKeyword(code.Span, code.Text, null);
			}

			var gotMask = false;
			while (!code.EndOfFile)
			{
				if (ReadAttribute(a, code, sb)) { }
				else if (!gotMask && code.ReadStringLiteral())
				{
					gotMask = true;
					sb.Append(' ');
					sb.Append(code.Text);
					a.OnStringLiteral(code.Span, code.Text);
				}
				else break;
			}

			return new DataType(ValType.Numeric, a.TypeName, sb.ToString());
		}

		private static DataType ProcessInt(ParseArgs a, string tokenText)
		{
			var sb = new StringBuilder();
			sb.Append(tokenText);

			var code = a.Code;

			if (code.ReadExact("unsigned"))
			{
				sb.Append(" unsigned");
				a.OnDataTypeKeyword(code.Span, code.Text, null);
			}
			else if (code.ReadExact("signed"))
			{
				sb.Append(" signed");
				a.OnDataTypeKeyword(code.Span, code.Text, null);
			}

			if (code.ReadNumber())
			{
				// width
				sb.Append(' ');
				sb.Append(code.Text);
				a.OnNumber(code.Span, code.Text);
			}

			while (!code.EndOfFile)
			{
				if (!ReadAttribute(a, code, sb)) break;
			}

			return new DataType(ValType.Numeric, a.TypeName, sb.ToString());
		}

		private static DataType ProcessChar(ParseArgs a, string tokenText)
		{
			var code = a.Code;
			if (!code.ReadExact('(')) return DataType.Char;

			BracketsToken brackets = null;
			if (a.TokenCreateCallback != null)
			{
				brackets = new BracketsToken(a.Scope);
				brackets.AddOpen(code.Span);
			}

			if (code.ReadNumber())
			{
				var sb = new StringBuilder();
				sb.Append(tokenText);
				sb.Append('(');
				sb.Append(code.Text);
				if (brackets != null) brackets.AddToken(new NumberToken(a.Scope, code.Span, code.Text));
				if (code.ReadExact(')'))
				{
					sb.Append(')');
					if (brackets != null)
					{
						brackets.AddClose(code.Span);
						a.OnToken(brackets);
					}
				}
				else return new DataType(ValType.String, a.TypeName, sb.ToString());

				var done = false;
				var gotMask = false;
				while (!done && !code.EndOfFile)
				{
					if (ReadAttribute(a, code, sb)) { }
					else if (!gotMask && code.ReadStringLiteral())
					{
						gotMask = true;
						sb.Append(' ');
						sb.Append(code.Text);
						a.OnStringLiteral(code.Span, code.Text);
					}
					else break;
				}

				return new DataType(ValType.String, a.TypeName, sb.ToString());
			}
			else if (a.VisibleModel)
			{
				var exp = ExpressionToken.TryParse(a.Scope, _bracketsEndTokens);
				if (exp != null)
				{
					var sb = new StringBuilder();
					sb.Append(tokenText);
					sb.Append('(');
					sb.Append(code.Text);
					if (brackets != null) brackets.AddToken(exp);
					if (code.ReadExact(')'))
					{
						sb.Append(')');
						if (brackets != null)
						{
							brackets.AddClose(code.Span);
							a.OnToken(brackets);
						}
					}

					return new DataType(ValType.String, a.TypeName, sb.ToString());
				}
				else
				{
					return new DataType(ValType.String, a.TypeName, tokenText);
				}
			}
			else
			{
				return new DataType(ValType.String, a.TypeName, tokenText);
			}
		}

		private static DataType ProcessString(ParseArgs a, string tokenText)
		{
			var sb = new StringBuilder();
			sb.Append(tokenText);

			var code = a.Code;

			if (code.ReadExact("varying"))
			{
				sb.Append(" varying");
				a.OnDataTypeKeyword(code.Span, code.Text, null);
			}
			else if (code.ReadNumber())
			{
				sb.Append(' ');
				sb.Append(code.Text);
				a.OnNumber(code.Span, code.Text);
			}

			while (!code.EndOfFile)
			{
				if (ReadAttribute(a, code, sb)) { }
				else if (code.ReadStringLiteral())
				{
					sb.Append(' ');
					sb.Append(code.Text);
					a.OnStringLiteral(code.Span, code.Text);
				}
				else break;
			}

			return new DataType(ValType.String, a.TypeName, sb.ToString());
		}

		private static DataType ProcessDate(ParseArgs a, string tokenText)
		{
			var sb = new StringBuilder();
			sb.Append(tokenText);

			var code = a.Code;

			if (code.ReadNumber())
			{
				// width
				sb.Append(' ');
				sb.Append(code.Text);
				a.OnNumber(code.Span, code.Text);
			}

			var gotMask = false;
			var done = false;
			while (!done && !code.EndOfFile)
			{
				if (ReadAttribute(a, code, sb, "shortform", "longform", "alternate")) { }
				else if (!gotMask && code.ReadStringLiteral())
				{
					sb.Append(' ');
					sb.Append(code.Text);
					a.OnStringLiteral(code.Span, code.Text);
				}
				else break;
			}

			return new DataType(ValType.Date, a.TypeName, sb.ToString());
		}

		private static DataType ProcessTime(ParseArgs a, string tokenText)
		{
			var sb = new StringBuilder();
			sb.Append(tokenText);

			var code = a.Code;

			var gotMask = false;
			while (!code.EndOfFile)
			{
				if (ReadAttribute(a, code, sb)) { }
				else if (!gotMask && code.ReadNumber())
				{
					gotMask = true;
					sb.Append(' ');
					sb.Append(code.Text);
					a.OnNumber(code.Span, code.Text);
				}
				else break;
			}

			return new DataType(ValType.Time, a.TypeName, sb.ToString());
		}

		private static DataType ProcessEnum(ParseArgs a)
		{
			var options = new List<Definition>();
			var sb = new StringBuilder();
			sb.Append("enum");

			var code = a.Code;
			BracesToken braces = null;

			// Read tokens before the option list
			var gotWidth = false;
			while (!code.EndOfFile)
			{
				if (code.ReadExact('{'))
				{
					if (a.TokenCreateCallback != null)
					{
						braces = new BracesToken(a.Scope);
						braces.AddOpen(code.Span);
					}
					break;
				}
				else if (ReadAttribute(a, code, sb, "alterable", "required", "nowarn", "numeric")) { }
				else if (!gotWidth && code.ReadNumber())
				{
					sb.Append(' ');
					sb.Append(code.Text);
					a.OnNumber(code.Span, code.Text);
					gotWidth = true;
				}
				else return new DataType(ValType.Enum, a.TypeName, sb.ToString());
			}

			// Read the option list
			if ((a.Flags & ParseFlag.Strict) != 0)
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

					if (code.Type == CodeType.Operator)
					{
						if (code.Text == "}")
						{
							if (braces != null) braces.AddClose(code.Span);
							break;
						}
						if (code.Text == ",")
						{
							if (braces != null) braces.AddToken(new DelimiterToken(a.Scope, code.Span));

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
					else if (code.Type == CodeType.StringLiteral || code.Type == CodeType.Word)
					{
						if (braces != null) braces.AddToken(new EnumOptionToken(a.Scope, code.Span, code.Text, null));

						var str = NormalizeEnumOption(code.Text);
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
							options.Add(new EnumOptionDefinition(str, null));
						}
					}
				}
			}
			else
			{
				while (!code.EndOfFile)
				{
					if (!code.Read()) break;
					if (code.Text == "}")
					{
						if (braces != null)
						{
							braces.AddClose(code.Span);
							a.OnToken(braces);
						}
						break;
					}
					if (code.Text == ",")
					{
						if (braces != null) braces.AddToken(new DelimiterToken(a.Scope, code.Span));
						continue;
					}
					switch (code.Type)
					{
						case CodeType.Word:
						case CodeType.StringLiteral:
							if (braces != null) braces.AddToken(new EnumOptionToken(a.Scope, code.Span, code.Text, null));
							options.Add(new EnumOptionDefinition(NormalizeEnumOption(code.Text), null));
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

			while (ReadAttribute(a, code, sb)) ;

			var dataType = new DataType(ValType.Enum, a.TypeName, sb.ToString())
			{
				_completionOptions = options.ToArray(),
				_completionOptionsType = CompletionOptionsType.EnumOptionsList
			};

			foreach (EnumOptionDefinition opt in options) opt.SetEnumDataType(dataType);

			if (braces != null)
			{
				foreach (var token in braces.FindDownward<EnumOptionToken>())
				{
					token.SetEnumDataType(dataType);
				}
			}

			return dataType;
		}

		private static DataType ProcessLike(ParseArgs a)
		{
			var code = a.Code;
			if (code.ReadWord())
			{
				var word1 = code.Text;
				var word1Span = code.Span;

				if (code.ReadExact('.'))
				{
					var dotSpan = code.Span;

					if (code.ReadWord())
					{
						var word2 = code.Text;
						var word2Span = code.Span;

						var table = DkDict.Dict.GetTable(word1);
						if (table != null)
						{
							var field = table.GetColumn(word2);
							if (field != null)
							{
								if (a.TokenCreateCallback != null)
								{
									var tableToken = new TableToken(a.Scope, word1Span, word1, table.Definition);
									var dotToken = new DotToken(a.Scope, dotSpan);
									var fieldToken = new TableFieldToken(a.Scope, word2Span, word2, field);
									var tableAndFieldToken = new TableAndFieldToken(a.Scope, tableToken, dotToken, fieldToken);
									a.OnToken(tableAndFieldToken);
								}
								return field.DataType;
							}
						}

						if (a.TokenCreateCallback != null)
						{
							a.OnToken(new UnknownToken(a.Scope, word1Span, word1));
							a.OnToken(new UnknownToken(a.Scope, dotSpan, "."));
							a.OnToken(new UnknownToken(a.Scope, word2Span, word2));
						}

						return new DataType(ValType.Unknown, a.TypeName, string.Concat("like ", word1, ".", word2));
					}
					else
					{
						if (a.TokenCreateCallback != null)
						{
							a.OnToken(new UnknownToken(a.Scope, word1Span, word1));
							a.OnToken(new UnknownToken(a.Scope, dotSpan, "."));
						}

						return new DataType(ValType.Unknown, a.TypeName, string.Concat("like ", word1, "."));
					}
				}

				if (a.VariableCallback != null)
				{
					var def = a.VariableCallback(word1);
					if (def != null)
					{
						a.OnIdentifier(code.Span, code.Text, def);
						return def.DataType;
					}
				}

				if (a.TokenCreateCallback != null) a.OnToken(new UnknownToken(a.Scope, word1Span, word1));

				return new DataType(ValType.Unknown, a.TypeName, string.Concat("like ", word1));
			}
			else return null;
		}

		private static DataType ProcessSection(ParseArgs a)
		{
			var sb = new StringBuilder();
			sb.Append("Section");

			var code = a.Code;

			if (code.ReadExact("Level"))
			{
				sb.Append(" Level");
				a.OnDataTypeKeyword(code.Span, code.Text, null);
				if (code.ReadNumber())
				{
					sb.Append(' ');
					sb.Append(code.Text);
					a.OnNumber(code.Span, code.Text);
				}
			}

			while (ReadAttribute(a, code, sb)) ;

			return new DataType(ValType.Section, a.TypeName, sb.ToString());
		}

		private static DataType ProcessScroll(ParseArgs a)
		{
			var sb = new StringBuilder();
			sb.Append("scroll");

			var code = a.Code;

			if (code.ReadNumber())
			{
				sb.Append(' ');
				sb.Append(code.Text);
				a.OnNumber(code.Span, code.Text);
			}

			while (ReadAttribute(a, code, sb)) ;

			return new DataType(ValType.Scroll, a.TypeName, sb.ToString());
		}

		private static DataType ProcessGraphic(ParseArgs a)
		{
			var sb = new StringBuilder();
			sb.Append("graphic");

			var code = a.Code;

			if (code.ReadNumber())	// rows
			{
				sb.Append(' ');
				sb.Append(code.Text);
				a.OnNumber(code.Span, code.Text);

				if (code.ReadNumber())	// columns
				{
					sb.Append(' ');
					sb.Append(code.Text);
					a.OnNumber(code.Span, code.Text);

					if (code.ReadNumber())	// bytes
					{
						sb.Append(' ');
						sb.Append(code.Text);
						a.OnNumber(code.Span, code.Text);
					}
				}
			}

			while (ReadAttribute(a, code, sb)) ;

			return new DataType(ValType.Graphic, a.TypeName, sb.ToString());
		}

		private static DataType ProcessInterface(ParseArgs a)
		{
			var code = a.Code;
			if (code.ReadWord())
			{
				var intfName = code.Text;

				var intf = DkDict.Dict.GetInterface(code.Text);
				if (intf != null)
				{
					if (a.TokenCreateCallback != null)
					{
						a.OnToken(new IdentifierToken(a.Scope, code.Span, code.Text, intf.Definition));
					}

					return intf.DataType;
				}

				// TODO: remove
				//return new DataType(ValType.Interface, a.TypeName, intfName, Definition.EmptyArray, CompletionOptionsType.InterfaceMembers)
				//{
				//	Interface = intf
				//};
			}

			return new DataType(ValType.Interface, a.TypeName, "interface");
		}

		private static bool ReadAttribute(ParseArgs a, CodeParser code, StringBuilder sb, params string[] extraTokens)
		{
			var startPos = code.Position;
			if (code.ReadWord())
			{
				var word = code.Text;
				switch (code.Text)
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
						a.OnDataTypeKeyword(code.Span, code.Text, null);
						sb.Append(' ');
						sb.Append(word);
						return true;

					case "tag":
						{
							sb.Append(" tag");
							a.OnDataTypeKeyword(code.Span, code.Text, null);

							var resetPos = code.Position;
							if (code.ReadTagName())
							{
								if (ProbeEnvironment.IsValidTagName(code.Text))
								{
									sb.Append(' ');
									sb.Append(code.Text);
									a.OnKeyword(code.Span, code.Text);
									if (code.ReadStringLiteral())
									{
										sb.Append(' ');
										sb.Append(code.Text);
										a.OnStringLiteral(code.Span, code.Text);
									}
									else if (code.ReadWord())
									{
										sb.Append(' ');
										sb.Append(code.Text);
										a.OnDataTypeKeyword(code.Span, code.Text, null);
									}
								}
								else
								{
									code.Position = resetPos;
								}
							}
						}
						return true;

					default:
						if (word.StartsWith("INTENSITY_") || extraTokens.Contains(word))
						{
							sb.Append(' ');
							sb.Append(word);
							a.OnDataTypeKeyword(code.Span, code.Text, null);
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
				a.OnDataTypeKeyword(code.Span, code.Text, null);
				return true;
			}

			return false;
		}

		private static readonly Regex _rxRepoWords = new Regex(@"\G((nomask|(NO)?PROBE|[%@]undefined|@neutral|(NO)?PICK)\b|&)");

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
			get
			{
				if (!string.IsNullOrEmpty(_name))
				{
					return string.Concat(_name, ": ", _source);
				}
				else
				{
					return _source;
				}
			}
		}

		public string ToPrettyString()
		{
			if (!string.IsNullOrEmpty(_name)) return _name;
			return _source;
		}

		public System.Windows.UIElement QuickInfoWpf
		{
			get
			{
				if (!string.IsNullOrEmpty(_name))
				{
					return Definition.WpfDivs(
						Definition.WpfMainLine(_name),
						Definition.WpfInfoLine(_source));
				}
				else
				{
					return Definition.WpfMainLine(_source);
				}
			}
		}

		public static string NormalizeEnumOption(string option)
		{
			if (string.IsNullOrWhiteSpace(option)) return "\" \"";

			if (option.IsWord()) return option;

			if (option.StartsWith("\"") && option.EndsWith("\""))
			{
				var inner = CodeParser.StringLiteralToString(option);
				if (string.IsNullOrWhiteSpace(inner) || inner.Trim() != inner || !inner.IsWord()) return option;
				return inner;
			}

			return CodeParser.StringToStringLiteral(option);
		}

#if DEBUG
		public static void CheckDataTypeParsing(string dataTypeText, CodeParser usedParser, DataType dataType)
		{
			if (dataType == null)
			{
				Log.Debug("WARNING: DataType.Parse was unable to parse [{0}]", dataTypeText);
			}
			else if (usedParser.Read())
			{
				Log.Debug("WARNING: DataType.Parse stopped before end of text [{0}] got [{1}]", dataTypeText, dataType.Name);
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

		public string ToCodeString()
		{
			if (!string.IsNullOrEmpty(_name))
			{
				return string.Concat("@", NormalizeEnumOption(_name), " ", _source);
			}
			else
			{
				return _source;
			}
		}

		public static float CalcArgumentCompatibility(DataType argType, DataType passType)
		{
			if (argType == null) return 1.0f;
			if (passType == null) return .5f;

			switch (argType.ValueType)
			{
				case ValType.Unknown:
				case ValType.Void:
					return 1.0f;
				case ValType.Numeric:
					switch (passType.ValueType)
					{
						case ValType.Unknown:	return .5f;
						case ValType.Void:		return .5f;
						case ValType.Numeric:	return 1.0f;
						case ValType.String:	return .9f;
						case ValType.Char:		return .75f;
						case ValType.Enum:		return .75f;
						case ValType.Date:		return .75f;
						case ValType.Time:		return .75f;
						default:				return .2f;
					}
				case ValType.String:
					switch (passType.ValueType)
					{
						case ValType.Unknown:	return .5f;
						case ValType.Void:		return .5f;
						case ValType.Numeric:	return .9f;
						case ValType.String:	return 1.0f;
						case ValType.Char:		return .8f;
						case ValType.Enum:		return .9f;
						case ValType.Date:		return .9f;
						case ValType.Time:		return .9f;
						default:				return .2f;
					}
				case ValType.Char:
					switch (passType.ValueType)
					{
						case ValType.Unknown:	return .5f;
						case ValType.Void:		return .5f;
						case ValType.Numeric:	return .7f;
						case ValType.String:	return .7f;
						case ValType.Char:		return 1.0f;
						case ValType.Enum:		return .8f;
						case ValType.Date:		return .7f;
						case ValType.Time:		return .7f;
						default:				return .2f;
					}
				case ValType.Enum:
					switch (passType.ValueType)
					{
						case ValType.Unknown:	return .5f;
						case ValType.Void:		return .5f;
						case ValType.Numeric:	return .8f;
						case ValType.String:	return .9f;
						case ValType.Char:		return .9f;
						case ValType.Enum:		return 1.0f;
						case ValType.Date:		return .7f;
						case ValType.Time:		return .7f;
						default:				return .2f;
					}
				case ValType.Date:
					switch (passType.ValueType)
					{
						case ValType.Unknown:	return .5f;
						case ValType.Void:		return .5f;
						case ValType.Numeric:	return .9f;
						case ValType.String:	return .9f;
						case ValType.Char:		return .7f;
						case ValType.Enum:		return .7f;
						case ValType.Date:		return 1.0f;
						case ValType.Time:		return .7f;
						default:				return .2f;
					}
				case ValType.Time:
					switch (passType.ValueType)
					{
						case ValType.Unknown:	return .5f;
						case ValType.Void:		return .5f;
						case ValType.Numeric:	return .9f;
						case ValType.String:	return .9f;
						case ValType.Char:		return .7f;
						case ValType.Enum:		return .7f;
						case ValType.Date:		return .7f;
						case ValType.Time:		return 1.0f;
						default:				return .2f;
					}
				case ValType.Table:
					switch (passType.ValueType)
					{
						case ValType.Unknown:	return .5f;
						case ValType.Void:		return .5f;
						case ValType.Table:		return 1.0f;
						case ValType.IndRel:	return .9f;
						default:				return .2f;
					}
				case ValType.IndRel:
					switch (passType.ValueType)
					{
						case ValType.Unknown:	return .5f;
						case ValType.Void:		return .5f;
						case ValType.Table:		return .9f;
						case ValType.IndRel:	return 1.0f;
						default:				return .2f;
					}
				case ValType.Interface:
					switch (passType.ValueType)
					{
						case ValType.Unknown:	return .5f;
						case ValType.Void:		return .5f;
						case ValType.Interface: return argType._source == passType._source ? 1.0f : .7f;
						default:				return .2f;
					}
				case ValType.Command:
					switch (passType.ValueType)
					{
						case ValType.Unknown:	return .5f;
						case ValType.Void:		return .5f;
						case ValType.Command:	return 1.0f;
						default:				return .2f;
					}
				case ValType.Section:
					switch (passType.ValueType)
					{
						case ValType.Unknown:	return .5f;
						case ValType.Void:		return .5f;
						case ValType.Section:	return 1.0f;
						default:				return .2f;
					}
				case ValType.Scroll:
					switch (passType.ValueType)
					{
						case ValType.Unknown:	return .5f;
						case ValType.Void:		return .5f;
						case ValType.Scroll:	return 1.0f;
						default:				return .2f;
					}
				case ValType.Graphic:
					switch (passType.ValueType)
					{
						case ValType.Unknown:	return .5f;
						case ValType.Void:		return .5f;
						case ValType.Graphic:	return 1.0f;
						default:				return .2f;
					}
				default:
					return .2f;
			}
		}
	}
}

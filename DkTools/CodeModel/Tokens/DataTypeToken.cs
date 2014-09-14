using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DkTools.CodeModel.Definitions;

namespace DkTools.CodeModel.Tokens
{
	internal class DataTypeToken : GroupToken, IDataTypeToken
	{
		private DataType _dataType;
		private string _text;

		/// <summary>
		/// Creates a data type token.
		/// </summary>
		/// <param name="parent">(required) Parent token</param>
		/// <param name="scope">(required) Current scope</param>
		/// <param name="startPos">(required) Starting position for the group</param>
		private DataTypeToken(GroupToken parent, Scope scope, int startPos)
			: base(parent, scope, startPos)
		{
			_dataType = DataType.Void;
			ClassifierType = Classifier.ProbeClassifierType.DataType;
		}

		/// <summary>
		/// Creates a data type token.
		/// </summary>
		/// <param name="parent">(required) Parent token</param>
		/// <param name="scope">(required) Current scope</param>
		/// <param name="tokens">(required) The tokens that make up the data type text.</param>
		/// <param name="completionOptions">(optional) Hard-coded completion options.</param>
		/// <param name="infoText">(required) Help text for the data type.</param>
		private DataTypeToken(GroupToken parent, Scope scope, IEnumerable<Token> tokens, Definition[] completionOptions, string infoText)
			: base(parent, scope, tokens)
		{
			_text = Token.GetNormalizedText(tokens);

			if (completionOptions != null) _dataType = new DataType(_text, completionOptions, infoText);
			else _dataType = new DataType(_text, infoText);

			ClassifierType = Classifier.ProbeClassifierType.DataType;
		}

		/// <summary>
		/// Creates a data type token.
		/// </summary>
		/// <param name="parent">(required) Parent token</param>
		/// <param name="scope">(required) Current scope</param>
		/// <param name="token">(required) Token that contains the data type text.</param>
		/// <param name="dataType">(required) Assigned data type</param>
		/// <param name="def">(optional) Definition point of the data type.  Can be null for built-in data types.</param>
		public DataTypeToken(GroupToken parent, Scope scope, IdentifierToken token, DataType dataType, Definition def)
			: base(parent, scope, new Token[] { token })
		{
			_dataType = dataType;
			_text = token.Text;

			if (def != null) token.SourceDefinition = def;

			ClassifierType = Classifier.ProbeClassifierType.DataType;
		}

		private static Regex _rxValidEnumOption = new Regex(@"^(\""(\\.|[^""])*\""|[a-zA-Z_][a-zA-Z0-9_]*)$");

		public static DataTypeToken TryParse(GroupToken parent, Scope scope)
		{
			var file = scope.File;
			if (!file.SkipWhiteSpaceAndComments(scope)) return null;

			var word = file.PeekWord();
			if (string.IsNullOrEmpty(word)) return null;

			if (Constants.DataTypeKeywords.Contains(word))
			{
				var startPos = file.Position;
				file.MoveNext(word.Length);

				var firstToken = new DataTypeKeywordToken(parent, scope, new Span(startPos, file.Position), word);
				return Parse(parent, scope, firstToken);
			}

			if (scope.Hint.HasFlag(ScopeHint.FunctionArgs))
			{
				if (word == "table")
				{
					var ident = new IdentifierToken(parent, scope, file.MoveNextSpan(word.Length), word);
					ident.ClassifierType = Classifier.ProbeClassifierType.DataType;
					return new DataTypeToken(parent, scope, ident, DataType.Table, null);
				}
				else if (word == "indrel")
				{
					var ident = new IdentifierToken(parent, scope, file.MoveNextSpan(word.Length), word);
					ident.ClassifierType = Classifier.ProbeClassifierType.DataType;
					return new DataTypeToken(parent, scope, ident, DataType.IndRel, null);
				}
			}

			var def = parent.GetDefinitions<DataTypeDefinition>(word).FirstOrDefault();
			if (def != null)
			{
				var startPos = file.Position;
				file.MoveNext(word.Length);

				return new DataTypeToken(parent, scope, new IdentifierToken(parent, scope, new Span(startPos, file.Position), word), def.DataType, def);
			}

			return null;
		}

		public static DataTypeToken Parse(GroupToken parent, Scope scope, DataTypeKeywordToken firstToken)
		{
			var file = scope.File;
			var startPos = firstToken.Span.Start;

			scope.Hint |= ScopeHint.SuppressDataType | ScopeHint.SuppressFunctionCall | ScopeHint.SuppressFunctionDefinition | ScopeHint.SuppressVarDecl | ScopeHint.SuppressVars;

			if (firstToken.Text == "enum")
			{
				var ret = new DataTypeToken(parent, scope, startPos);
				ret.AddToken(firstToken);

				var proto = KeywordToken.TryParseMatching(ret, scope, "proto");
				if (proto != null) ret.AddToken(proto);

				var nowarn = KeywordToken.TryParseMatching(ret, scope, "nowarn");
				if (nowarn != null) ret.AddToken(nowarn);

				Definition[] completionOptions = null;

				var braces = BracesToken.TryParse(ret, scope);
				if (braces != null)
				{
					ret.AddToken(braces);
					completionOptions = (from t in braces.FindDownward(x => _rxValidEnumOption.IsMatch(x.Text)) select new EnumOptionDefinition(t.Text)).ToArray();
				}

				var text = ret.NormalizedText;
				ret._dataType = new DataType(text, completionOptions, text);
				return ret;
			}
			else if (firstToken.Text == "numeric")
			{
				var ret = new DataTypeToken(parent, scope, startPos);
				ret.AddToken(firstToken);

				var width = BracketsToken.TryParse(ret, scope);
				if (width != null) ret.AddToken(width);

				var done = false;
				while (!done && file.SkipWhiteSpaceAndComments(scope))
				{
					var word = file.PeekWord();
					switch (word)
					{
						case "unsigned":
						case "currency":
						case "local_currency":
						case "LEADINGZEROS":
						case "PROBE":
							ret.AddToken(new DataTypeKeywordToken(ret, scope, file.MoveNextSpan(word.Length), word));
							break;
						default:
							if (string.IsNullOrEmpty(word))
							{
								var mask = StringLiteralToken.TryParse(ret, scope);
								if (mask != null) ret.AddToken(mask);
								else done = true;
							}
							else done = true;
							break;
					}
				}

				var text = ret.NormalizedText;
				ret._dataType = new DataType(text, text);
				return ret;
			}
			else if (firstToken.Text == "unsigned")
			{
				var ret = new DataTypeToken(parent, scope, startPos);
				ret.AddToken(firstToken);

				var width = NumberToken.TryParse(ret, scope, false);
				if (width != null) ret.AddToken(width);

				var text = ret.NormalizedText;
				ret._dataType = new DataType(text, text);
				return ret;
			}
			else if (firstToken.Text == "char")
			{
				var ret = new DataTypeToken(parent, scope, startPos);
				ret.AddToken(firstToken);

				var width = BracketsToken.TryParse(ret, scope);
				if (width != null) ret.AddToken(width);

				var mask = StringLiteralToken.TryParse(ret, scope);
				if (mask != null) ret.AddToken(mask);

				var text = ret.NormalizedText;
				ret._dataType = new DataType(text, text);
				return ret;
			}
			else if (firstToken.Text == "string")
			{
				var ret = new DataTypeToken(parent, scope, startPos);
				ret.AddToken(firstToken);

				var width = NumberToken.TryParse(parent, scope, false);
				if (width != null) ret.AddToken(width);

				return ret;
			}
			else if (firstToken.Text == "date")
			{
				var ret = new DataTypeToken(parent, scope, startPos);
				ret.AddToken(firstToken);

				var width = NumberToken.TryParse(parent, scope, false);
				if (width != null) ret.AddToken(width);

				var done = false;
				while (!done && file.SkipWhiteSpaceAndComments(scope))
				{
					var word = file.PeekWord();
					switch (word)
					{
						case "shortform":
						case "longform":
						case "alternate":
						case "PROBE":
							ret.AddToken(new DataTypeKeywordToken(ret, scope, file.MoveNextSpan(word.Length), word));
							break;
						default:
							done = true;
							break;
					}
				}

				var mask = StringLiteralToken.TryParse(ret, scope);
				if (mask != null) ret.AddToken(mask);

				var text = ret.NormalizedText;
				ret._dataType = new DataType(text, text);
				return ret;
			}
			else if (firstToken.Text == "like")
			{
				var ret = new DataTypeToken(parent, scope, startPos);
				ret.AddToken(firstToken);

				Definition[] completionOptions = null;

				var tf = TableAndFieldToken.TryParse(ret, scope);
				if (tf != null)
				{
					ret.AddToken(tf);
					var dt = tf.ValueDataType;
					if (dt != null) completionOptions = dt.CompletionOptions.ToArray();
				}
				else
				{
					var tt = TableToken.TryParse(parent, scope);
					if (tt != null)
					{
						ret.AddToken(tt);
						var probeTable = ProbeEnvironment.GetTable(tt.Text);

						var dot = DotToken.TryParse(parent, scope);
						if (dot != null)
						{
							ret.AddToken(dot);

							var field = IdentifierToken.TryParse(parent, scope);
							var probeField = probeTable.GetField(field.Text);
							if (probeField != null)
							{
								ret.AddToken(new TableFieldToken(ret, scope, field.Span, field.Text, tt));
								completionOptions = probeField.CompletionOptions.ToArray();
							}
							else
							{
								ret.AddToken(field);
							}
						}
					}
					else
					{
						var ident = IdentifierToken.TryParse(parent, scope);
						if (ident != null)
						{
							var def = parent.GetDefinitions<VariableDefinition>(ident.Text).FirstOrDefault();
							if (def != null)
							{
								ret.AddToken(new VariableToken(ret, scope, ident.Span, ident.Text, def));
								var dt = def.DataType;
								if (dt != null) completionOptions = dt.CompletionOptions.ToArray();
							}
							else ret.AddToken(ident);
						}
					}
				}

				var text = ret.NormalizedText;
				ret._dataType = new DataType(text, completionOptions, text);
				return ret;
			}

			return new DataTypeToken(parent, firstToken.Scope, new Token[] { firstToken }, null, firstToken.Text);
		}

		public DataType DataType
		{
			get { return _dataType; }
		}

		public override string Text
		{
			get
			{
				if (_text == null) _text = this.NormalizedText;
				return _text;
			}
		}

		protected override void OnChildTokenAdded(Token child)
		{
			_text = null;
			base.OnChildTokenAdded(child);
		}

		public override void DumpTreeInner(System.Xml.XmlWriter xml)
		{
			base.DumpTreeInner(xml);
			_dataType.DumpTree(xml);
		}
	}
}

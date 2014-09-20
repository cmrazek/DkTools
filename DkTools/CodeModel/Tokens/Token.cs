using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using DkTools.CodeModel.Definitions;

namespace DkTools.CodeModel.Tokens
{
	/// <summary>
	/// Base class for all token objects.
	/// </summary>
	internal abstract class Token
	{
		private Token _parent;
		private Scope _scope;
		private Span _span;
		private Definition _sourceDefinition;
		private Classifier.ProbeClassifierType _classifierType;

		public void DumpTree(System.Xml.XmlWriter xml)
		{
			xml.WriteStartElement(GetType().Name);
			DumpTreeAttribs(xml);
			DumpTreeInner(xml);
			xml.WriteEndElement();
		}

		public virtual void DumpTreeAttribs(System.Xml.XmlWriter xml)
		{
			xml.WriteAttributeString("offset", _span.Start.ToString());
		}

		public virtual void DumpTreeInner(System.Xml.XmlWriter xml)
		{
			if (_sourceDefinition != null)
			{
				xml.WriteStartElement("SourceDefinition");
				_sourceDefinition.DumpTree(xml);
				xml.WriteEndElement();	// SourceDefinition
			}
		}

		public string DumpTreeText()
		{
			var settings = new XmlWriterSettings();
			settings.Indent = true;
			settings.OmitXmlDeclaration = true;

			var sb = new StringBuilder();
			using (var xml = XmlWriter.Create(sb, settings))
			{
				DumpTree(xml);
			}
			return sb.ToString();
		}

		public Token(GroupToken parent, Scope scope, Span span)
		{
			_parent = parent;
			_scope = scope;
			_span = span;
		}

		/// <summary>
		/// Called when the token is being saved into the model, and is no longer considered temporary.
		/// </summary>
		public virtual void CommitToParentToken(Token parent)
		{
			_parent = parent;
		}

		public Token Parent
		{
			get { return _parent; }
			//set { _parent = value; }
		}

		public Scope Scope
		{
			get { return _scope; }
			set { _scope = value; }
		}

		public CodeFile File
		{
			get { return _scope.File; }
		}

		public Span Span
		{
			get { return _span; }
			set { _span = value; }
		}

		public override string ToString()
		{
			var text = NormalizedText;
			if (text.Length > 20) text = text.Substring(20);
			return string.Format("\"{0}\" ({1})", text, this.GetType());
		}

		public Token FindTokenOfType(int pos, Type type)
		{
			if (!_span.Contains(pos)) return null;
			if (this is GroupToken)
			{
				var group = this as GroupToken;
				foreach (var token in group.SubTokens)
				{
					var t = token.FindTokenOfType(pos, type);
					if (t != null) return t;
				}
				return type.IsAssignableFrom(GetType()) ? this : null;
			}
			else if (type.IsAssignableFrom(GetType()))
			{
				return this;
			}
			else
			{
				return null;
			}
		}

		public Token FindNearbyTokenOfType(int pos, Type type)
		{
			if (!_span.Touching(pos)) return null;
			if (this is GroupToken)
			{
				foreach (var token in (this as GroupToken).SubTokens)
				{
					var t = token.FindNearbyTokenOfType(pos, type);
					if (t != null) return t;
				}
				return type.IsAssignableFrom(GetType()) ? this : null;
			}
			else if (type.IsAssignableFrom(GetType()))
			{
				return this;
			}
			else
			{
				return null;
			}
		}

		public virtual IEnumerable<OutliningRegion> OutliningRegions
		{
			get
			{
				if (this is GroupToken)
				{
					foreach (var token in (this as GroupToken).SubTokens)
					{
						foreach (var region in token.OutliningRegions)
						{
							yield return region;
						}
					}
				}
			}
		}

		public virtual string Text
		{
			get { return _scope.File.GetText(_span); }
		}

		public virtual bool BreaksStatement
		{
			get { return false; }
		}

		public virtual string NormalizedText
		{
			get
			{
				if (this is GroupToken) return Token.GetNormalizedText((this as GroupToken).SubTokens);
				return NormalizePlainText(Text);
			}
		}

		public static string GetNormalizedText(IEnumerable<Token> tokenList)
		{
			var sb = new StringBuilder();
			Token lastToken = null;
			foreach (var token in tokenList)
			{
				if (sb.Length > 0)
				{
					var needSpace = true;
					if (token is DelimiterToken || token is StatementEndToken || token is DotToken || token is BraceToken || token is BracketToken || token is ArrayBraceToken) needSpace = false;
					else if (token is BracketsToken && lastToken is WordToken) needSpace = false;
					else if (token is ArrayBracesToken && lastToken is WordToken) needSpace = false;
					else if (token is WordToken && lastToken is ReferenceToken) needSpace = false;
					else if (lastToken is DotToken) needSpace = false;

					if (needSpace) sb.Append(" ");
				}
				sb.Append(token.NormalizedText);
				lastToken = token;
			}

			return sb.ToString();
		}

		public static string NormalizePlainText(string str)
		{
			var sb = new StringBuilder(str.Length);
			var lastWhiteSpace = true;

			foreach (var ch in str)
			{
				if (char.IsWhiteSpace(ch))
				{
					if (!lastWhiteSpace)
					{
						sb.Append(" ");
						lastWhiteSpace = true;
					}
				}
				else
				{
					sb.Append(ch);
					lastWhiteSpace = false;
				}
			}

			return sb.ToString().TrimEnd();
		}

		#region Find Operations
		public IEnumerable<Token> FindDownward(int pos)
		{
			if (!_span.Contains(pos)) yield break;

			if (this is GroupToken)
			{
				yield return this;

				foreach (var token in (this as GroupToken).SubTokens)
				{
					foreach (var token2 in token.FindDownward(pos))
					{
						yield return token2;
					}
				}
			}
			else
			{
				yield return this;
			}
		}

		public IEnumerable<Token> FindDownward(Span span)
		{
			if (!_span.Intersects(span)) yield break;

			yield return this;

			if (this is GroupToken)
			{
				foreach (var token in (this as GroupToken).SubTokens)
				{
					foreach (var token2 in token.FindDownward(span)) yield return token2;
				}
			}
		}

		public IEnumerable<Token> FindDownwardTouching(int offset)
		{
			if (!_span.Touching(offset)) yield break;

			if (typeof(GroupToken).IsAssignableFrom(GetType()))
			{
				yield return this;

				foreach (var token in (this as GroupToken).SubTokens)
				{
					foreach (var token2 in token.FindDownwardTouching(offset))
					{
						yield return token2;
					}
				}
			}
			else
			{
				yield return this;
			}
		}

		public IEnumerable<Token> FindDownward(Predicate<Token> pred)
		{
			if (pred(this)) yield return this;

			if (this is GroupToken)
			{
				foreach (var subToken in (this as GroupToken).SubTokens)
				{
					foreach (var subTokenFound in subToken.FindDownward(pred))
					{
						yield return subTokenFound;
					}
				}
			}
		}

		public IEnumerable<Token> FindDownward(int pos, Predicate<Token> pred)
		{
			if (!_span.Contains(pos)) yield break;
			if (pred(this)) yield return this;

			if (this is GroupToken)
			{
				foreach (var token in (this as GroupToken).SubTokens)
				{
					foreach (var token2 in token.FindDownward(pos, pred))
					{
						yield return token2;
					}
				}
			}
		}

		public IEnumerable<Token> FindDownwardTouching(int pos, Predicate<Token> pred)
		{
			if (!_span.Touching(pos)) yield break;
			if (pred(this)) yield return this;

			if (this is GroupToken)
			{
				foreach (var token in (this as GroupToken).SubTokens)
				{
					foreach (var token2 in token.FindDownwardTouching(pos, pred))
					{
						yield return token2;
					}
				}
			}
		}

		public IEnumerable<Token> FindDownward(int start, int length)
		{
			if (!_span.Intersects(start, length)) yield break;

			yield return this;

			if (this is GroupToken)
			{
				foreach (var token in (this as GroupToken).SubTokens)
				{
					foreach (var token2 in token.FindDownward(start, length)) yield return token2;
				}
			}
		}

		public IEnumerable<Token> FindDownward(int start, int length, Predicate<Token> pred)
		{
			if (!_span.Intersects(start, length)) yield break;
			if (pred(this)) yield return this;

			if (this is GroupToken)
			{
				foreach (var token in (this as GroupToken).SubTokens)
				{
					foreach (var token2 in token.FindDownward(start, length, pred))
					{
						yield return token2;
					}
				}
			}
		}

		public IEnumerable<Token> FindUpward(Predicate<Token> pred)
		{
			if (pred(this)) yield return this;

			for (var tok = this; tok != null; tok = tok._parent)
			{
				if (pred(tok)) yield return tok;

				if (tok is GroupToken)
				{
					foreach (var subTok in (tok as GroupToken).SubTokens)
					{
						if (pred(subTok)) yield return subTok;
					}
				}
			}
		}
		#endregion

		public virtual IEnumerable<IncludeToken.IncludeDef> GetUnprocessedIncludes()
		{
			if (typeof(GroupToken).IsAssignableFrom(GetType()))
			{
				foreach (var token in (this as GroupToken).SubTokens)
				{
					foreach (var includeDef in token.GetUnprocessedIncludes())
					{
						yield return includeDef;
					}
				}
			}
		}

		#region Definitions

		public Definition SourceDefinition
		{
			get { return _sourceDefinition; }
			set { _sourceDefinition = value; }
		}

		protected DefinitionProvider DefinitionProvider
		{
			get { return _scope.File.Model.DefinitionProvider; }
		}
		#endregion

		/// <summary>
		/// Attempts to locate tooltip text for the token, or will ask the parent for text.
		/// </summary>
		/// <param name="token">If not null, then the mouse is hovering over this sub-token; otherwise the mouse is hovering over this token.</param>
		/// <returns>Tooltip text for this token.</returns>
		public virtual string GetQuickInfo(Token token = null)
		{
			if (_sourceDefinition != null)
			{
				var defText = _sourceDefinition.QuickInfoText;
				if (!string.IsNullOrWhiteSpace(defText)) return defText;
			}

			if (_parent != null) return _parent.GetQuickInfo(token != null ? token : this);
			return string.Empty;
		}

		#region Data Types (assigned value)
		/// <summary>
		/// If this token can retain a value, get the data type object for it; otherwise returns null.
		/// </summary>
		public virtual DataType ValueDataType
		{
			get { return null; }
		}
		#endregion

		public bool IsOnRoot
		{
			get { return !_scope.Hint.HasFlag(ScopeHint.NotOnRoot); }
		}

		public Classifier.ProbeClassifierType ClassifierType
		{
			get { return _classifierType; }
			set { _classifierType = value; }
		}

		public static IEnumerable<Token> SafeTokenList(params Token[] tokens)
		{
			foreach (var token in tokens)
			{
				if (token != null) yield return token;
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using DkTools.CodeModel.Definitions;

namespace DkTools.CodeModel
{
	/// <summary>
	/// Base class for all token objects.
	/// </summary>
	internal abstract class Token
	{
		private Token _parent;
		private Scope _scope;
		private Span _span;
		private Dictionary<string, LinkedList<Definition>> _defs;	// Use a dictionary to make it faster to search by name
		private Definition _sourceDefinition;
		private Classifier.ProbeClassifierType _classifierType;
		private LocalFilePosition? _localFilePos;

		public void DumpTree(System.Xml.XmlWriter xml)
		{
			xml.WriteStartElement(GetType().Name);
			DumpTreeInner(xml);
			xml.WriteEndElement();
		}

		public virtual void DumpTreeInner(System.Xml.XmlWriter xml)
		{
			xml.WriteAttributeString("offset", _span.Start.Offset.ToString());

			if (_sourceDefinition != null)
			{
				xml.WriteStartElement("SourceDefinition");
				_sourceDefinition.DumpTree(xml);
				xml.WriteEndElement();	// SourceDefinition
			}

			if (_defs != null && _defs.Any())
			{
				xml.WriteStartElement("Definitions");
				foreach (var defList in _defs.Values)
				{
					foreach (var def in defList) def.DumpTree(xml);
				}
				xml.WriteEndElement();	// Definitions
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

		public string DumpDefinitionsText()
		{
			var sb = new StringBuilder();

			foreach (var defList in _defs.Values)
			{
				foreach (var def in defList)
				{
					sb.Append(def.Name);
					sb.Append(" ");
					sb.Append(def.GetType());
					sb.AppendLine();
				}
			}

			return sb.ToString();
		}

		public Token(GroupToken parent, Scope scope, Span span)
		{
			_parent = parent;
			_scope = scope;
			_span = span;

			if (!scope.Preprocessor)
			{
				var defProv = _scope.DefinitionProvider;
				if (defProv != null) AddDefinitions(defProv.GetLocalDefinitionsForOffset(span.Start.Offset));
			}
		}

		/// <summary>
		/// Called when the token is being saved into the model, and is no longer considered temporary.
		/// </summary>
		public virtual void CommitToParentToken(Token parent)
		{
			_parent = parent;

			if (_parent != null)
			{
				if (_defs != null &&
					Scope.Preprocessor)	// Only move definitions around when we're in the 'create' model.
				{
					Dictionary<string, LinkedList<Definition>> keepDefs = null;

					var copyLocal = !(this is GroupToken) || !(this as GroupToken).IsLocalScope;
					foreach (var defList in _defs.Values)
					{
						foreach (var def in defList)
						{
							if (def.Global || copyLocal)
							{
								_parent.AddDefinition(def);
							}
							else
							{
								if (keepDefs == null) keepDefs = new Dictionary<string, LinkedList<Definition>>();

								LinkedList<Definition> list;
								keepDefs.TryGetValue(def.Name, out list);
								if (list == null) keepDefs[def.Name] = list = new LinkedList<Definition>();
								list.AddLast(def);
							}
						}
					}

					_defs = keepDefs;
				}
			}
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

		public Token FindTokenOfType(Position pos, Type type)
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

		public Token FindNearbyTokenOfType(Position pos, Type type)
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

		public virtual IEnumerable<FunctionToken> LocalFunctions
		{
			get
			{
				if (this is GroupToken)
				{
					foreach (var token in (this as GroupToken).SubTokens)
					{
						foreach (var func in token.LocalFunctions)
						{
							yield return func;
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
		public IEnumerable<Token> FindDownward(Position pos)
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

		public IEnumerable<Token> FindDownwardTouching(Position pos)
		{
			if (!_span.Touching(pos)) yield break;

			if (this is GroupToken)
			{
				yield return this;

				foreach (var token in (this as GroupToken).SubTokens)
				{
					foreach (var token2 in token.FindDownwardTouching(pos))
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

		public IEnumerable<Token> FindDownward(int offset)
		{
			if (!_span.Contains(offset)) yield break;

			yield return this;

			if (this is GroupToken)
			{
				foreach (var token in (this as GroupToken).SubTokens)
				{
					foreach (var token2 in token.FindDownward(offset)) yield return token2;
				}
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

		public IEnumerable<Token> FindDownward(Position pos, Predicate<Token> pred)
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

		public IEnumerable<Token> FindDownward(int offset, Predicate<Token> pred)
		{
			if (!_span.Contains(offset)) yield break;
			if (pred(this)) yield return this;

			if (this is GroupToken)
			{
				foreach (var token in (this as GroupToken).SubTokens)
				{
					foreach (var token2 in token.FindDownward(offset, pred))
					{
						yield return token2;
					}
				}
			}
		}

		public IEnumerable<Token> FindDownwardTouching(Position pos, Predicate<Token> pred)
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
		public void AddDefinition(Definition def)
		{
			if (_defs == null)
			{
				_defs = new Dictionary<string, LinkedList<Definition>>();

				var list = new LinkedList<Definition>();
				_defs[def.Name] = list;
				list.AddFirst(def);
			}
			else
			{
				LinkedList<Definition> list;
				if (!_defs.TryGetValue(def.Name, out list))
				{
					list = new LinkedList<Definition>();
					_defs[def.Name] = list;
					list.AddFirst(def);
				}
				else if (!list.Contains(def))
				{
					list.AddFirst(def);
				}
			}
		}

		public void AddDefinitions(IEnumerable<Definition> defs)
		{
			foreach (var def in defs) AddDefinition(def);
		}

		public void CopyDefinitionsToToken(Token token, bool move)
		{
			if (_defs != null)
			{
				foreach (var defVal in _defs)
				{
					foreach (var def in defVal.Value.Reverse())	// Add in reverse order so they get pushed to the front of the list in proper order.
					{
						token.AddDefinition(def);
					}
				}
				if (move) _defs = null;
			}
		}

		public IEnumerable<Definition> GetDefinitions(string name)
		{
			if (_defs != null)
			{
				LinkedList<Definition> list;
				if (_defs.TryGetValue(name, out list))
				{
					foreach (var def in list) yield return def;
				}
			}

			if (_parent != null)
			{
				foreach (var def in _parent.GetDefinitions(name)) yield return def;
			}
		}

		public IEnumerable<T> GetDefinitions<T>(string name) where T: Definition
		{
			if (_defs != null)
			{
				LinkedList<Definition> list;
				if (_defs.TryGetValue(name, out list))
				{
					foreach (var def in list)
					{
						if (def is T) yield return def as T;
					}
				}
			}

			if (_parent != null)
			{
				foreach (var def in _parent.GetDefinitions<T>(name)) yield return def;
			}
		}

		public IEnumerable<Definition> GetDefinitions()
		{
			if (_defs != null)
			{
				foreach (var list in _defs.Values)
				{
					foreach (var def in list) yield return def;
				}
			}

			if (_parent != null)
			{
				foreach (var def in _parent.GetDefinitions()) yield return def;
			}
		}

		public IEnumerable<T> GetDefinitions<T>() where T : Definition
		{
			if (_defs != null)
			{
				foreach (var list in _defs.Values)
				{
					foreach (var def in list)
					{
						if (def is T) yield return def as T;
					}
				}
			}

			if (_parent != null)
			{
				foreach (var def in _parent.GetDefinitions<T>()) yield return def;
			}
		}

		public IEnumerable<Definition> GetDefinitionsAtThisLevel()
		{
			if (_defs != null)
			{
				foreach (var defList in _defs)
				{
					foreach (var def in defList.Value)
					{
						yield return def;
					}
				}
			}
		}

		public virtual IEnumerable<DefinitionLocation> GetDefinitionLocationsAtThisLevel()
		{
			foreach (var def in GetDefinitionsAtThisLevel())
			{
				yield return new DefinitionLocation(def, Span.Start.Offset);
			}
		}

		public Definition SourceDefinition
		{
			get { return _sourceDefinition; }
			set { _sourceDefinition = value; }
		}

		protected DefinitionProvider DefinitionProvider
		{
			get { return _scope.File.Model.DefinitionProvider; }
		}

		protected bool CreateDefinitions
		{
			get { return _scope.Preprocessor; }
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

		public LocalFilePosition LocalFilePosition
		{
			get
			{
				if (!_localFilePos.HasValue)
				{
					_localFilePos = _scope.File.CodeSource.GetFilePosition(Span.Start.Offset);
				}
				return _localFilePos.Value;
			}
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

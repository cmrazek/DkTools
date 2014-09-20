using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel.Tokens
{
	internal abstract class GroupToken : Token
	{
		private List<Token> _tokens = new List<Token>();
		private bool _isLocalScope;

		public GroupToken(GroupToken parent, Scope scope, int startPos)
			: base(parent, scope, new Span(startPos, startPos))
		{
		}

		public GroupToken(GroupToken parent, Scope scope, IEnumerable<Token> tokens)
			: base(parent, scope, new Span(tokens.First().Span.Start, tokens.Last().Span.End))
		{
			_tokens = tokens.ToList();
		}

		public override void DumpTreeInner(System.Xml.XmlWriter xml)
		{
			base.DumpTreeInner(xml);
			foreach (var token in _tokens) token.DumpTree(xml);
		}

		public IEnumerable<Token> SubTokens
		{
			get { return _tokens; }
		}

		public Token AddToken(Token child)
		{
#if DEBUG
			if (child == null) throw new ArgumentNullException();
#endif
			_tokens.Add(child);
			child.CommitToParentToken(this);
			Span = new Span(_tokens.First().Span.Start, _tokens.Last().Span.End);
			OnChildTokenAdded(child);
			return child;
		}

		protected virtual void OnChildTokenAdded(Token child)
		{
		}

		public void AddTokens(IEnumerable<Token> tokens)
		{
			foreach (var token in tokens) AddToken(token);
		}

		public bool IsLocalScope
		{
			get { return _isLocalScope; }
			protected set { _isLocalScope = value; }
		}

		public override string NormalizedText
		{
			get
			{
				return Token.GetNormalizedText(_tokens);
			}
		}

		public void ParseScope(Scope scope, Func<Token, ParseScopeResult> parseCallback)
		{
			while (true)
			{
				var token = scope.File.TryParseComplexToken(this, scope);
				if (token == null) return;

				switch (parseCallback(token))
				{
					case ParseScopeResult.Continue:
						AddToken(token);
						break;
					case ParseScopeResult.StopAndKeep:
						AddToken(token);
						return;
					case ParseScopeResult.StopAndReject:
						scope.File.Position = token.Span.Start;
						return;
				}
			}
		}

		public Token FindLastChildBeforeOffset(int pos)
		{
			return (from t in _tokens where t.Span.End <= pos select t).LastOrDefault();
		}

		public Token FindPreviousSibling(Token token)
		{
			var startOffset = token.Span.Start;
			return (from t in _tokens where t.Span.End <= token.Span.Start select t).LastOrDefault();
		}

		public Token FindNextSibling(Token token)
		{
			var endOffset = token.Span.End;
			return (from t in _tokens where t.Span.Start >= endOffset select t).FirstOrDefault();
		}
	}

	internal enum ParseScopeResult
	{
		/// <summary>
		/// This token will be added to the parent, and parsing will continue.
		/// </summary>
		Continue,

		/// <summary>
		/// This token will be added to the parent, but parsing will stop afterwards.
		/// </summary>
		StopAndKeep,

		/// <summary>
		/// This token will not be added to the parent, and parsing will stop.
		/// </summary>
		StopAndReject
	}
}

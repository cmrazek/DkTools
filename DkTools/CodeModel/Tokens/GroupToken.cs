using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel.Tokens
{
	internal abstract class GroupToken : Token
	{
		private List<Token> _tokens = new List<Token>();

		public GroupToken(Scope scope)
			: base(scope)
		{
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

			if (!child.Span.IsEmpty)
			{
				if (Span.IsEmpty) Span = child.Span;
				else Span = Span.Include(child.Span);
			}

			OnChildTokenAdded(child);
			return child;
		}

		public bool RemoveToken(Token child)
		{
			return _tokens.Remove(child);
		}

		protected override void OnSpanChanged()
		{
			if (Parent != null && !Span.IsEmpty)
			{
				if (Parent.Span.IsEmpty) Parent.Span = Span;
				else Parent.Span = Parent.Span.Include(Span);
			}
		}

		protected virtual void OnChildTokenAdded(Token child)
		{
		}

		public void AddTokens(IEnumerable<Token> tokens)
		{
			foreach (var token in tokens) AddToken(token);
		}

		public override string NormalizedText
		{
			get
			{
				return Token.GetNormalizedText(_tokens);
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel
{
	internal class ReplaceToken : GroupToken
	{
		private ReplaceStartToken _startToken;
		private ReplaceWithToken _withToken;
		private ReplaceEndToken _endToken;
		private List<Token> _oldTokens = new List<Token>();
		private List<Token> _newTokens = new List<Token>();

		private ReplaceToken(GroupToken parent, Scope scope, ReplaceStartToken startToken)
			: base(parent, scope, new Token[] { startToken })
		{
			_startToken = startToken;
			_startToken.ReplaceToken = this;
		}

		public static ReplaceToken Parse(GroupToken parent, Scope scope, ReplaceStartToken startToken)
		{
#if DEBUG
			if (startToken == null) throw new ArgumentNullException("replaceToken");
#endif

			var file = scope.File;

			var scopeIndent = scope.CloneIndent();

			var ret = new ReplaceToken(parent, scope, startToken);

			ret.ParseScope(scopeIndent, t =>
				{
					if (t is ReplaceWithToken)
					{
						ret._withToken = t as ReplaceWithToken;
						ret._withToken.ReplaceToken = ret;
						return ParseScopeResult.Continue;
					}
					if (t is ReplaceEndToken)
					{
						ret._endToken = t as ReplaceEndToken;
						ret._endToken.ReplaceToken = ret;
						return ParseScopeResult.StopAndKeep;
					}

					if (ret._withToken == null) ret._oldTokens.Add(t);
					else ret._newTokens.Add(t);
					return ParseScopeResult.Continue;
				});

			return ret;
		}

		public override bool BreaksStatement
		{
			get { return true; }
		}

		public ReplaceStartToken StartToken
		{
			get { return _startToken; }
		}

		public ReplaceWithToken WithToken
		{
			get { return _withToken; }
		}

		public ReplaceEndToken EndToken
		{
			get { return _endToken; }
		}
	}

	internal abstract class ReplaceBoundaryToken : Token, IBraceMatchingToken
	{
		private ReplaceToken _replaceToken;

		public ReplaceBoundaryToken(GroupToken parent, Scope scope, Span span)
			: base(parent, scope, span)
		{
			ClassifierType = Classifier.ProbeClassifierType.Preprocessor;
		}

		IEnumerable<Token> IBraceMatchingToken.BraceMatchingTokens
		{
			get
			{
				if (_replaceToken != null)
				{
					yield return _replaceToken.StartToken;
					if (_replaceToken.WithToken != null) yield return _replaceToken.WithToken;
					if (_replaceToken.EndToken != null) yield return _replaceToken.EndToken;
				}
			}
		}

		public override bool BreaksStatement
		{
			get { return true; }
		}

		public ReplaceToken ReplaceToken
		{
			get { return _replaceToken; }
			set { _replaceToken = value; }
		}
	}

	internal class ReplaceStartToken : ReplaceBoundaryToken
	{
		public ReplaceStartToken(GroupToken parent, Scope scope, Span span)
			: base(parent, scope, span)
		{
		}
	}

	internal class ReplaceWithToken : ReplaceBoundaryToken
	{
		public ReplaceWithToken(GroupToken parent, Scope scope, Span span)
			: base(parent, scope, span)
		{
		}
	}

	internal class ReplaceEndToken : ReplaceBoundaryToken
	{
		public ReplaceEndToken(GroupToken parent, Scope scope, Span span)
			: base(parent, scope, span)
		{
		}
	}
}

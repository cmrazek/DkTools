using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel.Tokens
{
	internal class ReplaceToken : GroupToken
	{
		private ReplaceStartToken _startToken;
		private ReplaceWithToken _withToken;
		private ReplaceEndToken _endToken;
		private List<Token> _oldTokens = new List<Token>();
		private List<Token> _newTokens = new List<Token>();

		private ReplaceToken(Scope scope, ReplaceStartToken startToken)
			: base(scope)
		{
			_startToken = startToken;
			_startToken.ReplaceToken = this;
			AddToken(startToken);
		}

		public static ReplaceToken Parse(Scope scope, ReplaceStartToken startToken)
		{
#if DEBUG
			if (startToken == null) throw new ArgumentNullException("replaceToken");
#endif
			var scopeIndent = scope.CloneIndent();

			var ret = new ReplaceToken(scope, startToken);

			var done = false;
			while (!done && !scope.Code.EndOfFile)
			{
				var stmt = StatementToken.TryParse(scopeIndent, t =>
				{
					if (t is ReplaceEndToken)
					{
						ret._endToken = t as ReplaceEndToken;
						ret._endToken.ReplaceToken = ret;
						done = true;
					}
					else if (ret._withToken == null)
					{
						if (t is ReplaceWithToken)
						{
							ret._withToken = t as ReplaceWithToken;
							ret._withToken.ReplaceToken = ret;
						}
						else
						{
							ret._oldTokens.Add(t);
						}
					}
					else
					{
						ret._newTokens.Add(t);
					}
				});
			}

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

		public ReplaceBoundaryToken(Scope scope, Span span)
			: base(scope, span)
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
		public ReplaceStartToken(Scope scope, Span span)
			: base(scope, span)
		{
		}
	}

	internal class ReplaceWithToken : ReplaceBoundaryToken
	{
		public ReplaceWithToken(Scope scope, Span span)
			: base(scope, span)
		{
		}
	}

	internal class ReplaceEndToken : ReplaceBoundaryToken
	{
		public ReplaceEndToken(Scope scope, Span span)
			: base(scope, span)
		{
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DkTools.CodeModel.Definitions;

namespace DkTools.CodeModel
{
	internal class DefineToken : GroupToken
	{
		private IdentifierToken _nameToken;
		private BracketsToken _argsToken;   // Optional
		private Token[] _bodyTokens;
		private DataType _dataType;

		private DefineToken(GroupToken parent, Scope scope, Position startPos)
			: base(parent, scope, startPos)
		{
			IsLocalScope = true;
		}

		private static Regex _rxBraceMacro = new Regex(@".*\{\s*$");

		public static DefineToken TryParse(GroupToken parent, Scope scope)
		{
			var file = scope.File;

			if (!file.SkipWhiteSpaceAndComments(scope) || file.PeekChar() != '#') return null;
			var startPos = file.Position;
			if (!file.SkipMatch("#define")) return null;
			var prepToken = new PreprocessorToken(parent, scope, new Span(startPos, file.Position), "#define");

			return Parse(parent, scope, prepToken);
		}

		public static DefineToken Parse(GroupToken parent, Scope scope, PreprocessorToken prepToken)
		{
			var file = scope.File;
			var startPos = prepToken.Span.Start;

			var defineScope = scope;
			defineScope.Hint |= ScopeHint.SuppressFunctionDefinition | ScopeHint.SuppressVarDecl | ScopeHint.SuppressFunctionCall;

			var ret = new DefineToken(parent, defineScope, startPos);
			ret.AddToken(prepToken);

			// Name
			if (!file.SkipWhiteSpaceAndComments(defineScope) || file.Position.LineNum != startPos.LineNum) return ret;
			var nameToken = IdentifierToken.TryParse(parent, defineScope);
			if (nameToken == null) return ret;
			ret.AddToken(ret._nameToken = nameToken);

			var bodyTokens = new List<Token>();

			// Arguments
			if (!file.SkipWhiteSpaceAndComments(defineScope) || file.Position.LineNum != startPos.LineNum) return ret;
			var bracketsToken = BracketsToken.TryParse(parent, defineScope);
			if (bracketsToken != null)
			{
				ret.AddToken(bracketsToken);
				if (bracketsToken.Span.Start == nameToken.Span.End) ret._argsToken = bracketsToken;
				else bodyTokens.Add(bracketsToken);
			}

			var lineNum = startPos.LineNum;

			while (file.SkipWhiteSpaceAndComments(defineScope) && file.Position.LineNum == lineNum)
			{
				var ch = file.PeekChar();
				if (ch == '\\')
				{
					lineNum++;
					ret.AddToken(new UnknownToken(ret, defineScope, file.MoveNextSpan(), "\\"));
				}
				else
				{
					var token = file.TryParseComplexToken(ret, defineScope);
					if (token != null)
					{
						ret.AddToken(token);
						bodyTokens.Add(token);
					}
				}
			}

			ret._bodyTokens = bodyTokens.ToArray();

			if (ret._nameToken != null)
			{
				if (ret._argsToken == null)
				{
					var isDataType = false;

					if (ret._bodyTokens.Length == 1)
					{
						var bodyToken = ret._bodyTokens.First();
						if (bodyToken is IDataTypeToken)
						{
							var dataType = (bodyToken as IDataTypeToken).DataType;
							if (dataType != null)
							{
								// Rebrand this data type with the new name.
								ret._dataType = new DataType(ret._nameToken.Text, dataType.CompletionOptions, dataType.InfoText);

								if (scope.Preprocessor)
								{
									var def = new DataTypeDefinition(scope, ret._nameToken.Text, ret._nameToken, ret._dataType);
									ret.AddDefinition(def);
									ret._nameToken.SourceDefinition = def;
								}
								isDataType = true;
							}
						}
					}

					if (!isDataType && scope.Preprocessor)
					{
						var def = new ConstantDefinition(scope, ret._nameToken.Text, ret._nameToken, Token.GetNormalizedText(ret._bodyTokens));
						ret.AddDefinition(def);
						ret._nameToken.SourceDefinition = def;
					}
				}
				else // args is not null
				{
					var bodyText = Token.GetNormalizedText(ret._bodyTokens);
					if (scope.Preprocessor)
					{
						var def = new MacroDefinition(scope, ret._nameToken.Text, ret._nameToken, ret.GetFunctionSignature(), bodyText);
						ret.AddDefinition(def);
						ret._nameToken.SourceDefinition = def;
					}
				}
			}

			return ret;
		}

		public override bool BreaksStatement
		{
			get
			{
				return true;
			}
		}

		private string GetFunctionSignature()
		{
#if DEBUG
			if (_nameToken == null) throw new InvalidOperationException("Name token is null");
			if (_argsToken == null) throw new InvalidOperationException("Args is null");
#endif
			var sig = new StringBuilder();
			sig.Append("#define");
			sig.Append(" ");
			sig.Append(_nameToken.Text);
			sig.Append(" ");
			sig.Append(_argsToken.NormalizedText);

			return sig.ToString();
		}

		public override IEnumerable<OutliningRegion> OutliningRegions
		{
			get
			{
				if (_bodyTokens != null && _bodyTokens.Length == 1 && _bodyTokens[0] is BracesToken)
				{
					var bodyToken = _bodyTokens[0] as BracesToken;
					yield return new OutliningRegion
					{
						Span = bodyToken.Span,
						CollapseToDefinition = true,
						Text = Constants.DefaultOutliningText,
						TooltipText = File.GetRegionText(bodyToken.Span)
					};
				}

				foreach (var reg in base.OutliningRegions) yield return reg;
			}
		}
	}
}

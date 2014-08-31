using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DkTools.CodeModel.Definitions;

namespace DkTools.CodeModel
{
	internal class FunctionToken : GroupToken
	{
		private Token _dataTypeToken;
		private IdentifierToken _nameToken;
		private BracketsToken _argsToken;
		private BracesToken _bodyToken;

		private FunctionToken(GroupToken parent, Scope scope, DataTypeToken dataTypeToken, IdentifierToken nameToken, BracketsToken argsToken)
			: base(parent, scope, dataTypeToken != null ? dataTypeToken.Span.Start : nameToken.Span.Start)
		{
#if DEBUG
			if (nameToken == null || argsToken == null) throw new ArgumentNullException();
#endif
			IsLocalScope = true;

			if (dataTypeToken != null) AddToken(_dataTypeToken = dataTypeToken);
			AddToken(_nameToken = nameToken);
			AddToken(_argsToken = argsToken);

			if (scope.Preprocessor)
			{
				var def = new FunctionDefinition(scope, _nameToken.Text, _nameToken, _dataTypeToken != null ? DataType.FromToken(_dataTypeToken) : DataType.Int, GetSignature());
				_nameToken.SourceDefinition = def;
				AddDefinition(def);
			}
			else
			{
				var def = GetDefinitions<FunctionDefinition>(_nameToken.Text).FirstOrDefault();
				if (def != null) _nameToken.SourceDefinition = def;
			}
		}

		private void AddBodyToken(BracesToken bodyToken)
		{
			// Copying the definitions before, then adding after will allow the global definitions (the function) to be copied upwards.
			//CopyDefinitionsToToken(bodyToken, true);
			AddToken(bodyToken);
			_bodyToken = bodyToken;
		}

		public override IEnumerable<FunctionToken> LocalFunctions
		{
			get
			{
				return new FunctionToken[] { this };
			}
		}

		public string Name
		{
			get { return _nameToken.Text; }
		}

		public override bool BreaksStatement
		{
			get
			{
				return true;
			}
		}

		public static FunctionToken TryParse(GroupToken parent, Scope scope)
		{
			if (scope.Hint.HasFlag(ScopeHint.SuppressFunctionDefinition)) return null;

			var file = scope.File;
			file.SkipWhiteSpaceAndComments(scope);
			var resetPos = file.Position;

			// Optional data type
			var dataTypeToken = DkTools.CodeModel.DataTypeToken.TryParse(parent, scope);

			// Function name
			var nameToken = IdentifierToken.TryParse(parent, scope);

			file.SkipWhiteSpaceAndComments(scope);
			if (file.PeekChar() != '(') { file.Position = resetPos; return null; }
			if (nameToken == null)
			{
				if (dataTypeToken != null && dataTypeToken.Text.IsWord())
				{
					// Incorrectly parsed the name token as a data type.
					nameToken = new IdentifierToken(parent, scope, dataTypeToken.Span, dataTypeToken.Text);
					dataTypeToken = null;
				}
				else { file.Position = resetPos; return null; }
			}

			// Arguments
			var argsScope = scope.Clone();
			argsScope.Hint |= ScopeHint.FunctionArgs | ScopeHint.SuppressVars;
			var argsToken = BracketsToken.Parse(parent, argsScope);

			var ret = new FunctionToken(parent, scope, dataTypeToken, nameToken, argsToken);

			file.SkipWhiteSpaceAndComments(scope);
			if (file.PeekChar() != '{')
			{
				// Attributes before body
				var attribScope = scope.Clone();
				attribScope.Hint |= ScopeHint.SuppressFunctionDefinition | ScopeHint.NotOnRoot | ScopeHint.SuppressFunctionCall |
					ScopeHint.SuppressVarDecl | ScopeHint.SuppressVars | ScopeHint.SuppressControlStatements;

				var broken = false;
				ret.ParseScope(attribScope, t =>
					{
						if (t.BreaksStatement)
						{
							broken = true;
							return ParseScopeResult.StopAndReject;
						}

						// Stop on the last token before the body.
						file.SkipWhiteSpaceAndComments(attribScope);
						if (file.PeekChar() == '{') return ParseScopeResult.StopAndKeep;

						return ParseScopeResult.Continue;
					});
				if (broken) { file.Position = resetPos; return null; }
			}

			// Body
			var bodyScope = scope;
			scope.Hint |= ScopeHint.SuppressFunctionDefinition | ScopeHint.NotOnRoot;
			var bodyToken = BracesToken.TryParse(ret, bodyScope);
			if (bodyToken == null) { file.Position = resetPos; return null; }
			else ret.AddBodyToken(bodyToken);

			return ret;
		}

		private string GetSignature()
		{
			var sb = new StringBuilder();
			if (_dataTypeToken != null)
			{
				sb.Append(_dataTypeToken.NormalizedText);
				sb.Append(" ");
			}
			sb.Append(_nameToken.Text);
			sb.Append(_argsToken.NormalizedText);
			return sb.ToString();
		}

		public Token DataTypeToken
		{
			get { return _dataTypeToken; }
		}

		public override IEnumerable<OutliningRegion> OutliningRegions
		{
			get
			{
				foreach (var region in base.OutliningRegions) yield return region;

				if (_bodyToken != null)
				{
					yield return new OutliningRegion
					{
						Span = new Span(_argsToken.Span.End, _bodyToken.Span.End),
						CollapseToDefinition = true,
						Text = Constants.DefaultOutliningText,
						TooltipText = File.GetRegionText(Span)
					};
				}
			}
		}

		public override IEnumerable<DefinitionLocation> GetDefinitionLocationsAtThisLevel()
		{
			foreach (var def in GetDefinitionsAtThisLevel())
			{
				if (_bodyToken != null)
				{
					yield return new DefinitionLocation(def, _bodyToken.Span.Start.Offset);
				}

				if (def is VariableDefinition && (def as VariableDefinition).Argument)
				{
					yield return new DefinitionLocation(def, _argsToken.Span.Start.Offset);
				}
			}
		}
	}
}

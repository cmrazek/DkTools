using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DkTools.CodeModel.Definitions;

namespace DkTools.CodeModel
{
	internal class ExternFunctionToken : GroupToken
	{
		private Token _dataTypeToken;   // Optional
		private IdentifierToken _nameToken;
		private BracketsToken _argsToken;

		public ExternFunctionToken(GroupToken parent, Scope scope, IEnumerable<Token> tokens, Token dataTypeToken, IdentifierToken nameToken, BracketsToken argsToken)
			: base(parent, scope, tokens)
		{
			_dataTypeToken = dataTypeToken;
			_nameToken = nameToken;
			_argsToken = argsToken;

			if (scope.Preprocessor)
			{
				var def = new FunctionDefinition(scope, _nameToken.Text, _nameToken, DataType.FromToken(_dataTypeToken), GetFunctionSignature(), argsToken.Span.End, Position.Start, FunctionPrivacy.Public, true);
				AddDefinition(def);
				_nameToken.SourceDefinition = def;
			}
			else
			{
				foreach (var def in parent.GetDefinitions<FunctionDefinition>(_nameToken.Text))
				{
					_nameToken.SourceDefinition = def;
				}
			}
		}

		private string GetFunctionSignature()
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
	}
}

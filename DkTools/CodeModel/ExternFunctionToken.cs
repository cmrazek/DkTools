﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

			var def = new FunctionDefinition(_nameToken.Text, _nameToken, DataType.FromToken(_dataTypeToken), GetFunctionSignature());
			AddDefinition(def);
			_nameToken.SourceDefinition = def;
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

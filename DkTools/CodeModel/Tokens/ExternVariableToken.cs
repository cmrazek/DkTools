// TODO: remove

//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using DkTools.CodeModel.Definitions;

//namespace DkTools.CodeModel.Tokens
//{
//	internal class ExternVariableToken : GroupToken
//	{
//		private Token _dataTypeToken;   // Optional
//		private WordToken[] _nameTokens;

//		public ExternVariableToken(GroupToken parent, Scope scope, IEnumerable<Token> tokens, Token dataTypeToken, IEnumerable<WordToken> nameTokens)
//			: base(parent, scope, tokens)
//		{
//			_dataTypeToken = dataTypeToken;
//			_nameTokens = nameTokens.ToArray();

//			if (scope.Preprocessor)
//			{
//				foreach (var tok in _nameTokens)
//				{
//					var def = new VariableDefinition(scope, tok.Text, tok, DataType.FromToken(_dataTypeToken), false);
//					tok.SourceDefinition = def;
//					AddDefinition(def);
//				}
//			}
//		}

//		public IEnumerable<Token> NameTokens
//		{
//			get { return _nameTokens; }
//		}
//	}
//}

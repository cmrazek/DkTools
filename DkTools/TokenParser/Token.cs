using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.TokenParser
{
	internal struct Token
	{
		private string _text;
		private TokenType _type;
		private Position _startPos;
		private Position _endPos;

		public Token(TokenType type, string text, Position startPos, Position endPos)
		{
			_type = type;
			_text = text;
			_startPos = startPos;
			_endPos = endPos;
		}

		public TokenType Type
		{
			get { return _type; }
		}

		public string Text
		{
			get { return _text; }
		}

		public Position StartPosition
		{
			get { return _startPos; }
		}

		public Position EndPosition
		{
			get { return _endPos; }
		}
	}
}

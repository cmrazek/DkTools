using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.TokenParser
{
	internal sealed class ParserEnumerator : IEnumerator<Token>
	{
		private Parser _parser;
		private int _pos;
		private Token _token;
		private bool _readNestable;

		public ParserEnumerator(Parser parser, bool readNestable)
		{
			_parser = parser;
			_readNestable = readNestable;
		}

		public Token Current
		{
			get
			{
				return _token;
			}
		}

		object IEnumerator.Current
		{
			get
			{
				return _token;
			}
		}

		bool IEnumerator.MoveNext()
		{
			_parser.Position = _pos;
			if (_readNestable)
			{
				if (!_parser.ReadNestable()) return false;
			}
			else
			{
				if (!_parser.Read()) return false;
			}
			_token = _parser.Token;
			_pos = _parser.Position;
			return true;
		}

		void IEnumerator.Reset()
		{
			_pos = 0;
		}

		void IDisposable.Dispose()
		{
		}
	}
}

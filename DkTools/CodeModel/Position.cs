using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.CodeModel
{
	public struct Position
	{
		private int _offset;
		private int _lineNum;
		private int _linePos;

		public static readonly Position Start = new Position(0, 0, 0);

		public Position(int offset, int lineNum, int linePos)
		{
			_offset = offset;
			_lineNum = lineNum;
			_linePos = linePos;
		}

		public int Offset
		{
			get { return _offset; }
		}

		/// <summary>
		/// Gets the zero-based line number.
		/// </summary>
		public int LineNum
		{
			get { return _lineNum; }
		}

		/// <summary>
		/// Gets the zero-based line position.
		/// </summary>
		public int LinePos
		{
			get { return _linePos; }
		}

		public override bool Equals(object obj)
		{
			if (obj == null || obj.GetType() != typeof(Position)) return false;
			return ((Position)obj)._offset == _offset;
		}

		public override int GetHashCode()
		{
			return _offset;
		}

		public static bool operator ==(Position a, Position b)
		{
			return a._offset == b._offset;
		}

		public static bool operator !=(Position a, Position b)
		{
			return a._offset != b._offset;
		}

		public static bool operator <(Position a, Position b)
		{
			return a._offset < b._offset;
		}

		public static bool operator <=(Position a, Position b)
		{
			return a._offset <= b._offset;
		}

		public static bool operator >(Position a, Position b)
		{
			return a._offset > b._offset;
		}

		public static bool operator >=(Position a, Position b)
		{
			return a._offset >= b._offset;
		}

		public override string ToString()
		{
			return string.Format("Line: {0} Ch: {1} Offset: {2}", _lineNum, _linePos, _offset);
		}

		public Position CalcNext(char thisChar)
		{
			if (thisChar == '\n') return new Position(_offset + 1, _lineNum + 1, 0);
			else return new Position(_offset + 1, _lineNum, _linePos + 1);
		}

		public static Position Calc(string text, int start, int offset)
		{
			var lineNum = 0;
			var linePos = 0;

			var off = start;
			while (off < offset)
			{
				if (text[off++] == '\n')
				{
					lineNum++;
					linePos = 0;
				}
				else
				{
					linePos++;
				}
			}

			return new Position(offset, lineNum, linePos);
		}

		public static Position Calc(string text, int offset)
		{
			return Position.Calc(text, 0, offset);
		}

		public Position Advance(string text, int start, int length)
		{
			var lineNum = _lineNum;
			var linePos = _linePos;
			var offset = _offset;

			var end = start + length;
			if (end > text.Length) end = text.Length;

			for (var i = start; i < end; i++)
			{
				if (text[i] == '\n')
				{
					lineNum++;
					linePos = 0;
				}
				else
				{
					linePos++;
				}
				offset++;
			}

			return new Position(offset, lineNum, linePos);
		}

		public Position Advance(string text)
		{
			return Advance(text, 0, text.Length);
		}

		public Position Advance(char ch)
		{
			if (ch == '\n') return new Position(_offset + 1, _lineNum + 1, 0);
			return new Position(_offset + 1, _lineNum, _linePos + 1);
		}

		public Position MoveOffset(int diff)
		{
			return new Position(_offset + diff, _lineNum, _linePos);
		}
	}
}

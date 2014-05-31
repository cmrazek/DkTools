using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools.TokenParser
{
	internal struct Position
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

		public int LineNum
		{
			get { return _lineNum; }
		}

		public int LinePos
		{
			get { return _linePos; }
		}

		public CodeModel.Position ToCodeModelPosition()
		{
			return new CodeModel.Position(_offset, _lineNum, _linePos);
		}
	}
}

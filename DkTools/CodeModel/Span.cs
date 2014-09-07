using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VsText = Microsoft.VisualStudio.Text;

namespace DkTools.CodeModel
{
	public struct Span
	{
		private Position _start;
		private Position _end;

		public static readonly Span Empty = new Span(Position.Start, Position.Start);

		public Span(Position start, Position end)
		{
			_start = start;
			_end = end;
		}

		public Position Start
		{
			get { return _start; }
		}

		public Position End
		{
			get { return _end; }
		}

		public int Length
		{
			get { return _end.Offset - _start.Offset; }
		}

		public override string ToString()
		{
			return string.Format("[{0}] - [{1}]", _start, _end);
		}

		public bool Contains(Position pos)
		{
			return _start <= pos && _end > pos;
		}

		public bool Contains(int offset)
		{
			return _start.Offset <= offset && _end.Offset > offset;
		}

		public bool Touching(Position pos)
		{
			return _start <= pos && _end >= pos;
		}

		public bool Touching(int offset)
		{
			return _start.Offset <= offset && _end.Offset >= offset;
		}

		public bool Intersects(Span span)
		{
			return span._end > _start && span._start < _end;
		}

		public bool Intersects(int start, int length)
		{
			return (start + length) > _start.Offset && start < _end.Offset;
		}

		public Microsoft.VisualStudio.TextManager.Interop.TextSpan ToVsTextInteropSpan()
		{
			return new Microsoft.VisualStudio.TextManager.Interop.TextSpan()
			{
				iStartLine = _start.LineNum,
				iStartIndex = _start.LinePos,
				iEndLine = _end.LineNum,
				iEndIndex = _end.LinePos
			};
		}

		public Microsoft.VisualStudio.Text.Span ToVsTextSpan()
		{
			return new Microsoft.VisualStudio.Text.Span(_start.Offset, _end.Offset - _start.Offset);
		}

		public override bool Equals(object obj)
		{
			if (obj == null || obj.GetType() != typeof(Span)) return false;
			return _start.Equals(((Span)obj)._start) && _end.Equals(((Span)obj)._end);
		}

		public override int GetHashCode()
		{
			uint end = (uint)_end.Offset;
			return _start.Offset ^ ((int)((end << 16) & 0xffff0000) | (int)((end >> 16) & 0x0000ffff));
		}

		public Span MoveOffset(int offset)
		{
			return new Span(_start.MoveOffset(offset), _end.MoveOffset(offset));
		}

		public string SaveString
		{
			get
			{
				return string.Concat(_start.Offset, ", ", _start.LineNum, ", ", _start.LinePos, ", ", _end.Offset, ", ", _end.LineNum, ", ", _end.LinePos);
			}
		}

		public static Span FromSaveString(string str)
		{
			var coords = str.Split(',');
			if (coords.Length != 6) return Span.Empty;
			return new Span(new Position(int.Parse(coords[0].Trim()), int.Parse(coords[1].Trim()), int.Parse(coords[2].Trim())),
				new Position(int.Parse(coords[3].Trim()), int.Parse(coords[4].Trim()), int.Parse(coords[5].Trim())));
		}
	}
}

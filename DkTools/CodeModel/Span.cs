using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VsText = Microsoft.VisualStudio.Text;

namespace DkTools.CodeModel
{
	public struct Span
	{
		private int _start;
		private int _end;

		public static readonly Span Empty = new Span(0, 0);

		public Span(int start, int end)
		{
			_start = start;
			_end = end;
		}

		public Span(Span startSpan, Span endSpan)
		{
			_start = startSpan._start;
			_end = endSpan._end;
		}

		public int Start
		{
			get { return _start; }
		}

		public int End
		{
			get { return _end; }
		}

		public int Length
		{
			get { return _end - _start; }
		}

		public override string ToString()
		{
			return string.Format("[{0}..{1}]", _start, _end);
		}

		public bool Contains(int pos)
		{
			return _start <= pos && _end > pos;
		}

		public bool Touching(int pos)
		{
			return _start <= pos && _end >= pos;
		}

		public bool Intersects(Span span)
		{
			return span._end > _start && span._start < _end;
		}

		public bool Intersects(int start, int length)
		{
			return (start + length) > _start && start < _end;
		}

		public Microsoft.VisualStudio.TextManager.Interop.TextSpan ToVsTextInteropSpan(Microsoft.VisualStudio.TextManager.Interop.IVsTextView view)
		{
			if (_end > _start)
			{
				int startLine, startPos, endLine, endPos;
				view.GetLineAndColumn(_start, out startLine, out startPos);
				view.GetLineAndColumn(_end, out endLine, out endPos);
				return new Microsoft.VisualStudio.TextManager.Interop.TextSpan()
				{
					iStartLine = startLine,
					iStartIndex = startPos,
					iEndLine = endLine,
					iEndIndex = endPos
				};
			}
			else
			{
				int startLine, startPos;
				view.GetLineAndColumn(_start, out startLine, out startPos);
				return new Microsoft.VisualStudio.TextManager.Interop.TextSpan()
				{
					iStartLine = startLine,
					iStartIndex = startPos,
					iEndLine = startLine,
					iEndIndex = startPos
				};
			}
		}

		public Microsoft.VisualStudio.Text.Span ToVsTextSpan()
		{
			return new Microsoft.VisualStudio.Text.Span(_start, _end - _start);
		}

		public override bool Equals(object obj)
		{
			if (obj == null || obj.GetType() != typeof(Span)) return false;
			return _start.Equals(((Span)obj)._start) && _end.Equals(((Span)obj)._end);
		}

		public override int GetHashCode()
		{
			uint end = (uint)_end;
			return _start ^ ((int)((end << 16) & 0xffff0000) | (int)((end >> 16) & 0x0000ffff));
		}

		public string SaveString
		{
			get
			{
				return string.Concat(_start, ", ", _end);
			}
		}

		public static Span FromSaveString(string str)
		{
			var coords = str.Split(',');
			if (coords.Length == 2)
			{
				return new Span(int.Parse(coords[0].Trim()), int.Parse(coords[1].Trim()));
			}
			else if (coords.Length == 6)
			{
				// For backward compatibility with old span strings.
				return new Span(int.Parse(coords[0].Trim()), int.Parse(coords[3].Trim()));
			}
			else return Span.Empty;
		}
	}
}

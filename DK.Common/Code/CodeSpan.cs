using System.Collections.Generic;

namespace DK.Code
{
	public struct CodeSpan
	{
		private int _start;
		private int _end;

		public static readonly CodeSpan Empty = new CodeSpan(0, 0);

		public CodeSpan(int start, int end)
		{
			_start = start;
			_end = end;
		}

		public CodeSpan(CodeSpan startSpan, CodeSpan endSpan)
		{
			_start = startSpan._start;
			_end = endSpan._end;
		}

		public int Start
		{
			get { return _start; }
			set { _start = value; }
		}

		public int End
		{
			get { return _end; }
			set { _end = value; }
		}

		public int Length
		{
			get { return _end - _start; }
		}

		public override string ToString()
		{
			return string.Format("[{0}..{1}]", _start, _end);
		}

		public static CodeSpan operator +(CodeSpan span, int offset) => new CodeSpan(span.Start + offset, span.End + offset);

		public bool Contains(int pos)
		{
			return _start <= pos && _end > pos;
		}

		public bool Touching(int pos)
		{
			return _start <= pos && _end >= pos;
		}

		public bool Intersects(CodeSpan span)
		{
			return span._end > _start && span._start < _end;
		}

		public bool Intersects(int start, int length)
		{
			return (start + length) > _start && start < _end;
		}

		public override bool Equals(object obj)
		{
			if (obj == null || obj.GetType() != typeof(CodeSpan)) return false;
			return _start.Equals(((CodeSpan)obj)._start) && _end.Equals(((CodeSpan)obj)._end);
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

		public static CodeSpan FromSaveString(string str)
		{
			var coords = str.Split(',');
			if (coords.Length == 2)
			{
				return new CodeSpan(int.Parse(coords[0].Trim()), int.Parse(coords[1].Trim()));
			}
			else if (coords.Length == 6)
			{
				// For backward compatibility with old span strings.
				return new CodeSpan(int.Parse(coords[0].Trim()), int.Parse(coords[3].Trim()));
			}
			else return CodeSpan.Empty;
		}

		public bool IsEmpty
		{
			get { return _end <= _start; }
		}

		public CodeSpan Include(CodeSpan other)
		{
			return new CodeSpan(_start < other._start ? _start : other._start, _end > other._end ? _end : other._end);
		}

		public static bool operator == (CodeSpan a, CodeSpan b)
		{
			return a._start == b._start && a._end == b._end;
		}

		public static bool operator != (CodeSpan a, CodeSpan b)
		{
			return a._start != b._start || a._end != b._end;
		}

		public CodeSpan Offset(int offset)
		{
			return new CodeSpan(_start + offset, _end + offset);
		}

		public CodeSpan Envelope(CodeSpan span)
		{
			return new CodeSpan(
				_start < span._start ? _start : span._start,
				_end > span._end ? _end : span._end);
		}

        public static CodeSpan Envelope(IEnumerable<CodeSpan> spans)
        {
            var first = true;
            var fullSpan = CodeSpan.Empty;

            foreach (var span in spans)
            {
                if (first)
                {
                    first = false;
                    fullSpan = span;
                }
                else
                {
                    fullSpan = fullSpan.Envelope(span);
                }
            }

            return fullSpan;
        }

		public CodeSpan Intersection(CodeSpan span)
		{
			var start = _start > span._start ? _start : span._start;
			var end = _end < span._end ? _end : span._end;
			if (start > end) return CodeSpan.Empty;
			return new CodeSpan(start, end);
		}
	}
}

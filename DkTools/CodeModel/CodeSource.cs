using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VsText = Microsoft.VisualStudio.Text;

namespace DkTools.CodeModel
{
	public class CodeSource
	{
		private StringBuilder _buf = new StringBuilder();
		private string _text = null;
		private List<CodeSegment> _segments = new List<CodeSegment>();
		private CodeSegment.CodeSegmentComparer _codeSegmentComparer = new CodeSegment.CodeSegmentComparer();
		private VsText.ITextSnapshot _snapshot;

		private class CodeSegment
		{
			public string fileName;
			public int start;
			public Position pos;

			public CodeSegment(string fileName, int start, Position pos)
			{
				this.fileName = fileName;
				this.start = start;
				this.pos = pos;
			}

			public class CodeSegmentComparer : IComparer<CodeSegment>
			{
				public int Compare(CodeSegment a, CodeSegment b)
				{
					return a.start.CompareTo(b.start);
				}
			}
		}

		public void Append(string fileName, Position filePos, string text)
		{
			if (text.Length > 0)
			{
				var start = _buf.Length;

				var lastSeg = _segments.LastOrDefault();
				var lastSegLength = lastSeg != null ? _buf.Length - lastSeg.start : 0;

				_buf.Append(text);

				if (lastSeg == null ||
					!lastSeg.fileName.Equals(fileName, StringComparison.OrdinalIgnoreCase) ||
					lastSeg.pos.Offset + lastSegLength != filePos.Offset)
				{
					_segments.Add(new CodeSegment(fileName, start, filePos));
				}

				_text = null;
			}
		}

		public void Insert(int insertOffset, string fileName, Position filePos, string text)
		{
			// Split the segment that comes before this one.
			var lastSegBefore = (from s in _segments where s.start < insertOffset select s).LastOrDefault();
			if (lastSegBefore != null)
			{
				var segSplitPos = lastSegBefore.pos.Advance(_buf.ToString(lastSegBefore.start, insertOffset - lastSegBefore.start));
				_segments.Add(new CodeSegment(lastSegBefore.fileName, insertOffset, segSplitPos));
			}

			// Bump all the segments that appear after this one.
			foreach (var seg in (from s in _segments where s.start >= insertOffset select s)) seg.start += text.Length;

			// Insert this new one.
			_segments.Add(new CodeSegment(fileName, insertOffset, filePos));
			_text.Insert(insertOffset, text);

			// Restore the sorting.
			_segments.Sort(_codeSegmentComparer);

			_text = null;
		}

		public string Text
		{
			get
			{
				if (_text == null) _text = _buf.ToString();
				return _text;
			}
		}

		public void GetFilePosition(int sourceOffset, out string foundFileName, out Position foundPos)
		{
			if (sourceOffset < 0 || sourceOffset > _text.Length) throw new ArgumentOutOfRangeException("offset");

			var seg = (from s in _segments where s.start <= sourceOffset select s).Last();
			var offset = seg.start;

			var pos = seg.pos;
			while (offset < sourceOffset) pos = pos.CalcNext(_text[offset++]);

			foundFileName = seg.fileName;
			foundPos = pos;
		}

		public void GetFileSpan(Span sourceSpan, out string foundFileName, out Span foundSpan)
		{
			string startFileName, endFileName;
			Position startPos, endPos;
			GetFilePosition(sourceSpan.Start.Offset, out startFileName, out startPos);
			GetFilePosition(sourceSpan.End.Offset, out endFileName, out endPos);

			if (startFileName.Equals(endFileName, StringComparison.OrdinalIgnoreCase) && endPos.Offset >= startPos.Offset)
			{
				foundFileName = startFileName;
				foundSpan = new Span(startPos, endPos);
			}
			else
			{
				foundFileName = startFileName;
				foundSpan = new Span(startPos, startPos);
			}
		}

#if DEBUG
		public string Dump()
		{
			var sb = new StringBuilder();
			
			for (var segIndex = 0; segIndex < _segments.Count; segIndex++)
			{
				var seg = _segments[segIndex];
				sb.AppendFormat("SEGMENT {0} FileName: {1} Pos: {2} [", segIndex, seg.fileName, seg.pos);

				var end = segIndex + 1 < _segments.Count ? _segments[segIndex + 1].start : _buf.Length;
				sb.Append(_buf.ToString(seg.start, end - seg.start));

				sb.Append("]");
			}

			return sb.ToString();
		}
#endif

		public VsText.ITextSnapshot Snapshot
		{
			get { return _snapshot; }
			set { _snapshot = value; }
		}
	}
}

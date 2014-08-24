using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VsText = Microsoft.VisualStudio.Text;

namespace DkTools.CodeModel
{
	internal class CodeSource : IPreprocessorWriter
	{
		private StringBuilder _buf = new StringBuilder();
		private string _text = null;
		private List<CodeSegment> _segments = new List<CodeSegment>();
		private CodeSegment.CodeSegmentComparer _codeSegmentComparer = new CodeSegment.CodeSegmentComparer();
		private VsText.ITextSnapshot _snapshot;
		private int _version;

		private class CodeSegment
		{
			public string fileName;
			public int start;
			public Position pos;
			public bool actualContent;

			public CodeSegment(string fileName, int start, Position pos, bool actualContent)
			{
				this.fileName = fileName;
				this.start = start;
				this.pos = pos;
				this.actualContent = actualContent;
			}

			public class CodeSegmentComparer : IComparer<CodeSegment>
			{
				public int Compare(CodeSegment a, CodeSegment b)
				{
					return a.start.CompareTo(b.start);
				}
			}
		}

		public void Append(string fileName, Position filePos, string text, bool actualContent)
		{
			Append(text, new CodeAttributes(fileName, filePos, actualContent));
		}

		public void Append(string text, CodeAttributes att)
		{
			var start = _buf.Length;

			var lastSeg = _segments.LastOrDefault();
			var lastSegLength = lastSeg != null ? _buf.Length - lastSeg.start : 0;

			_buf.Append(text);

			if (lastSeg == null ||
				!lastSeg.fileName.Equals(att.FileName, StringComparison.OrdinalIgnoreCase) ||
				lastSeg.pos.Offset + lastSegLength != att.FilePosition.Offset ||
				lastSeg.actualContent != att.ActualContent)
			{
				_segments.Add(new CodeSegment(att.FileName, start, att.FilePosition, att.ActualContent));
			}

			_text = null;
			_version++;
		}

		public void Append(CodeSource source)
		{
			for (int segIndex = 0, numSegs = source._segments.Count; segIndex < numSegs; segIndex++)
			{
				var seg = source._segments[segIndex];
				var length = (segIndex + 1 < numSegs ? source._segments[segIndex + 1].start : source._buf.Length) - seg.start;

				Append(seg.fileName, seg.pos, source._buf.ToString(seg.start, length), seg.actualContent);
			}
		}

		public void Insert(int insertOffset, string fileName, Position filePos, string text, bool actualContent)
		{
			Insert(insertOffset, text, new CodeAttributes(fileName, filePos, actualContent));
		}

		public void Insert(int insertOffset, string text, CodeAttributes att)
		{
			// Split the segment that comes before this one.
			CodeSegment lastSegBefore = null;	// = (from s in _segments where s.start < insertOffset select s).LastOrDefault();
			foreach (var seg in _segments)
			{
				lastSegBefore = seg;
				if (seg.start >= insertOffset) break;
			}
			if (lastSegBefore != null)
			{
				var segSplitPos = lastSegBefore.pos.Advance(_buf.ToString(lastSegBefore.start, insertOffset - lastSegBefore.start));
				_segments.Add(new CodeSegment(lastSegBefore.fileName, insertOffset, segSplitPos, att.ActualContent));
			}

			// Bump all the segments that appear after this one.
			foreach (var seg in _segments)
			{
				if (seg.start >= insertOffset) seg.start += text.Length;
			}

			// Insert this new one.
			_segments.Add(new CodeSegment(att.FileName, insertOffset, att.FilePosition, att.ActualContent));
			_text.Insert(insertOffset, text);

			// Restore the sorting.
			_segments.Sort(_codeSegmentComparer);

			_text = null;
			_version++;
		}

		public string Text
		{
			get
			{
				if (_text == null) _text = _buf.ToString();
				return _text;
			}
		}

		private int FindSegmentIndexForOffset(int offset)
		{
#if DEBUG
			if (offset < 0) throw new ArgumentOutOfRangeException("offset");
#endif
			if (_segments.Count == 0) return -1;

			// Do a binary search for the segment index
			var min = 0;
			var max = _segments.Count - 1;
			var lastSeg = _segments.Count - 1;
			while (min < max)
			{
				var mid = min + (max - min) / 2;
				if (mid == lastSeg) return mid;

				var seg = _segments[mid];
				if (seg.start > offset)
				{
					min = mid + 1;
					continue;
				}

				var length = _segments[mid + 1].start - seg.start;
				if (seg.start + length < offset)
				{
					max = mid - 1;
					continue;
				}

				return mid;
			}

			return min;
		}

		public void GetFilePosition(int sourceOffset, out string foundFileName, out Position foundPos)
		{
			if (sourceOffset < 0 || sourceOffset > _text.Length) throw new ArgumentOutOfRangeException("offset");

			var segIndex = FindSegmentIndexForOffset(sourceOffset);
			if (segIndex < 0)
			{
				foundFileName = null;
				foundPos = Position.Start;
				return;
			}
			var seg = _segments[segIndex];
			//var seg = (from s in _segments where s.start <= sourceOffset select s).Last();

			var pos = seg.pos;

			if (seg.actualContent)
			{
				var offset = seg.start;
				while (offset < sourceOffset) pos = pos.CalcNext(_text[offset++]);
			}

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

		private int GetSegmentLength(int segIndex)
		{
			if (segIndex + 1 < _segments.Count)
			{
				return _segments[segIndex + 1].start - _segments[segIndex].start;
			}
			else
			{
				return _buf.Length - _segments[segIndex].start;
			}
		}

		public static CodeSource Read(string fileName)
		{
			var content = System.IO.File.ReadAllText(fileName);
			var source = new CodeSource();
			source.Append(content, new CodeAttributes(fileName, Position.Start, true));
			return source;
		}

		public class CodeSourcePreprocessorReader : IPreprocessorReader
		{
			private CodeSource _src;
			private int _srcVersion;
			private int _segIndex;
			private CodeSegment _seg;
			private int _segOffset;
			private int _segLength;
			private Position _pos;
			private StringBuilder _sb = new StringBuilder();

			public CodeSourcePreprocessorReader(CodeSource src)
			{
#if DEBUG
				if (src == null) throw new ArgumentNullException("src");
#endif
				_src = src;
				_srcVersion = src._version;

				_segIndex = 0;
				_segOffset = 0;
				if (_src._segments.Count > 0)
				{
					_seg = _src._segments[_segIndex];
					_pos = _seg.pos;
					_segLength = _src.GetSegmentLength(_segIndex);
				}
				else
				{
					_seg = null;
					_pos = Position.Start;
					_segLength = 0;
				}
			}

			public bool EOF
			{
				get
				{
					return _seg == null;
				}
			}

			public char ReadChar()
			{
				var ch = PeekChar();
				MoveNext();
				return ch;
			}

			public char ReadChar(out CodeAttributes att)
			{
				var ch = PeekChar();
				att = new CodeAttributes(_seg.fileName, _pos, _seg.actualContent);
				MoveNext();
				return ch;
			}

			public char PeekChar()
			{
#if DEBUG
				if (_src._version != _srcVersion) throw new InvalidOperationException("The source content has changed.");
#endif
				if (_seg == null) return '\0';
				return _src._buf.ToString(_seg.start + _segOffset, 1)[0];
			}

			public char PeekChar(out CodeAttributes att)
			{
#if DEBUG
				if (_src._version != _srcVersion) throw new InvalidOperationException("The source content has changed.");
#endif
				if (_seg == null)
				{
					att = new CodeAttributes(null, Position.Start, false);
					return '\0';
				}
				att = new CodeAttributes(_seg.fileName, _pos, _seg.actualContent);
				return _src._buf.ToString(_seg.start + _segOffset, 1)[0];
			}

			public string Peek(int numChars)
			{
#if DEBUG
				if (_src._version != _srcVersion) throw new InvalidOperationException("The source content has changed.");
#endif
				if (_seg == null) return string.Empty;
				var startOffset = _seg.start + _segOffset;
				if (_segOffset + numChars > _segLength) numChars = _segLength - _segOffset;
				return _src._buf.ToString(startOffset, numChars);
			}

			public bool MoveNext()
			{
#if DEBUG
				if (_src._version != _srcVersion) throw new InvalidOperationException("The source content has changed.");
#endif
				if (_seg == null) return false;

				var ch = _src._buf.ToString(_seg.start + _segOffset, 1)[0];

				_pos = _pos.Advance(ch);
				_segOffset++;

				if (_segOffset >= _segLength)
				{
					_segIndex++;
					if (_segIndex >= _src._segments.Count)
					{
						_seg = null;
						return false;
					}
					else
					{
						_seg = _src._segments[_segIndex];
						_segOffset = 0;
						_pos = _seg.pos;
						_segLength = _src.GetSegmentLength(_segIndex);
						return true;
					}
				}
				else
				{
					return true;
				}
			}

			public bool MoveNext(int length)
			{
				while (length-- > 0) ReadChar();
				return !EOF;
			}

			public string ReadSegmentUntil(Func<char, bool> callback)
			{
				if (_seg == null) return string.Empty;

				var startSeg = _seg;
				char ch;

				_sb.Clear();

				while (_seg == startSeg)
				{
					ch = PeekChar();
					if (!callback(ch)) break;
					_sb.Append(ch);
					MoveNext();
				}

				return _sb.ToString();
			}

			public string ReadSegmentUntil(Func<char, bool> callback, out CodeAttributes att)
			{
				if (_seg == null)
				{
					att = new CodeAttributes(null, Position.Start, false);
					return string.Empty;
				}

				var startSeg = _seg;
				char ch;

				_sb.Clear();
				att = new CodeAttributes(_seg.fileName, _pos, _seg.actualContent);

				while (_seg == startSeg)
				{
					ch = PeekChar();
					if (!callback(ch)) break;
					_sb.Append(ch);
					MoveNext();
				}

				return _sb.ToString();
			}

			public string ReadAllUntil(Func<char, bool> callback)
			{
				if (_seg == null) return string.Empty;

				char ch;

				_sb.Clear();

				while (true)
				{
					ch = PeekChar();
					if (!callback(ch)) break;
					_sb.Append(ch);
					if (!MoveNext()) break;
				}

				return _sb.ToString();
			}

			public string ReadIdentifier()
			{
				if (_seg == null) return string.Empty;

				var ch = PeekChar();
				if (!Char.IsLetter(ch) && ch != '_') return string.Empty;

				_sb.Clear();

				while (_seg != null && (Char.IsLetterOrDigit(ch) || ch == '_'))
				{
					_sb.Append(ch);
					MoveNext();
					ch = PeekChar();
				}

				return _sb.ToString();
			}
		}
	}

	internal struct CodeAttributes
	{
		private string _fileName;
		private Position _pos;
		private bool _actualContent;

		public static readonly CodeAttributes Empty = new CodeAttributes(null, Position.Start, false);

		public CodeAttributes(string fileName, Position pos, bool actualContent)
		{
			_fileName = fileName;
			_pos = pos;
			_actualContent = actualContent;
		}

		public string FileName
		{
			get { return _fileName; }
		}

		public Position FilePosition
		{
			get { return _pos; }
		}

		public bool ActualContent
		{
			get { return _actualContent; }
		}

		public override string ToString()
		{
			return string.Format("FileName: {0} FilePosition: {1} ActualContent: {2}", _fileName, _pos, _actualContent);
		}
	}

	//internal struct CodePart
	//{
	//	private string _fileName;
	//	private Position _pos;
	//	private string _text;
	//	private bool _actualContent;

	//	public CodePart(string fileName, Position pos, string text, bool actualContent)
	//	{
	//		_fileName = fileName;
	//		_pos = pos;
	//		_text = text;
	//		_actualContent = actualContent;
	//	}

	//	public string FileName
	//	{
	//		get { return _fileName; }
	//	}

	//	public Position StartPosition
	//	{
	//		get { return _pos; }
	//	}

	//	public string Text
	//	{
	//		get { return _text; }
	//	}

	//	public bool ActualContent
	//	{
	//		get { return _actualContent; }
	//	}
	//}
}

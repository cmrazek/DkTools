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
			public int length;
			public Position startPos;
			public Position endPos;
			public bool actualContent;

			public CodeSegment(string fileName, int start, int length, Position startPos, Position endPos, bool actualContent)
			{
				this.fileName = fileName;
				this.start = start;
				this.length = length;
				this.startPos = startPos;
				this.endPos = endPos;
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

		public void Append(string text, string fileName, Position fileStartPos, Position fileEndPos, bool actualContent)
		{
			var lastSeg = _segments.LastOrDefault();

			if (lastSeg != null)
			{
				if (fileName == lastSeg.fileName && fileStartPos == lastSeg.endPos && actualContent == lastSeg.actualContent)
				{
					// Safe to append onto the end of this segment.
					lastSeg.endPos = fileEndPos;
					lastSeg.length += text.Length;
				}
				else
				{
					_segments.Add(new CodeSegment(fileName, _buf.Length, text.Length, fileStartPos, fileEndPos, actualContent));
				}
			}
			else
			{
				_segments.Add(new CodeSegment(fileName, _buf.Length, text.Length, fileStartPos, fileEndPos, actualContent));
			}

			_buf.Append(text);
			_text = null;
			_version++;
		}

		public void Append(string text, CodeAttributes att)
		{
			Append(text, att.FileName, att.FileStartPosition, att.FileEndPosition, att.ActualContent);
		}

		public void Append(CodeSource source)
		{
			_buf.EnsureCapacity(_buf.Length + source._buf.Length);

			for (int segIndex = 0, numSegs = source._segments.Count; segIndex < numSegs; segIndex++)
			{
				var seg = source._segments[segIndex];
				var length = (segIndex + 1 < numSegs ? source._segments[segIndex + 1].start : source._buf.Length) - seg.start;

				Append(source._buf.ToString(seg.start, length), seg.fileName, seg.startPos, seg.endPos, seg.actualContent);
			}
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

			var pos = seg.startPos;

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
				sb.AppendFormat("SEGMENT {0} FileName: {1} Pos: {2} [", segIndex, seg.fileName, seg.startPos);

				var end = segIndex + 1 < _segments.Count ? _segments[segIndex + 1].start : _buf.Length;
				sb.Append(_buf.ToString(seg.start, end - seg.start));

				sb.Append("]");
			}

			return sb.ToString();
		}

		public string DumpContinuousSegments()
		{
			var sb = new StringBuilder();

			var fileName = "?";
			var pos = Position.Start;
			var segIndex = 0;
			var sample = "";

			foreach (var seg in _segments)
			{
				if (seg.fileName != fileName || seg.startPos != pos)
				{
					if (segIndex > 0)
					{
						sb.AppendFormat(" EndPos [{0}] {1}", pos, sample.Trim());
						sb.AppendLine();
					}
					sb.AppendFormat("SEGMENT [{0}] Offset [{3}] FileName [{1}] Actual [{4}] StartPos [{2}]", segIndex, seg.fileName, seg.startPos, seg.start, seg.actualContent);

					var length = 30;
					if (length > seg.length) length = seg.length;
					sample = _buf.ToString(seg.start, length).Replace('\r', ' ').Replace('\n', ' ').Trim();
				}

				fileName = seg.fileName;
				pos = seg.endPos;
				segIndex++;
			}

			if (segIndex > 0)
			{
				sb.AppendFormat(" EndPos [{0}] {1}", pos, sample.Trim());
				sb.AppendLine();
			}

			return sb.ToString();
		}
#endif

		public VsText.ITextSnapshot Snapshot
		{
			get { return _snapshot; }
			set { _snapshot = value; }
		}

		public class CodeSourcePreprocessorReader : IPreprocessorReader
		{
			private CodeSource _src;
			//private int _srcVersion;
			private int _segIndex;
			private CodeSegment _seg;
			private int _segOffset;
			private Position _pos;
			private StringBuilder _sb = new StringBuilder();
			private IPreprocessorWriter _writer;
			private Stack<State> _stack = new Stack<State>();
			private bool _suppress;

			public CodeSourcePreprocessorReader(CodeSource src)
			{
#if DEBUG
				if (src == null) throw new ArgumentNullException("src");
#endif
				_src = src;

				_segIndex = 0;
				_segOffset = 0;
				if (_src._segments.Count > 0)
				{
					_seg = _src._segments[_segIndex];
					_pos = _seg.startPos;
				}
				else
				{
					_seg = null;
					_pos = Position.Start;
				}
			}

			private class State
			{
				public int segIndex;
				public CodeSegment seg;
				public int segOffset;
				public Position pos;

				public State Clone()
				{
					return new State
					{
						segIndex = segIndex,
						seg = seg,
						segOffset = segOffset,
						pos = pos
					};
				}
			}

			private void PushState()
			{
				_stack.Push(new State
				{
					segIndex = _segIndex,
					seg = _seg,
					segOffset = _segOffset,
					pos = _pos
				});
			}

			private void PopState()
			{
				var state = _stack.Pop();
				_segIndex = state.segIndex;
				_seg = state.seg;
				_segOffset = state.segOffset;
				_pos = state.pos;
			}

			public void SetWriter(IPreprocessorWriter writer)
			{
				_writer = writer;
			}

			public bool EOF
			{
				get
				{
					return _seg == null;
				}
			}

			public char Peek()
			{
				if (_seg == null) return '\0';
				return _src._buf.ToString(_seg.start + _segOffset, 1)[0];
			}

			public string Peek(int numChars)
			{
				_sb.Clear();
				PushState();

				while (numChars-- > 0 && _seg != null)
				{
					_sb.Append(Peek());
					MoveNext();
				}

				PopState();
				return _sb.ToString();
			}

			public string PeekUntil(Func<char, bool> callback)
			{
				_sb.Clear();
				PushState();

				char ch;

				while (_seg != null)
				{
					ch = Peek();
					if (callback(ch))
					{
						_sb.Append(ch);
						MoveNext();
					}
					else break;
				}

				PopState();
				return _sb.ToString();
			}

			public string PeekIdentifier()
			{
				var first = true;
				return PeekUntil(ch =>
					{
						if (first)
						{
							first = false;
							return ch.IsWordChar(true);
						}
						else return ch.IsWordChar(false);
					});
			}

			private void Parse(int numChars, bool use)
			{
				while (numChars > 0 && _seg != null)
				{
					if (_segOffset + numChars >= _seg.length)
					{
						var length = _seg.length - (_segOffset + numChars);
						_writer.Append(use ? _src._buf.ToString(_seg.start + _segOffset, length) : string.Empty,
							new CodeAttributes(_seg.fileName, _pos, _seg.endPos, _seg.actualContent));
						MoveNextSegment();
						numChars -= length;
					}
					else
					{
						var text = _src._buf.ToString(_seg.start + _segOffset, numChars);
						var newPos = _pos.Advance(text);
						_writer.Append(use ? text : string.Empty, new CodeAttributes(_seg.fileName, _pos, _seg.actualContent ? newPos : _pos, _seg.actualContent));
						_segOffset += numChars;
						_pos = newPos;
						numChars = 0;
					}
				}
			}

			private void ParseUntil(Func<char, bool> callback, bool use)
			{
				char ch;
				var startPos = _pos;
				var gotContent = false;

				_sb.Clear();

				while (_seg != null)
				{
					ch = Peek();
					if (callback(ch))
					{
						if (_segOffset + 1 == _seg.length)
						{
							// Hits the end of the segment
							if (use)
							{
								_sb.Append(ch);
								var text = _sb.ToString();
								_writer.Append(text, new CodeAttributes(_seg.fileName, startPos, _seg.endPos, _seg.actualContent));
								MoveNextSegment();
								startPos = _pos;
								_sb.Clear();
								gotContent = false;
							}
							else
							{
								_writer.Append(string.Empty, new CodeAttributes(_seg.fileName, startPos, _seg.endPos, _seg.actualContent));
								MoveNextSegment();
								startPos = _pos;
								gotContent = false;
							}
						}
						else
						{
							// Within the current segment
							if (use) _sb.Append(ch);
							MoveNext();
							gotContent = true;
						}
					}
					else break;
				}

				if (gotContent)
				{
					if (use)
					{
						_writer.Append(_sb.ToString(), new CodeAttributes(_seg.fileName, startPos, _pos, _seg.actualContent));
					}
					else
					{
						_writer.Append(string.Empty, new CodeAttributes(_seg.fileName, startPos, _pos, _seg.actualContent));
					}
				}
			}

			public void Use(int numChars)
			{
				Parse(numChars, _suppress ? false : true);
			}

			public void UseUntil(Func<char, bool> callback)
			{
				ParseUntil(callback, _suppress ? false : true);
			}

			public void Ignore(int numChars)
			{
				Parse(numChars, false);
			}

			public void IgnoreUntil(Func<char, bool> callback)
			{
				ParseUntil(callback, false);
			}

			public void Insert(string text)
			{
				if (_seg != null)
				{
					_writer.Append(text, new CodeAttributes(_seg.fileName, _pos, _pos, false));
				}
				else
				{
					if (_src._segments.Count > 0)
					{
						var lastSeg = _src._segments[_src._segments.Count - 1];
						_writer.Append(text, new CodeAttributes(lastSeg.fileName, lastSeg.endPos, lastSeg.endPos, false));
					}
					else
					{
						_writer.Append(text, CodeAttributes.Empty);
					}
				}
			}

			private bool MoveNext()
			{
				if (_seg == null) return false;

				var ch = _src._buf.ToString(_seg.start + _segOffset, 1)[0];

				if (_seg.actualContent) _pos = _pos.Advance(ch);
				_segOffset++;

				if (_segOffset >= _seg.length) return MoveNextSegment();
				return true;
			}

			private bool MoveNextSegment()
			{
				_segIndex++;
				if (_segIndex >= _src._segments.Count)
				{
					_seg = null;
					return false;
				}

				_seg = _src._segments[_segIndex];
				_segOffset = 0;
				_pos = _seg.startPos;
				return true;
			}

			public bool Suppress
			{
				get { return _suppress; }
				set { _suppress = value; }
			}
		}
	}

	internal struct CodeAttributes
	{
		public string FileName;
		public Position FileStartPosition;
		public Position FileEndPosition;
		public bool ActualContent;

		public static readonly CodeAttributes Empty = new CodeAttributes(null, Position.Start, Position.Start, false);

		public CodeAttributes(string fileName, Position fileStartPos, Position fileEndPos, bool actualContent)
		{
			FileName = fileName;
			FileStartPosition = fileStartPos;
			FileEndPosition = fileEndPos;
			ActualContent = actualContent;
		}

		public override string ToString()
		{
			return string.Format("FileName: {0} StartPos: {1} EndPos: {2} ActualContent: {3}", FileName, FileStartPosition, FileEndPosition, ActualContent);
		}
	}
}

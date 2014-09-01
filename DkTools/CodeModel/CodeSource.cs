using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using VsText = Microsoft.VisualStudio.Text;

namespace DkTools.CodeModel
{
	internal class CodeSource : IPreprocessorWriter
	{
		private StringBuilder _writeBuf = new StringBuilder();
		private string _text = null;
		private List<CodeSegment> _segments = new List<CodeSegment>();
		private CodeSegment.CodeSegmentComparer _codeSegmentComparer = new CodeSegment.CodeSegmentComparer();
		private VsText.ITextSnapshot _snapshot;
		private int _lastFindSegment = -1;
		private int _length;
		private bool _isEmptyLine = true;

		private class CodeSegment
		{
			public string fileName;
			public int start;
			public int length;
			public Position startPos;
			public Position endPos;
			public bool actualContent;
			public bool primaryFile;
			public string text;
			public bool disabled;

			public CodeSegment(string fileName, int start, Position startPos, Position endPos, bool actualContent, bool primaryFile, bool disabled)
			{
				this.fileName = fileName;
				this.start = start;
				this.length = 0;
				this.startPos = startPos;
				this.endPos = endPos;
				this.actualContent = actualContent;
				this.primaryFile = primaryFile;
				this.text = string.Empty;
				this.disabled = disabled;
			}

			public class CodeSegmentComparer : IComparer<CodeSegment>
			{
				public int Compare(CodeSegment a, CodeSegment b)
				{
					return a.start.CompareTo(b.start);
				}
			}
		}

		public void Flush()
		{
			if (_segments.Count == 0) return;

			if (_writeBuf.Length > 0)
			{
				var lastSeg = _segments[_segments.Count - 1];
				if (!string.IsNullOrEmpty(lastSeg.text)) lastSeg.text = string.Concat(lastSeg.text, _writeBuf);
				else lastSeg.text = _writeBuf.ToString();
#if DEBUG
				if (lastSeg.length != lastSeg.text.Length) throw new InvalidOperationException("Segment length is incorrect during flush.");
#endif
				_writeBuf.Clear();
			}
		}

		public void Append(string text, string fileName, Position fileStartPos, Position fileEndPos, bool actualContent, bool primaryFile, bool disabled)
		{
			var lastSeg = _segments.LastOrDefault();

			if (lastSeg == null ||
				fileName != lastSeg.fileName ||
				fileStartPos != lastSeg.endPos ||
				actualContent != lastSeg.actualContent ||
				disabled != lastSeg.disabled)
			{
				Flush();
				lastSeg = new CodeSegment(fileName, _length, fileStartPos, fileEndPos, actualContent, primaryFile, disabled);
				_segments.Add(lastSeg);
			}

			_writeBuf.Append(text);
			_length += text.Length;
			lastSeg.length += text.Length;
			lastSeg.endPos = fileEndPos;

#if DEBUG
			if (lastSeg.length != _writeBuf.Length + lastSeg.text.Length) throw new InvalidOperationException("Segment length is incorrect during append.");
#endif

			_text = null;

			foreach (var ch in text)
			{
				if (ch == '\n') _isEmptyLine = true;
				else if (!char.IsWhiteSpace(ch)) _isEmptyLine = false;
			}
		}

		public void Append(string text, CodeAttributes att)
		{
			Append(text, att.FileName, att.FileStartPosition, att.FileEndPosition, att.ActualContent, att.PrimaryFile, att.Disabled);
		}

		public void Append(CodeSource source)
		{
			foreach (var seg in source._segments)
			{
				Append(seg.text, seg.fileName, seg.startPos, seg.endPos, seg.actualContent, seg.primaryFile, seg.disabled);
			}
		}

		public string Text
		{
			get
			{
				if (_text == null)
				{
					var sb = new StringBuilder(_length);
					foreach (var seg in _segments) sb.Append(seg.text);
					_text = sb.ToString();
				}
				return _text;
			}
		}

		private int FindSegmentIndexForOffset(int offset)
		{
#if DEBUG
			if (offset < 0) throw new ArgumentOutOfRangeException("offset");
#endif
			if (_segments.Count == 0) return -1;

			if (_lastFindSegment >= 0)
			{
				var seg = _segments[_lastFindSegment];
				if (seg.start <= offset && seg.start + seg.length > offset) return _lastFindSegment;
			}

			// Do a binary search for the segment index
			var min = 0;
			var max = _segments.Count - 1;
			var lastSeg = _segments.Count - 1;
			while (min < max)
			{
				var mid = min + (max - min) / 2;
				if (mid == lastSeg)
				{
					_lastFindSegment = mid;
					return mid;
				}

				var seg = _segments[mid];
				if (seg.start > offset)
				{
					max = mid - 1;
					continue;
				}

				var length = _segments[mid + 1].start - seg.start;
				if (seg.start + length <= offset)
				{
					min = mid + 1;
					continue;
				}

				_lastFindSegment = mid;
				return mid;
			}

			_lastFindSegment = min;
			return min;
		}

		public bool OffsetIsInPrimaryFile(int offset)
		{
			var segIndex = FindSegmentIndexForOffset(offset);
			if (segIndex < 0) return false;

			return _segments[segIndex].primaryFile;
		}

		public LocalFilePosition GetFilePosition(int sourceOffset)
		{
			if (sourceOffset < 0 || sourceOffset > _length) throw new ArgumentOutOfRangeException("offset");

			var segIndex = FindSegmentIndexForOffset(sourceOffset);
			if (segIndex < 0)
			{
				return LocalFilePosition.Empty;
			}
			var seg = _segments[segIndex];
			var pos = seg.startPos;

			if (seg.actualContent)
			{
				pos = pos.Advance(seg.text, 0, sourceOffset - seg.start);
			}

			return new LocalFilePosition(seg.fileName, pos, seg.primaryFile);
		}

		public void GetFileSpan(Span sourceSpan, out string foundFileName, out Span foundSpan, out bool foundPrimaryFile)
		{
			var startLocalPos = GetFilePosition(sourceSpan.Start.Offset);
			var endLocalPos = GetFilePosition(sourceSpan.End.Offset);

			if (startLocalPos.FileName.Equals(endLocalPos.FileName, StringComparison.OrdinalIgnoreCase) && endLocalPos.Position >= startLocalPos.Position)
			{
				foundFileName = startLocalPos.FileName;
				foundSpan = new Span(startLocalPos.Position, endLocalPos.Position);
				foundPrimaryFile = startLocalPos.PrimaryFile;
			}
			else
			{
				foundFileName = startLocalPos.FileName;
				foundSpan = new Span(startLocalPos.Position, startLocalPos.Position);
				foundPrimaryFile = startLocalPos.PrimaryFile;
			}
		}

#if DEBUG
		public string Dump()
		{
			var sb = new StringBuilder();
			
			for (var segIndex = 0; segIndex < _segments.Count; segIndex++)
			{
				var seg = _segments[segIndex];
				sb.AppendFormat("SEGMENT {0} FileName [{1}] StartOffset [{4}] Length [{5}] Start [{2}] End [{3}] Primary [{6}] Content [",
					segIndex, System.IO.Path.GetFileName(seg.fileName), seg.startPos, seg.endPos, seg.start, seg.length, seg.primaryFile);
				sb.Append(seg.text);
				sb.AppendLine("]");
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
					sample = seg.text.Substring(0, length).Replace('\r', ' ').Replace('\n', ' ').Trim();
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

		public bool IsEmptyLine
		{
			get { return _isEmptyLine; }
		}

		public IEnumerable<Span> GenerateDisabledSections()
		{
			var disabled = false;
			var disableStart = Position.Start;

			foreach (var seg in _segments)
			{
				if (!seg.primaryFile) continue;
				if (seg.disabled == disabled) continue;

				if (seg.disabled)
				{
					disableStart = seg.startPos;
					disabled = true;
				}
				else
				{
					yield return new Span(disableStart, seg.startPos);
					disabled = false;
				}
			}

			if (disabled)
			{
				if (_segments.Count > 0) yield return new Span(disableStart, _segments[_segments.Count - 1].endPos);
			}
		}

		public class CodeSourcePreprocessorReader : IPreprocessorReader
		{
			private CodeSource _src;
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
				return _seg.text[_segOffset];
			}

			public string Peek(int numChars)
			{
				if (_segOffset + numChars <= _seg.length)
				{
					return _seg.text.Substring(_segOffset, numChars);
				}
				else
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

			public void Use(int numChars)
			{
				while (numChars > 0 && _seg != null)
				{
					if (_segOffset + numChars >= _seg.length)
					{
						var length = _seg.length - (_segOffset + numChars);
						_writer.Append(_seg.text.Substring(_segOffset, length), new CodeAttributes(_seg.fileName, _pos, _seg.endPos, _seg.actualContent, _seg.primaryFile, _suppress));
						MoveNextSegment();
						numChars -= length;
					}
					else
					{
						var text = _seg.text.Substring(_segOffset, numChars);
						var newPos = _pos.Advance(text);
						_writer.Append(text, new CodeAttributes(_seg.fileName, _pos, _seg.actualContent ? newPos : _pos, _seg.actualContent, _seg.primaryFile, _suppress));
						_segOffset += numChars;
						_pos = newPos;
						numChars = 0;
					}
				}
			}

			public void UseUntil(Func<char, bool> callback)
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
							_sb.Append(ch);
							var text = _sb.ToString();
							_writer.Append(text, new CodeAttributes(_seg.fileName, startPos, _seg.endPos, _seg.actualContent, _seg.primaryFile, _suppress));
							MoveNextSegment();
							startPos = _pos;
							_sb.Clear();
							gotContent = false;
						}
						else
						{
							// Within the current segment
							_sb.Append(ch);
							MoveNext();
							gotContent = true;
						}
					}
					else break;
				}

				if (gotContent)
				{
					_writer.Append(_sb.ToString(), new CodeAttributes(_seg.fileName, startPos, _pos, _seg.actualContent, _seg.primaryFile, _suppress));
				}
			}

			public void Ignore(int numChars)
			{
				while (numChars > 0 && _seg != null)
				{
					if (_segOffset + numChars >= _seg.length)
					{
						var length = _seg.length - (_segOffset + numChars);
						_writer.Append(string.Empty, new CodeAttributes(_seg.fileName, _pos, _seg.endPos, false, _seg.primaryFile, _suppress));
						MoveNextSegment();
						numChars -= length;
					}
					else
					{
						var text = _seg.text.Substring(_segOffset, numChars);
						var newPos = _pos.Advance(text);
						_writer.Append(string.Empty, new CodeAttributes(_seg.fileName, _pos, _seg.actualContent ? newPos : _pos, false, _seg.primaryFile, _suppress));
						_segOffset += numChars;
						_pos = newPos;
						numChars = 0;
					}
				}
			}

			public void IgnoreUntil(Func<char, bool> callback)
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
							_writer.Append(string.Empty, new CodeAttributes(_seg.fileName, startPos, _seg.endPos, false, _seg.primaryFile, _suppress));
							MoveNextSegment();
							startPos = _pos;
							gotContent = false;
						}
						else
						{
							// Within the current segment
							MoveNext();
							gotContent = true;
						}
					}
					else break;
				}

				if (gotContent)
				{
					_writer.Append(string.Empty, new CodeAttributes(_seg.fileName, startPos, _pos, false, _seg.primaryFile, _suppress));
				}
			}

			public void Insert(string text)
			{
				if (_seg != null)
				{
					_writer.Append(text, new CodeAttributes(_seg.fileName, _pos, _pos, false, _seg.primaryFile, _suppress));
				}
				else
				{
					if (_src._segments.Count > 0)
					{
						var lastSeg = _src._segments[_src._segments.Count - 1];
						_writer.Append(text, new CodeAttributes(lastSeg.fileName, lastSeg.endPos, lastSeg.endPos, false, lastSeg.primaryFile, _suppress));
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

				var ch = _seg.text[_segOffset];

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

			public string FileName
			{
				get
				{
					if (_seg == null) return string.Empty;
					return _seg.fileName;
				}
			}

			public Position Position
			{
				get
				{
					if (_seg == null) return Position.Start;
					return _pos;
				}
			}

			public Match Match(Regex rx)
			{
				var curIndex = _seg.start + _segOffset;
				var match = rx.Match(_src.Text, curIndex);
				if (match.Success && match.Index == curIndex) return match;
				return System.Text.RegularExpressions.Match.Empty;
			}
		}
	}

	internal struct CodeAttributes
	{
		public string FileName;
		public Position FileStartPosition;
		public Position FileEndPosition;
		public bool ActualContent;
		public bool PrimaryFile;
		public bool Disabled;

		public static readonly CodeAttributes Empty = new CodeAttributes(null, Position.Start, Position.Start, false, false, false);

		public CodeAttributes(string fileName, Position fileStartPos, Position fileEndPos, bool actualContent, bool primaryFile, bool disabled)
		{
			FileName = fileName;
			FileStartPosition = fileStartPos;
			FileEndPosition = fileEndPos;
			ActualContent = actualContent;
			PrimaryFile = primaryFile;
			Disabled = disabled;
		}

		public override string ToString()
		{
			return string.Format("FileName: {0} StartPos: {1} EndPos: {2} ActualContent: {3}", FileName, FileStartPosition, FileEndPosition, ActualContent);
		}
	}
}

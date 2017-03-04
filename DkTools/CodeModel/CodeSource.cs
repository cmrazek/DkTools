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
		public const int InitialCapacity = 4 * 1024;

		private StringBuilder _writeBuf = new StringBuilder(InitialCapacity);
		private string _text = null;
		private List<CodeSegment> _segments = new List<CodeSegment>();
		private CodeSegment.CodeSegmentComparer _codeSegmentComparer = new CodeSegment.CodeSegmentComparer();
		private VsText.ITextSnapshot _snapshot;
		private int _lastFindSegment = -1;
		private int _length;

		private class CodeSegment
		{
			public string fileName;
			public int fileStartPos;	// Starting position of the segment within the full combined source
			public int length;
			public int startPos;		// Starting position of the segment without the individual file
			public int endPos;
			public bool actualContent;
			public bool primaryFile;
			public int nextPrimaryStartPos;
			public string text;
			public bool disabled;

			public CodeSegment(string fileName, int fileStartPos, int startPos, int endPos, bool actualContent, bool primaryFile, bool disabled)
			{
				this.fileName = fileName;
				this.fileStartPos = fileStartPos;
				this.length = 0;
				this.startPos = startPos;
				this.endPos = endPos;
				this.actualContent = actualContent;
				this.primaryFile = primaryFile;
				this.nextPrimaryStartPos = -1;
				this.text = string.Empty;
				this.disabled = disabled;
			}

			public class CodeSegmentComparer : IComparer<CodeSegment>
			{
				public int Compare(CodeSegment a, CodeSegment b)
				{
					return a.fileStartPos.CompareTo(b.fileStartPos);
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

		public void Append(string text, string fileName, int fileStartPos, int fileEndPos, bool actualContent, bool primaryFile, bool disabled)
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
				
				if (primaryFile)
				{
					var primaryStartPos = fileStartPos;
					CodeSegment seg;
					for (int segIndex = _segments.Count - 1; segIndex >= 0; segIndex--)
					{
						seg = _segments[segIndex];
						if (seg.nextPrimaryStartPos == -1) seg.nextPrimaryStartPos = primaryStartPos;
						else break;
					}
				}

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

			//foreach (var ch in text)
			//{
			//	if (ch == '\n') _isEmptyLine = true;
			//	else if (!char.IsWhiteSpace(ch)) _isEmptyLine = false;
			//}
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
				if (seg.fileStartPos <= offset && seg.fileStartPos + seg.length > offset)
				{
					return _lastFindSegment;
				}
				else if (offset < seg.fileStartPos)
				{
					// work backwards until the segment is found
					while (--_lastFindSegment >= 0)
					{
						seg = _segments[_lastFindSegment];
						if (seg.fileStartPos <= offset) return _lastFindSegment;
					}
					return -1;
				}
				else
				{
					// work forwards until the segment is found
					while (++_lastFindSegment < _segments.Count)
					{
						seg = _segments[_lastFindSegment];
						if (seg.fileStartPos + seg.length > offset) return _lastFindSegment;
					}

					// If at the end of the file, then return the last segment.
					if (offset == _length) return _lastFindSegment = _segments.Count - 1;

					return _lastFindSegment = -1;
				}
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
				if (seg.fileStartPos > offset)
				{
					max = mid - 1;
					continue;
				}

				var length = _segments[mid + 1].fileStartPos - seg.fileStartPos;
				if (seg.fileStartPos + length <= offset)
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

		public FilePosition GetFilePosition(int sourceOffset)
		{
			if (sourceOffset < 0 || sourceOffset > _length) throw new ArgumentOutOfRangeException("sourceOffset");

			var segIndex = FindSegmentIndexForOffset(sourceOffset);
			if (segIndex < 0)
			{
				return FilePosition.Empty;
			}
			var seg = _segments[segIndex];
			var pos = seg.startPos;

			if (seg.actualContent) pos += sourceOffset - seg.fileStartPos;

			return new FilePosition(seg.fileName, pos, seg.primaryFile);
		}

		public Span GetFileSpan(Span sourceSpan, out string fileNameOut, out bool isInPrimaryFile)
		{
			var startFilePos = GetFilePosition(sourceSpan.Start);

			if (sourceSpan.Start < 0 || sourceSpan.Start >= _length) throw new ArgumentOutOfRangeException("sourceSpan");

			var startSegIndex = FindSegmentIndexForOffset(sourceSpan.Start);
			if (startSegIndex < 0)
			{
				fileNameOut = null;
				isInPrimaryFile = false;
				return Span.Empty;
			}

			var endSegIndex = FindSegmentIndexForOffset(sourceSpan.End);
			if (endSegIndex < 0)
			{
				fileNameOut = null;
				isInPrimaryFile = false;
				return Span.Empty;
			}

			var seg = _segments[startSegIndex];
			fileNameOut = seg.fileName;
			isInPrimaryFile = seg.primaryFile;

			var startPos = sourceSpan.Start - seg.fileStartPos + seg.startPos;

			if (seg.fileStartPos + seg.length >= sourceSpan.End)
			{
				// All fits within one segment
				if (seg.actualContent) return new Span(startPos, startPos + sourceSpan.Length);
				else return new Span(seg.startPos, seg.startPos);
			}

			var endPos = seg.actualContent ? seg.startPos + seg.length : seg.startPos;

			for (int s = startSegIndex + 1; s <= endSegIndex; s++)
			{
				seg = _segments[s];
				if (seg.fileName != fileNameOut) continue;

				if (seg.fileStartPos > sourceSpan.End) return new Span(startPos, endPos);
				if (seg.fileStartPos + seg.length >= sourceSpan.End)
				{
					return new Span(startPos, seg.actualContent ? seg.startPos + (sourceSpan.End - seg.fileStartPos) : seg.startPos);
				}

				endPos = seg.actualContent ? seg.startPos + seg.length : seg.startPos;
			}

			return new Span(startPos, endPos);
		}

		/// <summary>
		/// Gets the position in the primary file closest to the source offset.
		/// </summary>
		/// <param name="sourceOffset">The position in the source, which could consist of mixed file content.</param>
		/// <returns>The position in the primary file that's closest to sourceOffset.</returns>
		public int GetPrimaryFilePosition(int sourceOffset)
		{
			if (sourceOffset < 0 || sourceOffset > _length) throw new ArgumentOutOfRangeException("offset");

			var segIndex = FindSegmentIndexForOffset(sourceOffset);
			if (segIndex < 0) return -1;

			var seg = _segments[segIndex];
			if (seg.primaryFile)
			{
				if (seg.actualContent) return seg.startPos + (sourceOffset - seg.fileStartPos);
				else return seg.startPos;
			}
			else
			{
				return seg.nextPrimaryStartPos;
			}
		}

		public Span GetPrimaryFileSpan(Span sourceSpan)
		{
			var startPos = GetPrimaryFilePosition(sourceSpan.Start);
			var endPos = GetPrimaryFilePosition(sourceSpan.End);
			if (startPos < 0 || endPos < 0) return Span.Empty;
			return new Span(startPos, endPos);
		}

		/// <summary>
		/// Determines if the provided position appears in the primary file.
		/// </summary>
		/// <param name="sourceOffset">The position to be tested.</param>
		/// <returns>True if the position is in the primary file; false if it is in another file.</returns>
		public bool PositionIsInPrimaryFile(int sourceOffset)
		{
			var segIndex = FindSegmentIndexForOffset(sourceOffset);
			if (segIndex < 0) return false;

			return _segments[segIndex].primaryFile;
		}

		/// <summary>
		/// This correlates a position in the primary file with the position in the expanded source.
		/// </summary>
		/// <param name="primaryFilePos">The position in the primary file.</param>
		/// <returns>If found, the position is the expanded source; otherwise -1.</returns>
		public int PrimaryFilePositionToSource(int primaryFilePos)
		{
			foreach (var seg in _segments)
			{
				if (seg.primaryFile && seg.startPos <= primaryFilePos)
				{
					if (seg.actualContent)
					{
						if (seg.startPos + seg.length >= primaryFilePos)
						{
							return seg.fileStartPos + (primaryFilePos - seg.startPos);
						}
					}
					else
					{
						if (seg.fileStartPos == primaryFilePos)
						{
							return seg.fileStartPos;
						}
					}
				}
			}

			return -1;
		}

		public void GetFileSpan(Span sourceSpan, out string foundFileName, out Span foundSpan, out bool foundPrimaryFile)
		{
			var startLocalPos = GetFilePosition(sourceSpan.Start);
			var endLocalPos = GetFilePosition(sourceSpan.End);

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
					segIndex, System.IO.Path.GetFileName(seg.fileName), seg.startPos, seg.endPos, seg.fileStartPos, seg.length, seg.primaryFile);
				sb.Append(seg.text);
				sb.AppendLine("]");
			}

			return sb.ToString();
		}

		public string DumpContinuousSegments()
		{
			var sb = new StringBuilder();

			var fileName = "?";
			var pos = 0;
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
					sb.AppendFormat("SEGMENT [{0}] Offset [{3}] FileName [{1}] Actual [{4}] StartPos [{2}]", segIndex, seg.fileName, seg.startPos, seg.fileStartPos, seg.actualContent);

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

		public IEnumerable<Span> GenerateDisabledSections()
		{
			var disabled = false;
			var disableStart = 0;

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
			private int _segLength;
			private int _pos;
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
					_segLength = _seg.length;
				}
				else
				{
					_seg = null;
					_pos = 0;
					_segLength = 0;
				}
			}

			private class State
			{
				public int segIndex;
				public CodeSegment seg;
				public int segOffset;
				public int pos;
				public int segLength;

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
					pos = _pos,
					segLength = _segLength
				});
			}

			private void PopState()
			{
				var state = _stack.Pop();
				_segIndex = state.segIndex;
				_seg = state.seg;
				_segOffset = state.segOffset;
				_pos = state.pos;
				_segLength = state.segLength;
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
				if (_seg == null || _segOffset >= _segLength) return '\0';
				return _seg.text[_segOffset];
			}

			public string Peek(int numChars)
			{
				if (_segOffset + numChars <= _segLength)
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
					if (_segOffset + numChars >= _segLength)
					{
						var length = _segLength - _segOffset;
						_writer.Append(_seg.text.Substring(_segOffset, length), new CodeAttributes(_seg.fileName, _pos, _seg.endPos, _seg.actualContent, _seg.primaryFile, _suppress));
						MoveNextSegment();
						numChars -= length;
					}
					else
					{
						var text = _seg.text.Substring(_segOffset, numChars);
						var newPos = _pos + text.Length;
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
						if (_segOffset + 1 == _segLength)
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
					if (_segOffset + numChars >= _segLength)
					{
						var length = _segLength - _segOffset;
						_writer.Append(string.Empty, new CodeAttributes(_seg.fileName, _pos, _seg.endPos, false, _seg.primaryFile, _suppress));
						MoveNextSegment();
						numChars -= length;
					}
					else
					{
						var text = _seg.text.Substring(_segOffset, numChars);
						var newPos = _pos + text.Length;
						_writer.Append(string.Empty, new CodeAttributes(_seg.fileName, _pos, _seg.actualContent ? newPos : _pos, false, _seg.primaryFile, _suppress));
						_segOffset += numChars;
						_pos = newPos;
						numChars = 0;
					}
				}
			}

			public void IgnoreUntil(IEnumerable<char> breakChars)
			{
				char ch;
				var startPos = _pos;
				var gotContent = false;

				_sb.Clear();

				while (_seg != null)
				{
					ch = Peek();
					if (!breakChars.Contains(ch))
					{
						if (_segOffset + 1 == _segLength)
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

			public void IgnoreWhile(IEnumerable<char> whileChars)
			{
				char ch;
				var startPos = _pos;
				var gotContent = false;

				_sb.Clear();

				while (_seg != null)
				{
					ch = Peek();
					if (whileChars.Contains(ch))
					{
						if (_segOffset + 1 == _segLength)
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

				//var ch = _seg.text[_segOffset];

				if (_seg.actualContent) _pos++;
				_segOffset++;

				if (_segOffset >= _segLength) return MoveNextSegment();
				return true;
			}

			private bool MoveNextSegment()
			{
				_segIndex++;
				if (_segIndex >= _src._segments.Count)
				{
					_seg = null;
					_segLength = 0;
					_segOffset = 0;
					return false;
				}

				_seg = _src._segments[_segIndex];
				_segOffset = 0;
				_pos = _seg.startPos;
				_segLength = _seg.length;
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
					return _seg == null ? string.Empty : _seg.fileName;
				}
			}

			public int Position
			{
				get
				{
					return _seg == null ? 0 : _pos;
				}
			}

			public FilePosition FilePosition
			{
				get
				{
					if (_seg == null) return FilePosition.Empty;
					return new FilePosition(_seg.fileName, _seg.fileStartPos + (_pos - _seg.startPos), _seg.primaryFile);
				}
			}

			public Match Match(Regex rx)
			{
				var curIndex = _seg.fileStartPos + _segOffset;
				var match = rx.Match(_src.Text, curIndex);
				if (match.Success && match.Index == curIndex) return match;
				return System.Text.RegularExpressions.Match.Empty;
			}
		}
	}

	internal struct CodeAttributes
	{
		public string FileName;
		public int FileStartPosition;
		public int FileEndPosition;
		public bool ActualContent;
		public bool PrimaryFile;
		public bool Disabled;

		public static readonly CodeAttributes Empty = new CodeAttributes(null, 0, 0, false, false, false);

		public CodeAttributes(string fileName, int fileStartPos, int fileEndPos, bool actualContent, bool primaryFile, bool disabled)
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

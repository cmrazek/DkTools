﻿using DK.Code;
using DK.Diagnostics;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DkTools.CodeModeling
{
    internal class LiveCodeTracker
    {
        private ITextBuffer _buffer;
        private List<int> _lineStates = new List<int>();    // States at the end of the line
        private ITextSnapshot _snapshot;

        public LiveCodeTracker(ITextBuffer textBuffer)
        {
            _buffer = textBuffer ?? throw new ArgumentNullException(nameof(textBuffer));
            _snapshot = _buffer.CurrentSnapshot;
            _buffer.Changed += Buffer_Changed;
        }

        public ITextBuffer TextBuffer => _buffer;

        #region Text Buffer Properties
        public static LiveCodeTracker GetOrCreateForTextBuffer(ITextBuffer textBuffer)
        {
            if (textBuffer == null) throw new ArgumentNullException(nameof(textBuffer));

            if (!textBuffer.Properties.TryGetProperty<LiveCodeTracker>(Constants.TextBufferProperties.LiveCodeTracker, out var tracker))
            {
                tracker = new LiveCodeTracker(textBuffer);
                textBuffer.Properties.AddProperty(Constants.TextBufferProperties.LiveCodeTracker, tracker);
            }

            return tracker;
        }
        #endregion

        #region Line States
        private const int State_SingleLineComment = 0x01;
        private const int State_StringLiteral = 0x02;
        private const int State_CharLiteral = 0x04;
        private const int State_IncludeStringLiteral = 0x08;
        private const int State_IncludeAngleLiteral = 0x10;
        private const int State_AfterInclude = 0x20;

        private const int State_MultiLineComment = 0xff0000;
        private const int State_MultiLineShift = 16;

        // Bits that should get cleared on a line end.
        private const int State_LineEndMask = (State_SingleLineComment | State_StringLiteral | State_CharLiteral | State_IncludeStringLiteral | State_IncludeAngleLiteral | State_AfterInclude);

        private void Buffer_Changed(object sender, TextContentChangedEventArgs e)
        {
            try
            {
                var changeStartPos = e.Changes.Min(c => c.OldPosition);
                if (e.BeforeVersion.VersionNumber != _snapshot.Version.VersionNumber)
                {
                    changeStartPos = e.Before.TranslateOffsetToSnapshot(changeStartPos, _snapshot);
                }

                var changeStartLine = _snapshot.GetLineNumberFromPosition(changeStartPos);

                PurgeOnOrAfterLine(changeStartLine);
                _snapshot = e.After;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        private void PurgeOnOrAfterLine(int lineNumber)
        {
            if (_lineStates.Count > lineNumber)
            {
                _lineStates.RemoveRange(lineNumber, _lineStates.Count - lineNumber);
            }
        }

        public delegate bool StopScanningDelegate(int position, int state);

        private void ScanLineForState(string lineText, ref int state, StopScanningDelegate stopScanning = null)
        {
            var code = new CodeParser(lineText);
            code.ReturnComments = true;

            var pos = 0;
            var len = lineText.Length;
            char ch;
            while (pos < len)
            {
                if (stopScanning != null && stopScanning(pos, state)) break;

                ch = lineText[pos];

                if ((state & State_MultiLineComment) != 0)
                {
                    if (ch == '*' && pos + 1 < len && lineText[pos + 1] == '/')
                    {
                        pos += 2;
                        var level = (state & State_MultiLineComment) >> State_MultiLineShift;   // Decrement multi-line level
                        state = (state & ~State_MultiLineComment) | ((level - 1) << State_MultiLineShift);
                    }
                    else pos++;
                    continue;
                }

                if ((state & State_StringLiteral) != 0)
                {
                    if (ch == '\"')
                    {
                        pos++;
                        state &= ~State_StringLiteral;
                    }
                    else if (ch == '\\')
                    {
                        pos++;
                        if (pos < len) pos++;
                    }
                    else pos++;
                    continue;
                }

                if ((state & State_CharLiteral) != 0)
                {
                    if (ch == '\'')
                    {
                        pos++;
                        state &= ~State_CharLiteral;
                    }
                    else if (ch == '\\')
                    {
                        pos++;
                        if (pos < len) pos++;
                    }
                    else pos++;
                    continue;
                }

                if ((state & State_IncludeStringLiteral) != 0)
                {
                    if (ch == '\"') state &= ~State_IncludeStringLiteral;
                    pos++;
                    continue;
                }

                if ((state & State_IncludeAngleLiteral) != 0)
                {
                    if (ch == '>') state &= ~State_IncludeAngleLiteral;
                    pos++;
                    continue;
                }

                if (ch == ' ' || ch == '\t' || ch == '\r' || ch == '\n')
                {
                    pos++;
                    continue;
                }

                if ((state & State_AfterInclude) != 0)
                {
                    state &= ~State_AfterInclude;
                    if (ch == '\"')
                    {
                        pos++;
                        state |= State_IncludeStringLiteral;
                    }
                    else if (ch == '\'')
                    {
                        pos++;
                        state |= State_IncludeAngleLiteral;
                    }
                    continue;   // Deliberately not incrementing pos here so it will resume on the next iteration.
                }

                if (ch == '/' && pos + 1 < len && lineText[pos + 1] == '/')    // Start of a single-line comment
                {
                    state |= State_SingleLineComment;
                    pos = len;
                    continue;
                }

                if (ch == '/' && pos + 1 < len && lineText[pos + 1] == '*')    // Start of a multi-line comment
                {
                    pos += 2;
                    state = (state & ~State_MultiLineComment) | (1 << State_MultiLineShift);    // Set multi-line level to 1
                    continue;
                }

                if (ch == '\"')
                {
                    pos++;
                    state |= State_StringLiteral;
                    continue;
                }

                if (ch == '\'')
                {
                    pos++;
                    state |= State_CharLiteral;
                    continue;
                }

                if (ch == '#' && pos + 7 < len && lineText[pos + 1] == 'i' && lineText[pos + 2] == 'n' && lineText[pos + 3] == 'c' &&  // #include
                    lineText[pos + 4] == 'l' && lineText[pos + 5] == 'u' && lineText[pos + 6] == 'd' && lineText[pos + 7] == 'e')
                {
                    pos += 8;
                    state |= State_AfterInclude;
                    continue;
                }

                // Any other char doesn't change the state
                pos++;
            }
        }

        /// <summary>
        /// Returns the state at the end of the specified line.
        /// </summary>
        /// <param name="lineNumber"></param>
        /// <returns></returns>
        public int GetStateForLineEnd(int lineNumber)
        {
            if (lineNumber >= _buffer.CurrentSnapshot.LineCount) return 0;

            // Get previous state
            var state = _lineStates.Count == 0 ? 0 : _lineStates[_lineStates.Count - 1];

            while (_lineStates.Count <= lineNumber)
            {
                state &= ~State_LineEndMask;
                var snapshotLine = _buffer.CurrentSnapshot.GetLineFromLineNumber(_lineStates.Count);
                ScanLineForState(snapshotLine.GetText(), ref state);
                _lineStates.Add(state);
            }

            return _lineStates[lineNumber];
        }

        public int GetStateForLineStart(int lineNumber)
        {
            if (lineNumber == 0 || lineNumber > _buffer.CurrentSnapshot.LineCount) return 0;

            return GetStateForLineEnd(lineNumber - 1) & ~State_LineEndMask;
        }

        public int GetStateForPosition(int position)
        {
            if (position >= _buffer.CurrentSnapshot.Length) return 0;

            var line = _buffer.CurrentSnapshot.GetLineFromPosition(position);
            var chOffset = position - line.Start.Position;

            var state = GetStateForLineStart(line.LineNumber);
            if (chOffset == 0) return state;

            ScanLineForState(line.GetText(), ref state, (p,s) => p >= chOffset);
            return state;
        }

        public int FindLineStartNotInComment(int position, int beforePosition = -1)
        {
            if (position == 0) return 0;
            if (position > _buffer.CurrentSnapshot.Length) position = _buffer.CurrentSnapshot.Length;

            var lineNumber = _buffer.CurrentSnapshot.GetLineNumberFromPosition(beforePosition >= 0 ? beforePosition : position);

            var state = GetStateForLineStart(lineNumber);
            while ((state & State_MultiLineComment) != 0) state = GetStateForLineStart(--lineNumber);

            return lineNumber;
        }
        #endregion

        #region Text Parsing
        /// <summary>
        /// Gets the code tokens leading up to a position.
        /// Any nestable tokens that conclude before the position will be returned as a single item.
        /// </summary>
        /// <param name="position">The position for which items leading up to will be returned.</param>
        /// <param name="beforePosition">An optional earlier position that can be passed if the code reading needs to start happening earlier in the file.</param>
        /// <returns></returns>
        public CodeItemSearchResults GetCodeItemsLeadingUpToPosition(int position, int beforePosition = -1)
        {
            var lineNumber = FindLineStartNotInComment(position, beforePosition);
            var startPos = _buffer.CurrentSnapshot.GetLineFromLineNumber(lineNumber).Start.Position;
            var items = new List<CodeItem>();

            while (true)
            {
                var code = new CodeParser(_buffer.CurrentSnapshot.GetText(startPos, position - startPos));
                var endPos = position - startPos;

                while (!code.EndOfFile && code.Position < endPos)
                {
                    var item = code.ReadItemNestable();
                    if (item == null) break;

                    // If the nestable item encompasses the start point, then break the nestable item up into it's inner chunks
                    if (item.Value.Span.End > endPos)
                    {
                        code.Position = item.Value.Span.Start;
                        item = code.ReadItem();
                        if (item == null) break;
                    }

                    items.Add(item.Value.AdjustOffset(startPos));
                }

                if (items.Count > 0 || lineNumber == 0) break;

                // If no items found, then back up some more
                var lineStart = _buffer.CurrentSnapshot.GetLineFromLineNumber(lineNumber - 1).Start.Position;
                lineNumber = FindLineStartNotInComment(lineStart);
                startPos = _buffer.CurrentSnapshot.GetLineFromLineNumber(lineNumber).Start.Position;
            }

            return new CodeItemSearchResults
            {
                StartParsingPosition = startPos,
                Items = items
            };
        }

        public struct CodeItemSearchResults
        {
            public int StartParsingPosition { get; set; }
            public List<CodeItem> Items { get; set; }
        }

        public ReverseCodeParser CreateReverseCodeParser(int startPosition)
        {
            return new ReverseCodeParser(this, startPosition);
        }
        #endregion
    }
}

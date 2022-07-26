using DK.Diagnostics;
using DkTools.CodeModeling;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Runtime.InteropServices;

namespace DkTools.SmartIndenting
{
	internal class ProbeSmartIndentCommandHandler : IOleCommandTarget
	{
		private ProbeSmartIndentProvider _provider;
		private IVsTextView _textViewAdapter;
		private ITextView _textView;
		private IOleCommandTarget _nextCommandHandler;

		public ProbeSmartIndentCommandHandler(IVsTextView textViewAdapter, ITextView textView, ProbeSmartIndentProvider provider)
		{
			_provider = provider;
			_textViewAdapter = textViewAdapter;
			_textView = textView;

			_textViewAdapter.AddCommandFilter(this, out _nextCommandHandler);
		}

		int IOleCommandTarget.Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
		{
			try
			{
				ThreadHelper.ThrowIfNotOnUIThread();

				char typedChar = char.MinValue;

				if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.TYPECHAR)
				{
					typedChar = (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
				}

				var retVal = _nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
				if (ErrorHandler.Failed(retVal)) return retVal;

				if (typedChar == '}' || typedChar == '#' || typedChar == ':')
				{
					var liveCodeTracker = LiveCodeTracker.GetOrCreateForTextBuffer(_textView.TextBuffer);
					if (LiveCodeTracker.IsStateInLiveCode(liveCodeTracker.GetStateForPosition(_textView.Caret.Position.BufferPosition)))
					{
						TriggerIndentReformat(typedChar);
						retVal = VSConstants.S_OK;
					}
				}

				return retVal;
			}
			catch (Exception ex)
			{
				ProbeToolsPackage.Instance.App.Log.Error(ex);
				return VSConstants.E_FAIL;
			}
		}

		int IOleCommandTarget.QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			return _nextCommandHandler.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
		}

		private ITextSnapshotLine GetCaretLine()
		{
			var caretPtTest = _textView.Caret.Position.Point.GetPoint(textBuffer => (!textBuffer.ContentType.IsOfType("projection")), PositionAffinity.Predecessor);
			if (!caretPtTest.HasValue) return null;
			var caretPt = caretPtTest.Value;

			return caretPt.GetContainingLine();
		}

		private void TriggerIndentReformat(char typedChar)
		{
			_textView.Properties.TryGetProperty<ProbeSmartIndent>(typeof(ProbeSmartIndent), out var si);
			if (si == null) return;

			ITextUndoTransaction undo = null;

			if (typedChar == '}')
            {
				var liveCodeTracker = LiveCodeTracker.GetOrCreateForTextBuffer(_textView.TextBuffer);
				var closingPt = _textView.Caret.Position.BufferPosition;
				var openingPt = liveCodeTracker.FindParentOpenBrace(closingPt - 1);
				if (openingPt.HasValue)
				{
					var openingLine = liveCodeTracker.Snapshot.GetLineFromPosition(openingPt.Value);
					var closingLine = liveCodeTracker.Snapshot.GetLineFromPosition(closingPt);

					for (int lineNumber = openingLine.LineNumber + 1; lineNumber <= closingLine.LineNumber; lineNumber++)
                    {
						var line = liveCodeTracker.Snapshot.GetLineFromLineNumber(lineNumber);

						var desiredIndent = (si as ISmartIndent).GetDesiredIndentation(line);
                        if (desiredIndent != null)
                        {
							var lineText = line.GetText();
							if (lineText.GetIndentCount(si.TabSize) != desiredIndent)
							{
								lineText = string.IsNullOrWhiteSpace(lineText) ? lineText : lineText.AdjustIndent(desiredIndent.Value, si.TabSize, si.KeepTabs);
								if (undo == null) undo = _textView.TextBuffer.CreateUndoTransaction("DK Indent Reformat");
								_textView.TextBuffer.Replace(new Span(line.Start, line.Length), lineText);
							}
						}
                    }
				}
            }
			else
			{
				var line = GetCaretLine();
				if (line == null) return;

				var desiredIndent = (si as ISmartIndent).GetDesiredIndentation(line);
				if (desiredIndent == null) return;

				var lineText = line.GetText();
				if (lineText.GetIndentCount(si.TabSize) != desiredIndent)
				{
					lineText = string.IsNullOrWhiteSpace(lineText) ? lineText : lineText.AdjustIndent(desiredIndent.Value, si.TabSize, si.KeepTabs);
					if (undo == null) undo = _textView.TextBuffer.CreateUndoTransaction("DK Indent Reformat");
					_textView.TextBuffer.Replace(new Span(line.Start, line.Length), lineText);
				}
			}

			if (undo != null)
            {
				undo.Complete();
            }
		}
	}
}

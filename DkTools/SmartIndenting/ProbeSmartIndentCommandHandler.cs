using DK.Diagnostics;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
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
					if (QuickState.IsInLiveCode(_textView.Caret.Position.BufferPosition.GetQuickState()))
					{
						TriggerIndentReformat();
						retVal = VSConstants.S_OK;
					}
				}

				return retVal;
			}
			catch (Exception ex)
			{
				Log.WriteEx(ex);
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

		private void TriggerIndentReformat()
		{
			ProbeSmartIndent si;
			_textView.Properties.TryGetProperty(typeof(ProbeSmartIndent), out si);
			if (si != null)
			{
				var line = GetCaretLine();
				if (line != null)
				{
					var desiredIndent = (si as ISmartIndent).GetDesiredIndentation(line);
					if (desiredIndent.HasValue)
					{
						var lineText = line.GetText();
						if (lineText.GetIndentCount(si.TabSize) != desiredIndent)
						{
							lineText = lineText.AdjustIndent(desiredIndent.Value, si.TabSize, si.KeepTabs);
							_textView.TextBuffer.Replace(new Span(line.Start, line.Length), lineText);
						}
					}
				}
			}
		}
	}
}

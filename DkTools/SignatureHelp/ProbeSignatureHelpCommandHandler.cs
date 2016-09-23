using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using DkTools.CodeModel;
using DkTools.CodeModel.Tokens;

namespace DkTools.SignatureHelp
{
	internal sealed class ProbeSignatureHelpCommandHandler : IOleCommandTarget
	{
		private IOleCommandTarget _nextCommandHandler;
		private ITextView _textView;
		private ISignatureHelpBroker _broker;
		private ISignatureHelpSession _session;
		private ITextStructureNavigator _navigator;

		public static char s_typedChar = char.MinValue;

		internal ProbeSignatureHelpCommandHandler(IVsTextView textViewAdapter, ITextView textView, ITextStructureNavigator nav, ISignatureHelpBroker broker)
		{
			_textView = textView;
			_broker = broker;
			_navigator = nav;

			//add this to the filter chain
			textViewAdapter.AddCommandFilter(this, out _nextCommandHandler);
		}

		public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
		{
			try
			{
				char typedChar = char.MinValue;

				if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.TYPECHAR)
				{
					typedChar = (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
					if (typedChar == '(')
					{
						if (_textView.Caret.Position.BufferPosition.IsInLiveCode())
						{
							SnapshotPoint point = _textView.Caret.Position.BufferPosition;
							var pos = point.Position;
							var lineText = point.Snapshot.GetLineTextUpToPosition(pos).TrimEnd();

							if (lineText.Length > 0 && lineText[lineText.Length - 1].IsWordChar(false))
							{
								if (_session != null && !_session.IsDismissed) _session.Dismiss();
								s_typedChar = typedChar;
								_session = _broker.TriggerSignatureHelp(_textView);
							}
						}
					}
					else if (typedChar == ')' && _session != null)
					{
						if (!_session.IsDismissed) _session.Dismiss();
						_session = null;
					}
					else if (typedChar == ',' && (_session == null || _session.IsDismissed))
					{
						if (_textView.Caret.Position.BufferPosition.IsInLiveCode())
						{
							var fileStore = CodeModel.FileStore.GetOrCreateForTextBuffer(_textView.TextBuffer);
							if (fileStore != null)
							{
								var model = fileStore.GetMostRecentModel(_textView.TextSnapshot, "Signature help command handler - after ','");
								var caretPos = _textView.Caret.Position.BufferPosition.TranslateTo(model.Snapshot, PointTrackingMode.Negative).Position;

								var argsToken = model.File.FindDownward<CodeModel.Tokens.ArgsToken>(caretPos).LastOrDefault();
								if (argsToken != null)
								{
									s_typedChar = typedChar;
									_session = _broker.TriggerSignatureHelp(_textView);
								}
							}
						}
					}
				}
				return _nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
			}
			catch (Exception ex)
			{
				Log.WriteEx(ex);
				return VSConstants.E_FAIL;
			}
		}

		public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
		{
			return _nextCommandHandler.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
		}
	}
}

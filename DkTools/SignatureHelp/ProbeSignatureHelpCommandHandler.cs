using DK;
using DK.Code;
using DK.Diagnostics;
using DkTools.CodeModeling;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace DkTools.SignatureHelp
{
	internal sealed class ProbeSignatureHelpCommandHandler : IOleCommandTarget
	{
		private IOleCommandTarget _nextCommandHandler;
		private ITextView _textView;
		private ISignatureHelpBroker _broker;
		private ISignatureHelpSession _session;
		private ProbeSignatureHelpCommandProvider _provider;

		public static char s_typedChar = char.MinValue;

		internal ProbeSignatureHelpCommandHandler(
			IVsTextView textViewAdapter,
			ITextView textView,
			ISignatureHelpBroker signatureHelpBroker,
			ProbeSignatureHelpCommandProvider signatureHelpCommandProvider)
		{
			_textView = textView ?? throw new ArgumentNullException(nameof(textView));
			_broker = signatureHelpBroker ?? throw new ArgumentNullException(nameof(signatureHelpBroker));
			_provider = signatureHelpCommandProvider ?? throw new ArgumentNullException(nameof(signatureHelpCommandProvider));

			//add this to the filter chain
			textViewAdapter.AddCommandFilter(this, out _nextCommandHandler);

			_textView.Caret.PositionChanged += Caret_PositionChanged;
		}

		public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
		{
			try
			{
				ThreadHelper.ThrowIfNotOnUIThread();

				if (VsShellUtilities.IsInAutomationFunction(_provider.ServiceProvider))
				{
					return _nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
				}

				var commandId = nCmdID;
				var typedChar = char.MinValue;

				if (pguidCmdGroup == typeof(VSConstants.VSStd97CmdID).GUID)
				{
					if (nCmdID == (uint)VSConstants.VSStd97CmdID.GotoDefn)
					{
						Navigation.GoToDefinitionHelper.TriggerGoToDefinition(_textView, CancellationToken.None);
						return VSConstants.S_OK;
					}
					else if (nCmdID == (uint)VSConstants.VSStd97CmdID.FindReferences)
					{
						Navigation.GoToDefinitionHelper.TriggerFindReferences(_textView, CancellationToken.None);
						return VSConstants.S_OK;
					}
				}
				else if (pguidCmdGroup == VSConstants.VSStd2K)
				{
					if (nCmdID == (uint)VSConstants.VSStd2KCmdID.TYPECHAR)
					{
						var liveCodeTracker = LiveCodeTracker.GetOrCreateForTextBuffer(_textView.TextBuffer);
						if (LiveCodeTracker.IsStateInLiveCode(liveCodeTracker.GetStateForPosition(_textView.Caret.Position.BufferPosition)))
						{
							typedChar = (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
							if (typedChar == '(')
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
							else if (typedChar == ')' && _session != null)
							{
								if (!_session.IsDismissed) _session.Dismiss();
								_session = null;
							}
							else if (typedChar == ',' && (_session == null || _session.IsDismissed))
							{
								var revCode = liveCodeTracker.CreateReverseCodeParser(_textView.Caret.Position.BufferPosition);
								CodeItem? item;
								var foundOpenBracket = false;
								while ((item = revCode.GetPreviousItemNestable("{", "[", ";")) != null)
                                {
									if (item.Value.Type == CodeType.Operator && item.Value.Text == "(")
                                    {
										foundOpenBracket = true;
										break;
                                    }
                                }

								if (foundOpenBracket)
                                {
									var prevItem = revCode.GetPreviousItem();
									if (prevItem?.Type == CodeType.Word)
                                    {
										s_typedChar = typedChar;
										_session = _broker.TriggerSignatureHelp(_textView);
                                    }
                                }
							}
						}
					}
					else if (nCmdID == (uint)VSConstants.VSStd2KCmdID.GOTOBRACE)
					{
						Navigation.GoToBraceHelper.Trigger(_textView, false, CancellationToken.None);
						return VSConstants.S_OK;
					}
					else if (nCmdID == (uint)VSConstants.VSStd2KCmdID.GOTOBRACE_EXT)
					{
						Navigation.GoToBraceHelper.Trigger(_textView, true, CancellationToken.None);
						return VSConstants.S_OK;
					}
					else if (nCmdID == (uint)VSConstants.VSStd2KCmdID.COMMENT_BLOCK)
					{
						Tagging.Tagger.CommentBlock();
						return VSConstants.S_OK;
					}
					else if (nCmdID == (uint)VSConstants.VSStd2KCmdID.UNCOMMENT_BLOCK)
					{
						Tagging.Tagger.UncommentBlock();
						return VSConstants.S_OK;
					}
				}

				return _nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
			}
			catch (Exception ex)
			{
				ProbeToolsPackage.Log.Error(ex);
				return VSConstants.E_FAIL;
			}
		}

		public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var status = _nextCommandHandler.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);

			if (pguidCmdGroup == typeof(VSConstants.VSStd97CmdID).GUID)
			{
				for (int i = 0; i < cCmds; i++)
				{
					if (prgCmds[i].cmdID == (uint)VSConstants.VSStd97CmdID.FindReferences ||
						prgCmds[i].cmdID == (uint)VSConstants.VSStd97CmdID.GotoDefn)
					{
						prgCmds[i].cmdf = (uint)(OLECMDF.OLECMDF_ENABLED | OLECMDF.OLECMDF_SUPPORTED);
					}
					else if (prgCmds[i].cmdID == (uint)VSConstants.VSStd97CmdID.GotoDecl ||
						prgCmds[i].cmdID == (uint)VSConstants.VSStd97CmdID.GotoRef)
					{
						prgCmds[i].cmdf |= (uint)OLECMDF.OLECMDF_DEFHIDEONCTXTMENU;
					}
				}
			}

			return status;
		}

		private void Caret_PositionChanged(object sender, CaretPositionChangedEventArgs e)
		{
			if (_session != null && !_session.IsDismissed)
			{
				var caretPtTest = e.NewPosition.Point.GetPoint(_textView.TextSnapshot, PositionAffinity.Successor);
				if (caretPtTest.HasValue)
				{
					var caretPt = caretPtTest.Value;
					if (_session.SelectedSignature.ApplicableToSpan.GetSpan(caretPt.Snapshot).Contains(caretPt))
					{
						var sig = _session.SelectedSignature as ProbeSignature;
						if (sig != null)
						{
							sig.ComputeCurrentParameter(caretPt);
						}
					}
				}
			}
		}
	}
}

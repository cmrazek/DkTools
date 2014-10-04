﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;

namespace DkTools.StatementCompletion
{
	internal class ProbeCompletionCommandHandler : IOleCommandTarget
	{
		private IOleCommandTarget _nextCommandHandler;
		private ITextView _textView;
		private IVsTextView _textViewAdapter;
		private ProbeCompletionCommandHandlerProvider _provider;
		private ICompletionSession _session;
		private int _insideCommentLineStart = -1;
		private bool _insideComment;

		private static readonly Regex _rxAfterAssignOrCompare = new Regex(@"(?:==|=|!=)\s$");
		private static readonly Regex _rxFunctionStartBracket = new Regex(@"\w+\s*\($");
		private static readonly Regex _rxAutoCompleteKeyword = new Regex(@"\b(return|case|extract|permanent)\s$");
		private static readonly Regex _rxAfterIfDef = new Regex(@"\#ifn?def\s$");
		private static readonly Regex _rxAfterInclude = new Regex(@"\#include\s+(?:\<|\"")$");
		private static readonly Regex _rxOrderBy = new Regex(@"\border\s+by\s$");
		private static readonly Regex _rxTag = new Regex(@"\btag\s$");

		public ProbeCompletionCommandHandler(IVsTextView textViewAdapter, ITextView textView, ProbeCompletionCommandHandlerProvider provider)
		{
			_textView = textView;
			_textViewAdapter = textViewAdapter;
			_provider = provider;

			textViewAdapter.AddCommandFilter(this, out _nextCommandHandler);
		}

		int IOleCommandTarget.QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
		{
			return _nextCommandHandler.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
		}

		int IOleCommandTarget.Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
		{
			try
			{
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
						Navigation.GoToDefinitionHelper.TriggerGoToDefinition(_textView);
						return VSConstants.S_OK;
					}
				}

				// Make sure the input is a char.
				if (pguidCmdGroup == VSConstants.VSStd2K)
				{
					if (nCmdID == (uint)VSConstants.VSStd2KCmdID.TYPECHAR)
					{
						typedChar = (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
					}
					else if (nCmdID == (uint)VSConstants.VSStd2KCmdID.GOTOBRACE)
					{
						Navigation.GoToBraceHelper.Trigger(_textView, false);
						return VSConstants.S_OK;
					}
					else if (nCmdID == (uint)VSConstants.VSStd2KCmdID.GOTOBRACE_EXT)
					{
						Navigation.GoToBraceHelper.Trigger(_textView, true);
						return VSConstants.S_OK;
					}
				}

				if (_session != null && !_session.IsDismissed)
				{
					// List is still displayed.

					if (typedChar != char.MinValue && (char.IsWhiteSpace(typedChar) || char.IsPunctuation(typedChar)) && typedChar != '_')
					{
						// User typed a char that would end the session.
						_session.Dismiss();
					}
					//if (typedChar != char.MinValue && (char.IsWhiteSpace(typedChar) || char.IsPunctuation(typedChar)) && typedChar != '_')
					//{
					//    // Dismiss session but allow it to be added to the buffer.
					//    if (_session.SelectedCompletionSet.SelectionStatus.IsSelected) _session.Commit();
					//    else _session.Dismiss();
					//}
					else if (nCmdID == (uint)VSConstants.VSStd2KCmdID.RETURN || nCmdID == (uint)VSConstants.VSStd2KCmdID.TAB)
					{
						// User pressed return or tab.
						// Dismiss session, but don't add char to buffer.
						if (_session.SelectedCompletionSet.SelectionStatus.IsSelected)
						{
							_session.Commit();
							return VSConstants.S_OK;    // Don't add char to the buffer.
						}
						else _session.Dismiss();
					}
				}

				var retVal = _nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
				var handled = false;

				if (nCmdID == (uint)VSConstants.VSStd2KCmdID.COMPLETEWORD)
				{
					// User called Complete Word command.
					if (_session == null || _session.IsDismissed) TriggerCompletionIfAllowed(false);
					if (_session != null) _session.Filter();
					handled = true;
				}
				else if (typedChar != char.MinValue && (char.IsLetterOrDigit(typedChar) || typedChar == '_' || typedChar == '.'))
				{
					// User typed a char that should start a session.
					if (_session == null || _session.IsDismissed) TriggerCompletionIfAllowed(false);
					if (_session != null) _session.Filter();
					handled = true;
				}
				else if (commandId == (uint)VSConstants.VSStd2KCmdID.BACKSPACE || commandId == (uint)VSConstants.VSStd2KCmdID.DELETE)
				{
					if (_session != null && !_session.IsDismissed) _session.Filter();
					handled = true;
				}
				else if (typedChar == ' ')
				{
					if (_session == null || _session.IsDismissed)
					{
						var caretPos = _textView.Caret.Position.BufferPosition.Position;
						var prefix = _textView.TextBuffer.CurrentSnapshot.GetLineTextUpToPosition(caretPos);

						if (prefix.EndsWith(", ") ||
							_rxAutoCompleteKeyword.IsMatch(prefix) ||
							_rxAfterIfDef.IsMatch(prefix) ||
							_rxOrderBy.IsMatch(prefix) ||
							_rxTag.IsMatch(prefix))
						{
							TriggerCompletionIfAllowed(false);
						}
						else if (_rxAfterAssignOrCompare.IsMatch(prefix))
						{
							TriggerCompletionIfAllowed(true);	// Requires a model rebuild in order to tell what token is before the '='.
						}
					}
				}
				else if (typedChar == '(')
				{
					if (_session == null || _session.IsDismissed)
					{
						var caretPos = _textView.Caret.Position.BufferPosition.Position;
						var prefix = _textView.TextBuffer.CurrentSnapshot.GetLineTextUpToPosition(caretPos);

						if (_rxFunctionStartBracket.IsMatch(prefix)) TriggerCompletionIfAllowed(false);
					}
				}
				else if (typedChar == '<' || typedChar == '\"')
				{
					if (_session == null || _session.IsDismissed)
					{
						var caretPos = _textView.Caret.Position.BufferPosition.Position;
						var prefix = _textView.TextBuffer.CurrentSnapshot.GetLineTextUpToPosition(caretPos);

						if (_rxAfterInclude.IsMatch(prefix)) TriggerCompletionIfAllowed(false, allowInsideString: true);
					}
				}

				if (handled) return VSConstants.S_OK;
				return retVal;
			}
			catch (Exception ex)
			{
				Log.WriteEx(ex);
				return VSConstants.E_FAIL;
			}
		}

		private bool TriggerCompletionIfAllowed(bool modelRebuildRequired, bool allowInsideString = false)
		{
			// Get caret point
			var caretPtTest = _textView.Caret.Position.Point.GetPoint(textBuffer => (!textBuffer.ContentType.IsOfType("projection")), PositionAffinity.Predecessor);
			if (!caretPtTest.HasValue) return false;
			var caretPt = caretPtTest.Value;

			// Check if completion is allowed at this point.
			// Read the line text up to this point to see what state the text is in.
			var line = caretPt.GetContainingLine();
			var prefixLength = caretPt.Position - line.Start.Position;
			if (prefixLength > 0)
			{
				var prefixText = line.GetText().Substring(0, prefixLength);

				var parser = new TokenParser.Parser(prefixText);
				parser.ReturnComments = true;

				var lastTokenType = TokenParser.TokenType.WhiteSpace;
				var lastTokenTerminated = true;
				while (parser.Read())
				{
					lastTokenType = parser.TokenType;
					lastTokenTerminated = parser.TokenTerminated;
				}

				if (((lastTokenType == TokenParser.TokenType.StringLiteral && !allowInsideString) || lastTokenType == TokenParser.TokenType.Comment) && !lastTokenTerminated)
				{
					// User is in the middle of typing a string literal or comment
					return false;
				}
			}

			// Check if inside a multi-line comment.
			if (line.Start.Position == _insideCommentLineStart)
			{
				if (_insideComment) return false;
			}
			else
			{
				// Check the comment state at the beginning of the line.  Remember this for typing subsequent chars later.
				var commentTracker = new TokenParser.CommentTracker(caretPt.Snapshot.GetText());
				_insideCommentLineStart = line.Start.Position;
				_insideComment = commentTracker.PositionInsideComment(line.Start.Position);

				// Check the comment state at the typing position.
				commentTracker = new TokenParser.CommentTracker(line.GetText(), _insideComment);
				if (commentTracker.PositionInsideComment(caretPt.Position - line.Start.Position))
				{
					return false;
				}
			}

			if (!modelRebuildRequired)
			{
				// Start the new session
				//_session = _provider.CompletionBroker.CreateCompletionSession(_textView, caretPt.Snapshot.CreateTrackingPoint(caretPt.Position, PointTrackingMode.Positive), true);
				//_session.Dismissed += OnSessionDismissed;
				//_session.Start();

				ShowSession(caretPt);
			}
			else
			{
				var modelSnapshot = _textView.TextSnapshot;
				var fileStore = CodeModel.FileStore.GetOrCreateForTextBuffer(caretPt.Snapshot.TextBuffer);
				if (fileStore != null)
				{
					ProbeToolsPackage.Instance.StartBackgroundWorkItem(() =>
						{
							fileStore.GetCurrentModel(caretPt.Snapshot, "Auto-completion deferred model build");
						}, ex =>
						{
							if (ex != null) return;

							if (_session == null || _session.IsDismissed)
							{
								if (_textView.TextSnapshot.Version.VersionNumber == modelSnapshot.Version.VersionNumber)
								{
									ShowSession(caretPt);
								}
							}
						});
				}
			}

			return true;
		}

		private void ShowSession(SnapshotPoint caretPt)
		{
			// This function must only be called from the main thread.
			if (_session == null || _session.IsDismissed)
			{
				_session = _provider.CompletionBroker.CreateCompletionSession(_textView, caretPt.Snapshot.CreateTrackingPoint(caretPt.Position, PointTrackingMode.Positive), true);
				_session.Dismissed += OnSessionDismissed;
				_session.Start();
			}
		}

		private void OnSessionDismissed(object sender, EventArgs e)
		{
			_session.Dismissed -= OnSessionDismissed;
			_session = null;
		}
	}
}

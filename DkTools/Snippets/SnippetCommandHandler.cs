using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace DkTools.Snippets
{
	internal sealed class SnippetCommandHandler : IOleCommandTarget, IVsExpansionClient
	{
		private ITextView _view;
		private IVsTextView _viewAdapter;
		private IOleCommandTarget _nextCommandHandler;
		private IVsExpansionManager _exManager;
		private IVsExpansionSession _exSession;

		private static readonly Regex _rxWordEnd = new Regex(@"([A-Za-z0-9_#]+)$");

		public SnippetCommandHandler(ITextView view, IVsTextView textViewAdapter)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			_view = view;
			_viewAdapter = textViewAdapter;

			ProbeToolsPackage.Instance.TextManager2Service.GetExpansionManager(out _exManager);

			textViewAdapter.AddCommandFilter(this, out _nextCommandHandler); 
		}

		public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			char typedChar = '\0';

			if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.TYPECHAR)
			{
				typedChar = (char)(ushort)Marshal.GetObjectForNativeVariant(pvaIn);
			}

			if (pguidCmdGroup == VSConstants.VSStd2K && nCmdID == (uint)VSConstants.VSStd2KCmdID.INSERTSNIPPET)
			{
				if (_exManager != null)
				{
					int result = _exManager.InvokeInsertionUI(_viewAdapter, this, GuidList.guidSnippets, null, 0, 0, null, 0, 0, "DK Snippets", string.Empty);
					return VSConstants.S_OK;
				}
			}

			if (_exSession != null)
			{
				if (nCmdID == (uint)VSConstants.VSStd2KCmdID.BACKTAB)
				{
					_exSession.GoToPreviousExpansionField();
					return VSConstants.S_OK;
				}

				if (nCmdID == (uint)VSConstants.VSStd2KCmdID.TAB)
				{
					_exSession.GoToNextExpansionField(0);	// 0 to cycle through fields
					return VSConstants.S_OK;
				}

				if (nCmdID == (uint)VSConstants.VSStd2KCmdID.RETURN || nCmdID == (uint)VSConstants.VSStd2KCmdID.CANCEL)
				{
					if (_exSession.EndCurrentExpansion(0) == VSConstants.S_OK)
					{
						_exSession = null;
						return VSConstants.S_OK;
					}
				}
			}
			else if (_exSession == null && nCmdID == (uint)VSConstants.VSStd2KCmdID.TAB)
			{
				var caretPos = _view.Caret.Position.BufferPosition;
				var prefix = _view.TextSnapshot.GetLineTextUpToPosition(caretPos);

				var match = _rxWordEnd.Match(prefix);
				if (match.Success)
				{
					var word = match.Groups[1].Value;
					if (InsertAnyExpansion(word, null, null)) return VSConstants.S_OK;
				}
			}

			return _nextCommandHandler.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
		}

		public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (!VsShellUtilities.IsInAutomationFunction(ProbeToolsPackage.Instance))
			{
				if (pguidCmdGroup == VSConstants.VSStd2K && cCmds > 0)
				{
					if (prgCmds[0].cmdID == (uint)VSConstants.VSStd2KCmdID.INSERTSNIPPET)
					{
						prgCmds[0].cmdf = (int)Microsoft.VisualStudio.OLE.Interop.Constants.MSOCMDF_ENABLED | (int)Microsoft.VisualStudio.OLE.Interop.Constants.MSOCMDF_SUPPORTED;
						return VSConstants.S_OK;
					}
				}
			}

			return _nextCommandHandler.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
		}

		public int EndExpansion()
		{
			_exSession = null;
			return VSConstants.S_OK;
		}

		public int FormatSpan(IVsTextLines pBuffer, TextSpan[] ts)
		{
            ThreadHelper.ThrowIfNotOnUIThread();

            var buf = ProbeToolsPackage.Instance.EditorAdaptersService.GetDocumentBuffer(pBuffer);
			if (buf != null)
			{
				var tabSize = _view.GetTabSize();
				var keepTabs = _view.GetKeepTabs();
				var appSettings = DkEnvironment.CurrentAppSettings;

				foreach (var span in ts)
				{
					SmartIndenting.ProbeSmartIndent.FixIndentingBetweenLines(buf, span.iStartLine, span.iEndLine, tabSize, keepTabs, appSettings);
				}
			}

			return VSConstants.S_OK;
		}

		public int GetExpansionFunction(MSXML.IXMLDOMNode xmlFunctionNode, string bstrFieldName, out IVsExpansionFunction pFunc)
		{
			pFunc = null;
			return VSConstants.S_OK;
		}

		public int IsValidKind(IVsTextLines pBuffer, TextSpan[] ts, string bstrKind, out int pfIsValidKind)
		{
			pfIsValidKind = 1;
			return VSConstants.S_OK;
		}

		public int IsValidType(IVsTextLines pBuffer, TextSpan[] ts, string[] rgTypes, int iCountTypes, out int pfIsValidType)
		{
			pfIsValidType = 1;
			return VSConstants.S_OK;
		}

		public int OnAfterInsertion(IVsExpansionSession pSession)
		{
			//TextSpan[] pts = new TextSpan[1];
			//if (pSession.GetSnippetSpan(pts) == VSConstants.S_OK)
			//{
			//	var span = pts[0];

			//	SmartIndenting.ProbeSmartIndent smartIndent;
			//	if (_view.Properties.TryGetProperty(typeof(SmartIndenting.ProbeSmartIndent), out smartIndent) && smartIndent != null)
			//	{
			//		smartIndent.FixIndentingBetweenLines(span.iStartLine, span.iEndLine);
			//	}
			//	else
			//	{
			//		Log.WriteDebug("Failed to get smart indent object for view.");
			//	}
			//}

			return VSConstants.S_OK;
		}

		public int OnBeforeInsertion(IVsExpansionSession pSession)
		{
			return VSConstants.S_OK;
		}

		public int OnItemChosen(string pszTitle, string pszPath)
		{
			InsertAnyExpansion(null, pszTitle, pszPath);
			return VSConstants.S_OK;
		}

		public int PositionCaretForEditing(IVsTextLines pBuffer, TextSpan[] ts)
		{
			return VSConstants.S_OK;
		}

		private bool InsertAnyExpansion(string shortcut, string title, string path)
		{
			if (_exManager == null) return false;

			int startLine, endCol;
			_viewAdapter.GetCaretPos(out startLine, out endCol);

			var addSpan = new TextSpan();
			addSpan.iStartIndex = endCol;
			addSpan.iEndIndex = endCol;
			addSpan.iStartLine = startLine;
			addSpan.iEndLine = startLine;

			if (shortcut != null)
			{
				addSpan.iStartIndex = addSpan.iEndIndex - shortcut.Length;	// To the start of the word typed

				_exManager.GetExpansionByShortcut(this, GuidList.guidSnippets, shortcut, _viewAdapter, new TextSpan[] { addSpan }, 0, out path, out title);
			}

			if (title != null && path != null)
			{
				IVsTextLines textLines;
				_viewAdapter.GetBuffer(out textLines);

				var bufferExpansion = textLines as IVsExpansion;
				if (bufferExpansion != null)
				{
					var result = bufferExpansion.InsertNamedExpansion(title, path, addSpan, this, GuidList.guidSnippets, 0, out _exSession);
					if (result == VSConstants.S_OK) return true;
				}
			}

			return false;
		}
	}
}

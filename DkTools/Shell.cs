using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace DkTools
{
	internal static class Shell
	{
		private static EnvDTE.DTE _dte;

		internal static void ShowError(Exception ex)
		{
			Log.WriteEx(ex);
			System.Windows.Forms.MessageBox.Show(string.Concat(ex.GetType().ToString(), "\r\n\r\n", ex.ToString()), "Error",
				System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
		}

		internal static void ShowError(string text)
		{
			Log.Write(LogLevel.Error, text);
			System.Windows.Forms.MessageBox.Show(text, "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
		}

		internal static void OpenDocument(string fileName)
		{
			VsShellUtilities.OpenDocument(ProbeToolsPackage.Instance, fileName);
		}

		internal static void OpenDocument(string fileName, CodeModel.Span selectSpan)
		{
			try
			{
				IVsTextView view;
				IVsWindowFrame windowFrame;
				OpenDocument(fileName, out view, out windowFrame);
				ErrorHandler.ThrowOnFailure(windowFrame.Show());
				ErrorHandler.ThrowOnFailure(view.SetSelection(selectSpan.Start.LineNum, selectSpan.Start.LinePos, selectSpan.End.LineNum, selectSpan.End.LinePos));
				ErrorHandler.ThrowOnFailure(view.CenterLines(selectSpan.Start.LineNum, 1));
			}
			catch (Exception ex)
			{
				ShowError(ex);
			}
		}

		private static void OpenDocument(string fileName, out IVsTextView view, out IVsWindowFrame windowFrame)
		{
			try
			{
				IVsUIHierarchy uiHierarchy;
				uint itemId;
				VsShellUtilities.OpenDocument(ProbeToolsPackage.Instance, fileName, Guid.Empty, out uiHierarchy, out itemId, out windowFrame, out view);
			}
			catch (Exception ex)
			{
				ShowError(ex);
				view = null;
				windowFrame = null;
			}
		}

		public static void OpenTempContent(string content, string fileTitle, string ext)
		{
			string fileName;
			using (var tempFile = new TempFileOutput(fileTitle, ext))
			{
				tempFile.WriteLine(content);
				fileName = tempFile.FileName;
			}
			OpenDocument(fileName);
		}

		internal static EnvDTE.DTE DTE
		{
			get
			{
				if (_dte == null)
				{
					_dte = Package.GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
					if (_dte == null) throw new InvalidOperationException("Unable to query for DTE object.");
				}
				return _dte;
			}
		}

		internal static OutputPane CreateOutputPane(Guid paneGuid, string name)
		{
			// http://msdn.microsoft.com/en-us/library/bb187346%28v=vs.80%29.aspx

			var output = ProbeToolsPackage.Instance.OutputWindowService;
			if (output == null) throw new InvalidOperationException("Unable to get SVsOutputWindow service.");

			IVsOutputWindowPane pane;
			var guid = paneGuid;
			int retVal;

			if (ErrorHandler.Failed(retVal = output.GetPane(ref guid, out pane)) || pane == null)
			{
				if (ErrorHandler.Failed(retVal = output.CreatePane(ref guid, name, 1, 1))) throw new InvalidOperationException(string.Format("Error when creating new output pane: {0:X8}", retVal));
				if (ErrorHandler.Failed(retVal = output.GetPane(ref guid, out pane))) throw new InvalidOperationException(string.Format("Error when getting new output pane: {0:X8}", retVal));
				if (pane == null) throw new InvalidOperationException("SVsOutputWindow.GetPane returned null.");
			}

			var outputPane = new OutputPane(pane, true);
			return outputPane;
		}

		internal static void ShowErrorList()
		{
			ProbeToolsPackage.Instance.ErrorListService.BringToFront();
		}

		internal static void SetStatusText(string text)
		{
			ProbeToolsPackage.Instance.StatusBarService.SetText(text);
		}

		internal static ITextBuffer ActiveBuffer
		{
			get
			{
				var view = ActiveView;
				if (view != null) return view.TextBuffer;
				return null;
			}
		}

		internal static IWpfTextView ActiveView
		{
			get
			{
				IVsTextView vsView;
				if (ErrorHandler.Failed(ProbeToolsPackage.Instance.TextManagerService.GetActiveView(1, null, out vsView))) return null;
				if (vsView == null) return null;

				return ProbeToolsPackage.Instance.EditorAdaptersService.GetWpfTextView(vsView);
			}
		}

		internal static ITextBuffer GetBufferForVsTextBuffer(IVsTextBuffer vsBuf)
		{
			return ProbeToolsPackage.Instance.EditorAdaptersService.GetDocumentBuffer(vsBuf);
		}

		internal static IWpfTextView VsTextViewToWpfTextView(IVsTextView vsView)
		{
			return ProbeToolsPackage.Instance.EditorAdaptersService.GetWpfTextView(vsView);
		}

		internal static void ShowFindInFiles(IEnumerable<string> searchDirs)
		{
			var dte = Shell.DTE;
			dte.ExecuteCommand("Edit.FindinFiles");

			if (searchDirs != null)
			{
				var sb = new StringBuilder();
				foreach (var dir in searchDirs)
				{
					if (string.IsNullOrWhiteSpace(dir)) continue;
					if (sb.Length > 0) sb.Append(";");
					sb.Append(dir);
				}
				dte.Find.SearchPath = sb.ToString();
			}

			dte.Find.SearchSubfolders = true;
			dte.Find.FilesOfType = "";
		}

		internal static ProbeExplorer.ProbeExplorerToolWindow ShowProbeExplorerToolWindow()
		{
			var window = ProbeToolsPackage.Instance.FindToolWindow(typeof(ProbeExplorer.ProbeExplorerToolWindow), 0, true) as ProbeExplorer.ProbeExplorerToolWindow;
			if (window == null || window.Frame == null)
			{
				throw new NotSupportedException("Unable to create Probe Explorer tool window.");
			}

			ErrorHandler.ThrowOnFailure((window.Frame as IVsWindowFrame).Show());
			return window;
		}
	}
}

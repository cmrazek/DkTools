using DK.Code;
using DK.Diagnostics;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Collections.Generic;
using System.Text;

namespace DkTools
{
	internal static class Shell
	{
		private static EnvDTE.DTE _dte;

		internal static void ShowError(Exception ex)
		{
			Log.WriteEx(ex);
			Util.ShowErrorDialog(ex.Message, ex.ToString());
		}

		internal static void ShowError(string text)
		{
			Log.Write(LogLevel.Error, text);
			Util.ShowErrorDialog(text, null);
		}

		internal static void OpenDocument(string fileName)
		{
			ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

				VsShellUtilities.OpenDocument(ProbeToolsPackage.Instance, fileName);
			});
		}

		internal static void OpenDocument(string fileName, CodeSpan selectSpan)
		{
			ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

				try
				{
					IVsTextView view;
					IVsWindowFrame windowFrame;
					OpenDocument(fileName, out view, out windowFrame);
					ErrorHandler.ThrowOnFailure(windowFrame.Show());

					if (selectSpan.Start > 0 || selectSpan.End > 0)
					{
						if (selectSpan.End > selectSpan.Start)
						{
							int startLine, startCol;
							view.GetLineAndColumn(selectSpan.Start, out startLine, out startCol);

							int endLine, endCol;
							view.GetLineAndColumn(selectSpan.End, out endLine, out endCol);

							view.SetSelection(startLine, startCol, endLine, endCol);
							view.CenterLines(startLine, 1);
						}
						else
						{
							int startLine, startCol;
							view.GetLineAndColumn(selectSpan.Start, out startLine, out startCol);
							view.SetSelection(startLine, startCol, startLine, startCol);
							view.CenterLines(startLine, 1);
						}
					}
				}
				catch (Exception ex)
				{
					ShowError(ex);
				}
			});
		}

		internal static void OpenDocumentAndLine(string fileName, int lineNum)
		{
			ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

				try
				{
					IVsTextView view;
					IVsWindowFrame windowFrame;
					OpenDocument(fileName, out view, out windowFrame);
					ErrorHandler.ThrowOnFailure(windowFrame.Show());

					view.SetSelection(lineNum, 0, lineNum, 0);
					view.CenterLines(lineNum, 1);
				}
				catch (Exception ex)
				{
					ShowError(ex);
				}
			});
		}

		internal static void OpenDocument(string fileName, int pos)
		{
			ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

				if (pos < 0) pos = 0;
				OpenDocument(fileName, new CodeSpan(pos, pos));
			});
		}

		internal static void OpenDocument(FilePosition filePos)
		{
			if (filePos.IsEmpty) throw new ArgumentException("File position is empty.");

			ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

				OpenDocument(filePos.FileName, new CodeSpan(filePos.Position, filePos.Position));
			});
		}

		private static void OpenDocument(string fileName, out IVsTextView view, out IVsWindowFrame windowFrame)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

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
				ThreadHelper.ThrowIfNotOnUIThread();

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

			ThreadHelper.ThrowIfNotOnUIThread();

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

		internal static ITextBuffer ActiveBuffer
		{
			get
			{
				ThreadHelper.ThrowIfNotOnUIThread();

				return ActiveView?.TextBuffer;
			}
		}

		internal static IWpfTextView ActiveView
		{
			get
			{
				ThreadHelper.ThrowIfNotOnUIThread();

				IVsTextView vsView;
				if (ErrorHandler.Failed(ProbeToolsPackage.Instance.TextManagerService.GetActiveView(1, null, out vsView))) return null;
				if (vsView == null) return null;

				return ProbeToolsPackage.Instance?.EditorAdaptersService?.GetWpfTextView(vsView);
			}
		}

		internal static IWpfTextView VsTextViewToWpfTextView(IVsTextView vsView)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			return ProbeToolsPackage.Instance?.EditorAdaptersService?.GetWpfTextView(vsView);
		}

		internal static void ShowFindInFiles(IEnumerable<string> searchDirs)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

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

		internal static ProbeExplorer.ProbeExplorerToolWindow GetProbeExplorerToolWindow()
		{
			var window = ProbeToolsPackage.Instance.FindToolWindow(typeof(ProbeExplorer.ProbeExplorerToolWindow), 0, true) as ProbeExplorer.ProbeExplorerToolWindow;
			if (window == null || window.Frame == null)
			{
				throw new NotSupportedException("Unable to create Probe Explorer tool window.");
			}

			return window;
		}

		internal static ProbeExplorer.ProbeExplorerToolWindow ShowProbeExplorerToolWindow()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			var window = GetProbeExplorerToolWindow();
			ErrorHandler.ThrowOnFailure((window.Frame as IVsWindowFrame).Show());
			return window;
		}

		public static void OnTextViewActivated(IWpfTextView view)
		{
            ThreadHelper.ThrowIfNotOnUIThread();

            var window = GetProbeExplorerToolWindow();
			window.OnDocumentActivated(view);
		}

		public static void ShowNotificationAsync(string message, string caption)
		{
			ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

				System.Windows.MessageBox.Show(message, caption, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
			});
		}

		public static void Status(string text)
		{
			ProbeToolsPackage.Instance.SetStatusText(text);
		}
	}
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;

namespace DkTools.ProbeExplorer
{
	//[Guid("64371DE0-6D8A-4F1F-9368-E68D3A194387")]
	internal class ProbeExplorerToolWindow : ToolWindowPane
	{
		private ProbeExplorerControl _exp;

		public ProbeExplorerToolWindow()
		{
			Caption = "DK Explorer";
			Content = _exp = new ProbeExplorerControl();

			_exp.FileOpenRequested += new EventHandler<ProbeExplorerControl.OpenFileEventArgs>(Explorer_FileOpenRequested);
		}

		void Explorer_FileOpenRequested(object sender, ProbeExplorerControl.OpenFileEventArgs e)
		{
			try
			{
				Shell.OpenDocument(e.FileName);
			}
			catch (Exception ex)
			{
				Log.WriteEx(ex);
			}
		}

		public void FocusFileFilter()
		{
			_exp.FocusFileFilter();
		}

		public override void OnToolWindowCreated()
		{
			ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

				base.OnToolWindowCreated();

				var windowFrame = base.Frame as IVsWindowFrame;
				if (windowFrame != null)
				{
					windowFrame.SetProperty((int)__VSFPROPID4.VSFPROPID_TabImage, Res.ProbeExplorerImg.GetHbitmap());
				}
			});
		}

		public void FocusTable(string tableName)
		{
			_exp.FocusTable(tableName);
		}

		public void FocusTableField(string tableName, string fieldName)
		{
			_exp.FocusTableField(tableName, fieldName);
		}

		public void FocusTableRelInd(string tableName, string relIndName)
		{
			_exp.FocusTableRelInd(tableName, relIndName);
		}

		public void OnDocumentActivated(Microsoft.VisualStudio.Text.Editor.IWpfTextView view)
		{
			_exp.OnDocumentActivated(view);
		}

		public void FocusFunctionFilter()
		{
			_exp.FocusFunctionFilter();
		}

		public void FocusDictFilter()
		{
			_exp.FocusDictFilter();
		}
	}
}

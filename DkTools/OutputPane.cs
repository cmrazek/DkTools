using DK.Diagnostics;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;

namespace DkTools
{
	class OutputPane : Output
	{
		private IVsOutputWindowPane _pane;

		public OutputPane(IVsOutputWindowPane pane, bool activate)
		{
			if (pane == null) throw new ArgumentNullException("pane");
			_pane = pane;

			if (activate)
			{
				ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
				{
					await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

					_pane.Activate();
				});
			}
		}

		public override void WriteLine(string text)
		{
			ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

				_pane.OutputStringThreadSafe(string.Concat(text, "\r\n"));
#if DEBUG
				System.Diagnostics.Debug.WriteLine(text);
#endif
			});
		}

		public void WriteLineAndTask(string lineText, string taskText, TaskType type, string fileName, uint lineNum)
		{
			ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

				VSTASKPRIORITY priority = VSTASKPRIORITY.TP_NORMAL;
				var subCategory = "";
				switch (type)
				{
					case TaskType.Error:
						priority = VSTASKPRIORITY.TP_HIGH;
						subCategory = "Error";
						break;
					case TaskType.Warning:
						priority = VSTASKPRIORITY.TP_NORMAL;
						subCategory = "Warning";
						break;
				}

				if (lineNum > 0) lineNum--;

				_pane.OutputTaskItemString(lineText + "\r\n", priority, VSTASKCATEGORY.CAT_BUILDCOMPILE, subCategory, (int)_vstaskbitmap.BMP_COMPILE, fileName, lineNum, taskText);
				_pane.FlushToTaskList();
			});
		}

		public void Clear()
		{
			ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

				_pane.Clear();
			});
		}

		public void Show()
		{
			ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

				_pane.Activate();
				Shell.DTE.ExecuteCommand("View.Output");
			});
		}

		public enum TaskType
		{
			Error,
			Warning
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace DkTools
{
	class OutputPane : Output
	{
		private IVsOutputWindowPane _pane;

		public OutputPane(IVsOutputWindowPane pane, bool activate)
		{
			if (pane == null) throw new ArgumentNullException("pane");
			_pane = pane;
			if (activate) _pane.Activate();
		}

		public override void WriteLine(string text)
		{
			_pane.OutputStringThreadSafe(string.Concat(text, "\r\n"));

#if DEBUG
			System.Diagnostics.Debug.WriteLine(text);
#endif
		}

		public void Clear()
		{
			_pane.Clear();
		}

		public void Show()
		{
			_pane.Activate();
			Shell.DTE.ExecuteCommand("View.Output");
		}
	}
}

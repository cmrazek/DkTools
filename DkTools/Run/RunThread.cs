using DK.AppEnvironment;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DkTools.Run
{
    internal class RunThread
    {
        private static CancellationTokenSource _cancel;
        private static OutputPane _pane;

        private const string _paneGuid = "9299A472-9190-412F-AA9B-61B23807DB33";

        public static void Run(IEnumerable<RunItem> runItems, DkAppSettings appSettings)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_cancel != null) _cancel.Cancel();
            _cancel = new CancellationTokenSource();

            if (_pane == null) _pane = Shell.CreateOutputPane(Guid.Parse(_paneGuid), Constants.RunOutputPaneTitle);
            _pane.Clear();
            _pane.Show();

            var task = ThreadHelper.JoinableTaskFactory.StartOnIdle(async () =>
            {
                foreach (var runItem in runItems)
                {
                    await runItem.RunAsync(appSettings, _pane, _cancel.Token);
                }
            });
        }
    }
}

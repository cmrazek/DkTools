using DK.AppEnvironment;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DkTools.Run
{
    internal class RunThread
    {
        private static Thread _thread;
        private static CancellationTokenSource _cancel;
        private static RunItem[] _runItems;
        private static OutputPane _pane;
        private static DkAppSettings _appSettings;

        private const string _paneGuid = "9299A472-9190-412F-AA9B-61B23807DB33";

        public static void Run(IEnumerable<RunItem> runItems, DkAppSettings appSettings)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            _runItems = (runItems ?? throw new ArgumentNullException(nameof(runItems))).ToArray();
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));

            if (_thread?.IsAlive == true)
            {
                _cancel?.Cancel();
                _thread.Join();
            }

            _cancel = new CancellationTokenSource();

            if (_pane == null) _pane = Shell.CreateOutputPane(Guid.Parse(_paneGuid), Constants.RunOutputPaneTitle);
            _pane.Clear();
            _pane.Show();

            _thread = new Thread(new ThreadStart(ThreadProc))
            {
                Name = "Run Thread",
                Priority = ThreadPriority.BelowNormal
            };
            _thread.Start();
        }

        private static void ThreadProc()
        {
            try
            {
                var first = true;

                foreach (var runItem in _runItems)
                {
                    if (first) first = false;
                    else _pane.WriteLine(string.Empty);

                    if (_cancel.IsCancellationRequested) break;
                    runItem.Run(_appSettings, _pane, _cancel.Token);
                }
            }
            catch (Exception ex)
            {
                _pane.WriteLine(ex.ToString());
            }
        }
    }
}

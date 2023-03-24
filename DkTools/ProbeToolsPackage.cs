using DK.AppEnvironment;
using DK.Diagnostics;
using DK.Implementation.Windows;
using DkTools.AppEnvironment;
using DkTools.BraceCompletion;
using DkTools.Compiler;
using DkTools.LanguageSvc;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace DkTools
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "This class is part of MPF.")]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [Guid(GuidList.strProbeToolsPkg)]
    [ProvideService(typeof(ProbeLanguageService), ServiceName = Constants.DkContentType)]
    [ProvideLanguageService(typeof(ProbeLanguageService), "DK", /*50433*/ 0,
        RequestStockColors = true,
        EnableLineNumbers = true,
        CodeSense = true,
        MatchBraces = false,
        MatchBracesAtCaret = false,
        ShowMatchingBrace = false,
        EnableAsyncCompletion = false,
        EnableFormatSelection = true)]
    [ProvideLanguageExtension(typeof(ProbeLanguageService), ".cc")]
    [ProvideLanguageExtension(typeof(ProbeLanguageService), ".cc&")]
    [ProvideLanguageExtension(typeof(ProbeLanguageService), ".cc+")]
    [ProvideLanguageExtension(typeof(ProbeLanguageService), ".ct")]
    [ProvideLanguageExtension(typeof(ProbeLanguageService), ".ct&")]
    [ProvideLanguageExtension(typeof(ProbeLanguageService), ".ct+")]
    [ProvideLanguageExtension(typeof(ProbeLanguageService), ".f")]
    [ProvideLanguageExtension(typeof(ProbeLanguageService), ".f&")]
    [ProvideLanguageExtension(typeof(ProbeLanguageService), ".f+")]
    [ProvideLanguageExtension(typeof(ProbeLanguageService), ".i")]
    [ProvideLanguageExtension(typeof(ProbeLanguageService), ".i&")]
    [ProvideLanguageExtension(typeof(ProbeLanguageService), ".i+")]
    [ProvideLanguageExtension(typeof(ProbeLanguageService), ".il")]
    [ProvideLanguageExtension(typeof(ProbeLanguageService), ".il&")]
    [ProvideLanguageExtension(typeof(ProbeLanguageService), ".il+")]
    [ProvideLanguageExtension(typeof(ProbeLanguageService), ".gp")]
    [ProvideLanguageExtension(typeof(ProbeLanguageService), ".gp&")]
    [ProvideLanguageExtension(typeof(ProbeLanguageService), ".gp+")]
    [ProvideLanguageExtension(typeof(ProbeLanguageService), ".nc")]
    [ProvideLanguageExtension(typeof(ProbeLanguageService), ".nc&")]
    [ProvideLanguageExtension(typeof(ProbeLanguageService), ".nc+")]
    [ProvideLanguageExtension(typeof(ProbeLanguageService), ".sc")]
    [ProvideLanguageExtension(typeof(ProbeLanguageService), ".sc&")]
    [ProvideLanguageExtension(typeof(ProbeLanguageService), ".sc+")]
    [ProvideLanguageExtension(typeof(ProbeLanguageService), ".sp")]
    [ProvideLanguageExtension(typeof(ProbeLanguageService), ".sp&")]
    [ProvideLanguageExtension(typeof(ProbeLanguageService), ".sp+")]
    [ProvideLanguageExtension(typeof(ProbeLanguageService), ".st")]
    [ProvideLanguageExtension(typeof(ProbeLanguageService), ".st&")]
    [ProvideLanguageExtension(typeof(ProbeLanguageService), ".st+")]
    [ProvideLanguageExtension(typeof(ProbeLanguageService), ".t")]
    [ProvideLanguageExtension(typeof(ProbeLanguageService), ".t&")]
    [ProvideLanguageExtension(typeof(ProbeLanguageService), ".t+")]
    [ProvideLanguageExtension(typeof(ProbeLanguageService), ".ic")]
    [ProvideLanguageExtension(typeof(ProbeLanguageService), ".id")]
    [ProvideLanguageExtension(typeof(ProbeLanguageService), ".ie")]
    [ProvideToolWindow(typeof(ProbeExplorer.ProbeExplorerToolWindow))]
    [ProvideMenuResource("Menus.ctmenu", 2)]
    [ProvideOptionPage(typeof(ProbeExplorer.ProbeExplorerOptions), "DK", "DkTools Options", 101, 106, true)]
    [ProvideOptionPage(typeof(Tagging.TaggingOptions), "DK", "Tagging", 101, 107, true)]
    [ProvideOptionPage(typeof(EditorOptions), "DK", "Editor", 101, 108, true)]
    [ProvideOptionPage(typeof(ErrorSuppressionOptions), "DK", "Error Suppressions", 101, 109, true)]
    [ProvideLanguageCodeExpansion(typeof(ProbeLanguageService), Constants.DkContentType, 0, Constants.DkContentType,
        "%LocalAppData%\\DkTools2012\\SnippetIndex.xml",
        SearchPaths = "%LocalAppData%\\DkTools2012\\Snippets\\;%MyDocs%\\Code Snippets\\DK\\My Code Snippets\\")]
    [ProvideBraceCompletion(Constants.DkContentType)]
    public sealed partial class ProbeToolsPackage : AsyncPackage, IOleComponent
    {
        private uint _componentId;
        private static ProbeToolsPackage _instance;
        private ErrorTagging.ErrorTaskProvider _errorTaskProvider;
        private DkAppContext _app;
        private WindowsFileSystem _fs;
        private WindowsLogger _log;
        private WindowsAppConfigSource _config;
        private ProbeCompiler _compiler;
        private DkFileSystemWatcher _fileSystemWatcher;

        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public ProbeToolsPackage()
        {
            _instance = this;
        }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initilaization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override async System.Threading.Tasks.Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            base.Initialize();

            _fs = new WindowsFileSystem();
            _log = new WindowsLogger(LogDir, Constants.LogFileNameFormat);
            _log.Info("DkTools {0}", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);

            _config = new WindowsAppConfigSource(_log);

            _app = new DkAppContext(_fs, _log, _config);

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            _app.LoadAppSettings(null);
            TempManager.Init(TempDir);
            Snippets.SnippetDeploy.DeploySnippets();

            _compiler = new ProbeCompiler(_app);
            _fileSystemWatcher = new DkFileSystemWatcher(_app);

            // Proffer the service.	http://msdn.microsoft.com/en-us/library/bb166498.aspx
            var langService = new ProbeLanguageService(this);
            langService.SetSite(this);

            var serviceContainer = this as IServiceContainer;
            if (serviceContainer != null) serviceContainer.AddService(typeof(ProbeLanguageService), langService, true);
            else _app.Log.Warning("Failed to get service container.");

            // Register a timer to call our language service during idle periods.
            var mgr = await GetServiceAsync(typeof(SOleComponentManager)) as IOleComponentManager;
            if (_componentId == 0 && mgr != null)
            {
                OLECRINFO[] crinfo = new OLECRINFO[1];
                crinfo[0].cbSize = (uint)Marshal.SizeOf(typeof(OLECRINFO));
                crinfo[0].grfcrf = (uint)_OLECRF.olecrfNeedIdleTime | (uint)_OLECRF.olecrfNeedPeriodicIdleTime;
                crinfo[0].grfcadvf = (uint)_OLECADVF.olecadvfModal | (uint)_OLECADVF.olecadvfRedrawOff | (uint)_OLECADVF.olecadvfWarningsOff;
                crinfo[0].uIdleTimeInterval = 1000;
                int hr = mgr.FRegisterComponent(this, crinfo, out _componentId);
            }

            _errorTaskProvider = new ErrorTagging.ErrorTaskProvider(this);
            TaskListService.RegisterTaskProvider(_errorTaskProvider, out _);

            var mcs = await GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (mcs != null) Commands.InitCommands(mcs);

            FunctionFileScanning.FFScanner.OnStartup();

            Microsoft.VisualStudio.PlatformUI.VSColorTheme.ThemeChanged += VSColorTheme_ThemeChanged;
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                FunctionFileScanning.FFScanner.OnShutdown();

                if (_componentId != 0)
                {
                    var mgr = GetService(typeof(SOleComponentManager)) as IOleComponentManager;
                    if (mgr != null) mgr.FRevokeComponent(_componentId);
                    _componentId = 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }

            _log.Close();

            base.Dispose(disposing);
        }

        public DkAppContext App => _app;
        internal ProbeCompiler Compiler => _compiler;
        internal DkFileSystemWatcher FileSystemWatcher => _fileSystemWatcher;

        public static ProbeToolsPackage Instance => _instance;
        public static ILogger Log => _instance._log;

        public int FDoIdle(uint grfidlef)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            bool bPeriodic = (grfidlef & (uint)_OLEIDLEF.oleidlefPeriodic) != 0;
            ProbeLanguageService?.OnIdle(bPeriodic);

            return 0;
        }

        public int FContinueMessageLoop(uint uReason, IntPtr pvLoopData, MSG[] pMsgPeeked)
        {
            return 1;
        }

        public int FPreTranslateMessage(MSG[] pMsg)
        {
            return 0;
        }

        public int FQueryTerminate(int fPromptUser)
        {
            return 1;
        }

        public int FReserved1(uint dwReserved, uint message, IntPtr wParam, IntPtr lParam)
        {
            return 1;
        }

        public IntPtr HwndGetWindow(uint dwWhich, uint dwReserved)
        {
            return IntPtr.Zero;
        }

        public void OnActivationChange(IOleComponent pic, int fSameComponent, OLECRINFO[] pcrinfo, int fHostIsActivating, OLECHOSTINFO[] pchostinfo, uint dwReserved)
        {
        }

        public void OnAppActivate(int fActive, uint dwOtherThreadID)
        {
        }

        public void OnEnterState(uint uStateID, int fEnter)
        {
        }

        public void OnLoseActivation()
        {
        }

        public void Terminate()
        {
        }

        /// <summary>
        /// Returns the local application data directory for this package.
        /// </summary>
        internal static string AppDataDir
        {
            get
            {
                var dir = Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Constants.AppDataDir);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                return dir;
            }
        }

        /// <summary>
        /// Returns a temporary directory for this package.
        /// </summary>
        internal static string TempDir
        {
            get
            {
                var dir = Path.Combine(AppDataDir, Constants.TempDir);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                return dir;
            }
        }

        /// <summary>
        /// Returns the directory where log files are to be written.
        /// </summary>
        internal static string LogDir
        {
            get
            {
                var dir = Path.Combine(AppDataDir, Constants.LogDir);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                return dir;
            }
        }

        private void VSColorTheme_ThemeChanged(Microsoft.VisualStudio.PlatformUI.ThemeChangedEventArgs e)
        {
            VSTheme.OnThemeChanged();
        }

        #region Settings
        private const int k_settingsRefreshTime = 5000;	// Only reload settings every 5 seconds

        private ProbeExplorer.ProbeExplorerOptions _probeExplorerOptions;
        private DateTime _probeExplorerOptionsTime;

        internal ProbeExplorer.ProbeExplorerOptions ProbeExplorerOptions
        {
            get
            {
                var time = DateTime.Now;
                if (_probeExplorerOptions == null || time.Subtract(_probeExplorerOptionsTime).TotalMilliseconds > k_settingsRefreshTime)
                {
                    _probeExplorerOptions = this.GetDialogPage(typeof(ProbeExplorer.ProbeExplorerOptions)) as ProbeExplorer.ProbeExplorerOptions;
                    _probeExplorerOptionsTime = time;
                }
                return _probeExplorerOptions;
            }
        }

        internal Tagging.TaggingOptions TaggingOptions
        {
            get { return this.GetDialogPage(typeof(Tagging.TaggingOptions)) as Tagging.TaggingOptions; }
        }

        internal EditorOptions EditorOptions
        {
            get { return GetDialogPage(typeof(EditorOptions)) as EditorOptions; }
        }

        internal ErrorSuppressionOptions ErrorSuppressionOptions
        {
            get { return GetDialogPage(typeof(ErrorSuppressionOptions)) as ErrorSuppressionOptions; }
        }
        #endregion

        #region Services
        private ProbeLanguageService _probeLanguageService;
        internal ProbeLanguageService ProbeLanguageService
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (_probeLanguageService == null)
                {
                    _probeLanguageService = GetService(typeof(ProbeLanguageService)) as ProbeLanguageService;
                }

                return _probeLanguageService;
            }
        }

        private Microsoft.VisualStudio.Editor.IVsEditorAdaptersFactoryService _editorAdaptersService;
        internal Microsoft.VisualStudio.Editor.IVsEditorAdaptersFactoryService EditorAdaptersService
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (_editorAdaptersService == null)
                {
                    var model = ProbeToolsPackage.Instance.GetService(typeof(Microsoft.VisualStudio.ComponentModelHost.SComponentModel)) as Microsoft.VisualStudio.ComponentModelHost.IComponentModel;
                    if (model == null) throw new InvalidOperationException("Unable to get service 'Microsoft.VisualStudio.ComponentModelHost.SComponentModel'.");

                    _editorAdaptersService = model.GetService<Microsoft.VisualStudio.Editor.IVsEditorAdaptersFactoryService>() as Microsoft.VisualStudio.Editor.IVsEditorAdaptersFactoryService;
                    if (_editorAdaptersService == null) throw new InvalidOperationException("Unable to get service 'Microsoft.VisualStudio.Editor.IVsEditorAdaptersFactoryService'.");
                }
                return _editorAdaptersService;
            }
        }

        private Microsoft.VisualStudio.TextManager.Interop.IVsTextManager _textManagerService;
        internal Microsoft.VisualStudio.TextManager.Interop.IVsTextManager TextManagerService
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (_textManagerService == null)
                {
                    _textManagerService = ProbeToolsPackage.Instance.GetService(typeof(Microsoft.VisualStudio.TextManager.Interop.SVsTextManager)) as Microsoft.VisualStudio.TextManager.Interop.IVsTextManager;
                    if (_textManagerService == null) throw new InvalidOperationException("Unable to get service 'Microsoft.VisualStudio.TextManager.Interop.SVsTextManager'.");
                }
                return _textManagerService;
            }
        }

        private Microsoft.VisualStudio.TextManager.Interop.IVsTextManager2 _textManager2Service;
        internal Microsoft.VisualStudio.TextManager.Interop.IVsTextManager2 TextManager2Service
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (_textManager2Service == null)
                {
                    _textManager2Service = ProbeToolsPackage.Instance.GetService(typeof(Microsoft.VisualStudio.TextManager.Interop.SVsTextManager)) as Microsoft.VisualStudio.TextManager.Interop.IVsTextManager2;
                    if (_textManager2Service == null) throw new InvalidOperationException("Unable to get service 'Microsoft.VisualStudio.TextManager.Interop.SVsTextManager'.");
                }
                return _textManager2Service;
            }
        }

        private Microsoft.VisualStudio.Shell.Interop.IVsOutputWindow _outputWindowService;
        internal Microsoft.VisualStudio.Shell.Interop.IVsOutputWindow OutputWindowService
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (_outputWindowService == null)
                {
                    _outputWindowService = this.GetService(typeof(SVsOutputWindow)) as IVsOutputWindow;
                    if (_outputWindowService == null) throw new InvalidOperationException("Unable to get service 'Microsoft.VisualStudio.Shell.Interop.IVsOutputWindow'.");
                }
                return _outputWindowService;
            }
        }

        private Microsoft.VisualStudio.Shell.Interop.IVsTaskList _taskListService;
        internal Microsoft.VisualStudio.Shell.Interop.IVsTaskList TaskListService
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (_taskListService == null)
                {
                    _taskListService = this.GetService(typeof(SVsTaskList)) as IVsTaskList;
                    if (_taskListService == null) throw new InvalidOperationException("Unable to get service 'Microsoft.VisualStudio.Shell.Interop.IVsTaskList'.");
                }
                return _taskListService;
            }
        }

        private Microsoft.VisualStudio.Text.Classification.IClassificationTypeRegistryService _classificationtypeRegistryService;
        internal Microsoft.VisualStudio.Text.Classification.IClassificationTypeRegistryService ClassificationTypeRegistryService
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();

                if (_classificationtypeRegistryService == null)
                {
                    var model = ProbeToolsPackage.Instance.GetService(typeof(Microsoft.VisualStudio.ComponentModelHost.SComponentModel)) as Microsoft.VisualStudio.ComponentModelHost.IComponentModel;
                    if (model == null) throw new InvalidOperationException("Unable to get service 'Microsoft.VisualStudio.ComponentModelHost.SComponentModel'.");

                    _classificationtypeRegistryService = model.GetService<Microsoft.VisualStudio.Text.Classification.IClassificationTypeRegistryService>() as Microsoft.VisualStudio.Text.Classification.IClassificationTypeRegistryService;
                    if (_classificationtypeRegistryService == null) throw new InvalidOperationException("Unable to get service 'Microsoft.VisualStudio.Text.Classification.IClassificationTypeRegistryService'.");
                }
                return _classificationtypeRegistryService;
            }
        }
        #endregion

        #region Status Bar
        private Microsoft.VisualStudio.Shell.Interop.IVsStatusbar _statusBarService;
        internal void SetStatusText(string text)
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await SetStatusTextAsync(text);
            });
        }

        internal async System.Threading.Tasks.Task SetStatusTextAsync(string text)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (_statusBarService == null)
            {
                _statusBarService = await this.GetServiceAsync(typeof(SVsStatusbar)) as IVsStatusbar;
                if (_statusBarService == null) throw new InvalidOperationException("Unable to get service 'Microsoft.VisualStudio.Shell.Interop.IVsStatusbar'.");
            }

            _statusBarService.SetText(text);
            _log.Info(text);
        }
        #endregion

        #region Error List
        private Microsoft.VisualStudio.Shell.Interop.IVsErrorList _errorListService;
        internal void ShowErrorList()
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

                if (_errorListService == null)
                {
                    _errorListService = await this.GetServiceAsync(typeof(SVsErrorList)) as IVsErrorList;
                    if (_statusBarService == null) throw new InvalidOperationException("Unable to get service 'Microsoft.VisualStudio.Shell.Interop.IVsErrorList'.");
                }

                _errorListService.BringToFront();
            });
        }
        #endregion
    }
}

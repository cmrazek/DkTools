﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using DkTools.LanguageSvc;

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
	[PackageRegistration(UseManagedResourcesOnly = true)]
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
	[ProvideMenuResource("Menus.ctmenu", 1)]
	[ProvideOptionPage(typeof(ProbeExplorer.ProbeExplorerOptions), "DK", "DkTools Options", 101, 106, true)]
	[ProvideOptionPage(typeof(Tagging.TaggingOptions), "DK", "Tagging", 101, 107, true)]
	[ProvideOptionPage(typeof(EditorOptions), "DK", "Editor", 101, 108, true)]
	[ProvideOptionPage(typeof(ErrorSuppressionOptions), "DK", "Error Suppressions", 101, 109, true)]
	[ProvideLanguageCodeExpansion(typeof(ProbeLanguageService), Constants.DkContentType, 0, Constants.DkContentType,
		"%LocalAppData%\\DkTools2012\\SnippetIndex.xml",
		SearchPaths = "%LocalAppData%\\DkTools2012\\Snippets\\;%MyDocs%\\Code Snippets\\DK\\My Code Snippets\\")]
	public sealed partial class ProbeToolsPackage : Package, IOleComponent
	{
		private uint _componentId;
		private static ProbeToolsPackage _instance;
		private EnvDTE.Events _dteEvents;
		private EnvDTE.DocumentEvents _dteDocumentEvents;
		private FunctionFileScanning.FFScanner _functionScanner;
		private ErrorTagging.ErrorTaskProvider _errorTaskProvider;
		private uint _errorTaskProviderCookie;

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
		protected override void Initialize()
		{
			base.Initialize();

			Log.Initialize();
			ProbeEnvironment.Initialize();
			TempManager.Init(TempDir);
			Snippets.SnippetDeploy.DeploySnippets();
			CodeModel.SignatureDocumentor.Initialize();

			ThreadHelper.ThrowIfNotOnUIThread();

			// Proffer the service.	http://msdn.microsoft.com/en-us/library/bb166498.aspx
			var langService = new ProbeLanguageService(this);
			langService.SetSite(this);
			(this as IServiceContainer).AddService(typeof(ProbeLanguageService), langService, true);

			// Register a timer to call our language service during idle periods.
			var mgr = GetService(typeof(SOleComponentManager)) as IOleComponentManager;
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
			TaskListService.RegisterTaskProvider(_errorTaskProvider, out _errorTaskProviderCookie);

			var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
			if (mcs != null) Commands.InitCommands(mcs);

			_functionScanner = new FunctionFileScanning.FFScanner();

			// Need to keep a reference to Events and DocumentEvents in order for DocumentSaved to be triggered.
			_dteEvents = Shell.DTE.Events;

			_dteDocumentEvents = _dteEvents.DocumentEvents;
			_dteDocumentEvents.DocumentSaved += DocumentEvents_DocumentSaved;

			ProbeAppSettings.FileChanged += ProbeAppSettings_FileChanged;
			ProbeAppSettings.FileDeleted += ProbeAppSettings_FileDeleted;

			Microsoft.VisualStudio.PlatformUI.VSColorTheme.ThemeChanged += VSColorTheme_ThemeChanged;
		}

		protected override void Dispose(bool disposing)
		{
			try
			{
				ThreadHelper.ThrowIfNotOnUIThread();

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
			

			if (_functionScanner != null)
			{
				_functionScanner.Dispose();
				_functionScanner = null;
			}

			Log.Close();

			base.Dispose(disposing);
		}

		public static ProbeToolsPackage Instance
		{
			get { return _instance; }
		}

		public int FDoIdle(uint grfidlef)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			bool bPeriodic = (grfidlef & (uint)_OLEIDLEF.oleidlefPeriodic) != 0;
			// Use typeof(TestLanguageService) because we need to reference the GUID for our language service.
			var service = GetService(typeof(ProbeLanguageService)) as ProbeLanguageService;
			if (service != null) service.OnIdle(bPeriodic);

			ProcessBackgroundWorkItems();

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

		internal FunctionFileScanning.FFScanner FunctionFileScanner
		{
			get { return _functionScanner; }
		}

		private void DocumentEvents_DocumentSaved(EnvDTE.Document Document)
		{
			ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

				FireRefreshDocument(Document.FullName);
			});
		}

		private void ProbeAppSettings_FileChanged(object sender, ProbeAppSettings.FileEventArgs e)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			FireRefreshDocument(e.FilePath);
		}

		private void ProbeAppSettings_FileDeleted(object sender, ProbeAppSettings.FileEventArgs e)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			FireRefreshDocument(e.FilePath);
		}

		private void VSColorTheme_ThemeChanged(Microsoft.VisualStudio.PlatformUI.ThemeChangedEventArgs e)
		{
			VSTheme.OnThemeChanged();
		}

		public event EventHandler RefreshAllDocumentsRequired;

		public void FireRefreshAllDocuments()
		{
			Log.Debug("Event: RefreshAllDocumentsRequired");
			RefreshAllDocumentsRequired?.Invoke(this, EventArgs.Empty);
		}

		public event EventHandler<RefreshDocumentEventArgs> RefreshDocumentRequired;

		public class RefreshDocumentEventArgs : EventArgs
		{
			public string FilePath { get; private set; }

			public RefreshDocumentEventArgs(string filePath)
			{
				FilePath = filePath;
			}
		}

		public void FireRefreshDocument(string filePath)
		{
			Log.Debug("Event: RefreshDocumentRequired: {0}", filePath);
			RefreshDocumentRequired?.Invoke(this, new RefreshDocumentEventArgs(filePath));
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

		internal Run.RunOptions RunOptions
		{
			get { return this.GetDialogPage(typeof(Run.RunOptions)) as Run.RunOptions; }
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

		#region Background Work Items
		private Queue<BackgroundWorkItem> _backgroundWorkItems = new Queue<BackgroundWorkItem>();

		private class BackgroundWorkItem
		{
			public Action<Exception> finishedCallback;
			public Exception exception;
		}

		internal void StartBackgroundWorkItem(Action workCallback, Action<Exception> finishedCallback = null)
		{
			System.Threading.ThreadPool.QueueUserWorkItem(x =>
				{
					try
					{
						workCallback();
						if (finishedCallback != null)
						{
							lock (_backgroundWorkItems)
							{
								_backgroundWorkItems.Enqueue(new BackgroundWorkItem
								{
									finishedCallback = finishedCallback,
									exception = null
								});
							}
						}
					}
					catch (Exception ex)
					{
						if (finishedCallback != null)
						{
							lock (_backgroundWorkItems)
							{
								_backgroundWorkItems.Enqueue(new BackgroundWorkItem
								{
									finishedCallback = finishedCallback,
									exception = ex
								});
							}
						}
					}
				});
		}

		private void ProcessBackgroundWorkItems()
		{
			BackgroundWorkItem workItem = null;
			do
			{
				workItem = null;
				lock (_backgroundWorkItems)
				{
					if (_backgroundWorkItems.Count > 0)
					{
						workItem = _backgroundWorkItems.Dequeue();
					}
				}

				if (workItem != null)
				{
					try
					{
						workItem.finishedCallback(workItem.exception);
					}
					catch (Exception ex)
					{
						Log.Debug("Exception when processing finished callback for background work item: {0}", ex);
					}
				}
			}
			while (workItem != null);
		}
		#endregion

		#region Services
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
		#endregion

		#region Status Bar
		private Microsoft.VisualStudio.Shell.Interop.IVsStatusbar _statusBarService;
		internal void SetStatusText(string text)
		{
			ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

				if (_statusBarService == null)
				{
					_statusBarService = this.GetService(typeof(SVsStatusbar)) as IVsStatusbar;
					if (_statusBarService == null) throw new InvalidOperationException("Unable to get service 'Microsoft.VisualStudio.Shell.Interop.IVsStatusbar'.");
				}

				_statusBarService.SetText(text);
			});
		}
		#endregion

		#region Error List
		private Microsoft.VisualStudio.Shell.Interop.IVsErrorList _errorListService;
		internal void ShowErrorList()
		{
			ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

				if (_errorListService == null)
				{
					_errorListService = this.GetService(typeof(SVsErrorList)) as IVsErrorList;
					if (_statusBarService == null) throw new InvalidOperationException("Unable to get service 'Microsoft.VisualStudio.Shell.Interop.IVsErrorList'.");
				}

				_errorListService.BringToFront();
			});
		}
		#endregion

		#region Image Catalogue
		//IVsImageService2 _imageService;
		//internal Microsoft.VisualStudio.Text.Adornments.ImageElement GetImage(ImageMoniker moniker)
		//{
		//	ThreadHelper.ThrowIfNotOnUIThread();

		//	if (_imageService == null)
		//	{
		//		_imageService = this.GetService(typeof(SVsImageService)) as IVsImageService2;
		//		if (_imageService == null) throw new InvalidOperationException("Unable to get service 'Microsoft.VisualStudio.Shell.Interop.IVsImageService2'.");
		//	}

		//	var attribs = new ImageAttributes
		//	{
		//		StructSize = Marshal.SizeOf(typeof(ImageAttributes)),
		//		Flags = 0
		//	};

		//	var img = _imageService.GetImage(moniker, attribs);
		//	return new Microsoft.VisualStudio.Text.Adornments.ImageElement(

		//}
		#endregion
	}
}

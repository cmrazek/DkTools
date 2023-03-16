using DK.Diagnostics;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace DkTools.ProbeExplorer
{
	[Guid(GuidList.strProbeExplorerOptions)]
	class ProbeExplorerOptions : DialogPage
	{
		[Category("DK Explorer Options")]
		[DisplayName("Show Files in Tree")]
		[Description("Show all the files when expanding folders in the tree-view. The tree list can get slow if there are many files to display. If turned off, files are still accessible through the filter list.")]
		public bool ShowFilesInTree { get; set; }

		[Category("DK Explorer Options")]
		[DisplayName("Hidden Extensions")]
		[Description("List of extensions to be not displayed in DK Explorer, separated by spaces. If blank, default extensions hidden will be (" + Constants.DefaultHiddenExtensions + ")")]
		public string HiddenExtensions { get; set; }

		[Category("DK Explorer Options")]
		[DisplayName("Auto-Scroll to Functions")]
		[Description("When functions are selected, automatically scroll to them in the document.")]
		public bool AutoScrollToFunctions { get; set; }

		[Category("Compile")]
		[DisplayName("Show Error List After Compile")]
		[Description("When a compile is complete, if there are errors or warnings, then display Error List tool window.")]
		public bool ShowErrorListAfterCompile { get; set; }

		[Category("Compile")]
		[DisplayName("Run DCCMP after a successful compile.")]
		[Description("When a compile completes successfully, the DCCMP process will be automatically run. You may want to disable this if need to run dccmp in manual mode.")]
		public bool RunDccmpAfterCompile { get; set; }

		[Category("Compile")]
		[DisplayName("Compile Arguments")]
		[Description("Arguments passed to 'pc' when compiling")]
		public string CompileArguments { get; set; }

		[Category("Compile")]
		[DisplayName("DCCMP Arguments")]
		[Description("Arguments passed to 'dccmp'\r\n(/P automatically appended to specify current app)")]
		public string DccmpArguments { get; set; }

		[Category("Compile")]
		[DisplayName("CREDELIX Arguments")]
		[Description("Arguments passed to 'credelix'\r\n(/P automatically appended to specify current app)")]
		public string CredelixArguments { get; set; }

		[Category("Compile")]
		[DisplayName("FEC Arguments")]
		[Description("Arguments passed to 'fec' when getting errors/warnings.")]
		public string FecArguments { get; set; }

		[Category("Extension")]
		[DisplayName("Logging Level")]
		[Description("Log detail level.")]
		public LogLevel LogLevel { get; set; }

		public ProbeExplorerOptions()
		{
			ShowFilesInTree = false;
			ShowErrorListAfterCompile = false;
			RunDccmpAfterCompile = true;
			CompileArguments = "/w";
			DccmpArguments = "/z /D";
			CredelixArguments = "/p";
			FecArguments = "";
			AutoScrollToFunctions = true;
#if DEBUG
			LogLevel = LogLevel.Debug;
#else
			LogLevel = LogLevel.Info;
#endif
		}

		public override void SaveSettingsToStorage()
		{
			base.SaveSettingsToStorage();

			ProbeToolsPackage.Instance.App.Log.Level = LogLevel;
		}
	}
}

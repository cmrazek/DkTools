using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace DkTools
{
	[Guid(GuidList.strEditorOptions)]
	class EditorOptions : DialogPage
	{
		[Category("Editor Options")]
		[DisplayName("Disable Dead Code")]
		[Description("Code that is excluded due to preprocessor commands will be colored gray.")]
		public bool DisableDeadCode { get; set; }

		[Category("Editor Options")]
		[DisplayName("Highlight Report Output")]
		[Description("Expressions that write to the report output stream will be highlighted. (Requires Code Analysis)")]
		public bool HighlightReportOutput { get; set; }

		[Category("Editor Options")]
		[DisplayName("Show FEC Errors")]
		[Description("When a file is saved, run FEC in the background and show detected errors and warnings.")]
		public bool RunBackgroundFecOnSave { get; set; }

		[Category("Editor Options")]
		[DisplayName("Disable Background Scanning")]
		[Description("Stop scanning files in the background (will cause classes, functions to not be detected properly).")]
		public bool DisableBackgroundScan { get; set; }

		[Category("Auto-Completion")]
		[DisplayName("Auto-Complete on Space")]
		[Description("Typing a space will accept the currently selected auto-completion item.")]
		public bool AutoCompleteOnSpace { get; set; }

		[Category("Auto-Completion")]
		[DisplayName("Auto-Complete on Tab")]
		[Description("Typing a tab will accept the currently selected auto-completion item.")]
		public bool AutoCompleteOnTab { get; set; }

		[Category("Auto-Completion")]
		[DisplayName("Auto-Complete on Dot")]
		[Description("Typing a dot '.' will accept the currently selected auto-completion item.")]
		public bool AutoCompleteOnDot { get; set; }

		[Category("Code Analysis")]
		[DisplayName("Show Code Analysis on Save")]
		[Description("When a file is saved, run code analysis and show detected errors and warnings.")]
		public bool RunCodeAnalysisOnSave { get; set; }

		[Category("Code Analysis")]
		[DisplayName("Show Code Analysis on User Input")]
		[Description("Run code analysis as code is being modified.")]
		public bool RunCodeAnalysisOnUserInput { get; set; }

		[Category("Code Analysis")]
		[DisplayName("Max Warnings")]
		[Description("Maximum number of warnings to display per file. Set to zero to get ALL warnings.")]
		public int MaxWarnings { get; set; }

		public EditorOptions()
		{
			DisableDeadCode = true;
			HighlightReportOutput = true;
			RunBackgroundFecOnSave = true;
			AutoCompleteOnSpace = false;
			AutoCompleteOnTab = true;
			AutoCompleteOnDot = true;

			RunCodeAnalysisOnSave = true;
			RunCodeAnalysisOnUserInput = true;
			MaxWarnings = 100;
		}

		public override void SaveSettingsToStorage()
		{
			base.SaveSettingsToStorage();

            ProbeToolsPackage.Instance.App.OnRefreshAllDocumentsRequired();
		}
	}
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

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
		[DisplayName("Show FEC Errors")]
		[Description("When a file is saved, run FEC in the background and show detected errors and warnings.")]
		public bool RunBackgroundFecOnSave { get; set; }

		[Category("Editor Options")]
		[DisplayName("Show Code Analysis Automatically")]
		[Description("When a file is saved, run code analysis and show detected errors and warnings.")]
		public bool RunCodeAnalysisOnSave { get; set; }

		[Category("Editor Options")]
		[DisplayName("Disable Background Scanning")]
		[Description("Stop scanning files in the background (will cause classes, functions to not be detected properly).")]
		public bool DisableBackgroundScan { get; set; }

		public EditorOptions()
		{
			DisableDeadCode = true;
			RunBackgroundFecOnSave = true;
			RunCodeAnalysisOnSave = true;
		}

		public override void SaveSettingsToStorage()
		{
			base.SaveSettingsToStorage();

			ProbeToolsPackage.Instance.FireRefreshAllDocuments();
		}
	}
}

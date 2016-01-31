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
		[DisplayName("Show Errors")]
		[Description("Show detected errors and warnings in the source code.")]
		public bool ShowErrors { get; set; }

		[Category("Editor Options")]
		[DisplayName("Background Scanning")]
		[Description("Scan files in the background to provide more info.")]
		public bool BackgroundScan { get; set; }

		public event EventHandler EditorRefreshRequired;

		public EditorOptions()
		{
			DisableDeadCode = true;

#if REPORT_ERRORS
			ShowErrors = false;
#endif
		}

		public override void SaveSettingsToStorage()
		{
			base.SaveSettingsToStorage();

			FireEditorRefresh();
		}

		public void FireEditorRefresh()
		{
			var ev = EditorRefreshRequired;
			if (ev != null) ev(this, EventArgs.Empty);
		}
	}
}

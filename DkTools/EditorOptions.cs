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
		[DisplayName("Disable Dead Dode")]
		[Description("Code that is excluded due to preprocessor commands will be colored gray.")]
		public bool DisableDeadCode { get; set; }

		public event EventHandler ClassifierRefreshRequired;

		public EditorOptions()
		{
			DisableDeadCode = true;
		}

		public override void SaveSettingsToStorage()
		{
			base.SaveSettingsToStorage();

			var ev = ClassifierRefreshRequired;
			if (ev != null) ev(this, EventArgs.Empty);
		}
	}
}

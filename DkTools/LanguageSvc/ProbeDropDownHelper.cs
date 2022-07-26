using DkTools.CodeModeling;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Collections;
using System.Linq;

namespace DkTools.LanguageSvc
{
	internal class ProbeDropDownHelper : TypeAndMemberDropdownBars
	{
		private const int k_methodImageIndex = 72;
		public ProbeDropDownHelper(LanguageService ls)
			: base(ls)
		{
		}

		public override bool OnSynchronizeDropdowns(LanguageService languageService, IVsTextView textView, int line, int col,
			ArrayList dropDownTypes, ArrayList dropDownMembers, ref int selectedType, ref int selectedMember)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (languageService.GetType() != typeof(ProbeLanguageService)) return false;

			if (textView == null) return false;
			var wpfTextView = Shell.VsTextViewToWpfTextView(textView);
			if (wpfTextView == null) return false;
			var buf = wpfTextView.TextBuffer;

			dropDownMembers.Clear();
			selectedMember = -1;

			int caretPos, virtualSpaces;
			textView.GetNearestPosition(line, col, out caretPos, out virtualSpaces);
			var fileStore = FileStoreHelper.GetOrCreateForTextBuffer(buf);
			if (fileStore == null) return false;

			var model = fileStore.Model;
			if (model != null)
			{
				var index = 0;
				foreach (var func in fileStore.GetFunctionDropDownList(model).OrderBy(f => f.Name.ToLower()))
				{
					var span = func.EntireFunctionSpan;
					dropDownMembers.Add(new DropDownMember(func.Name, span.ToVsTextInteropSpan(textView), k_methodImageIndex, DROPDOWNFONTATTR.FONTATTR_PLAIN));
					if (span.Contains(caretPos)) selectedMember = index;
					index++;
				}
			}

			return true;
		}
	}
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;

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
			if (languageService.GetType() != typeof(ProbeLanguageService)) return false;

			if (textView == null) return false;
			var wpfTextView = Shell.VsTextViewToWpfTextView(textView);
			if (wpfTextView == null) return false;
			var buf = wpfTextView.TextBuffer;
			var model = CodeModel.FileStore.GetOrCreateForTextBuffer(buf).GetOrCreateModelForSnapshot(buf.CurrentSnapshot);
			if (model == null) return false;

			var caretPos = model.GetPosition(line, col);

			dropDownMembers.Clear();

			var index = 0;
			foreach (var func in model.LocalFunctions.OrderBy(f => f.Name.ToLower()))
			{
				dropDownMembers.Add(new DropDownMember(func.Name, func.Span.ToVsTextInteropSpan(), k_methodImageIndex, DROPDOWNFONTATTR.FONTATTR_PLAIN));
				if (func.Span.Contains(caretPos)) selectedMember = index;
				index++;
			}

			return true;
		}
	}
}

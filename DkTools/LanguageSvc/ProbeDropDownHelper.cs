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

			dropDownMembers.Clear();
			selectedMember = -1;

			var fileStore = CodeModel.FileStore.GetOrCreateForTextBuffer(buf);
			var model = fileStore.GetMostRecentModel(buf.CurrentSnapshot, "Function dropdown list");
			var caretPos = model.GetPosition(line, col);
			var index = 0;

			foreach (var func in fileStore.GetFunctionDropDownList(buf.CurrentSnapshot))
			{
				var span = func.EntireFunctionSpan;
				dropDownMembers.Add(new DropDownMember(func.Name, span.ToVsTextInteropSpan(), k_methodImageIndex, DROPDOWNFONTATTR.FONTATTR_PLAIN));
				if (span.Contains(caretPos)) selectedMember = index;
				index++;
			}

			// TODO: remove
			//var model = CodeModel.FileStore.GetOrCreateForTextBuffer(buf).GetMostRecentModel(buf.CurrentSnapshot, "Function dropdown list");
			//if (model == null) return false;

			//var caretPos = model.GetPosition(line, col);

			//dropDownMembers.Clear();

			//var index = 0;
			//foreach (var func in model.LocalFunctions.OrderBy(f => f.Name.ToLower()))
			//{
			//	dropDownMembers.Add(new DropDownMember(func.Name, func.Span.ToVsTextInteropSpan(), k_methodImageIndex, DROPDOWNFONTATTR.FONTATTR_PLAIN));
			//	if (func.Span.Contains(caretPos)) selectedMember = index;
			//	index++;
			//}

			return true;
		}
	}
}

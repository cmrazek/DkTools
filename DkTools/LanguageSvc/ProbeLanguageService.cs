using DK.Diagnostics;
using Microsoft.VisualStudio.Package;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Runtime.InteropServices;

namespace DkTools.LanguageSvc
{
	[Guid(GuidList.strLanguageService)]
	internal class ProbeLanguageService : LanguageService
	{
		private LanguagePreferences _prefs;

		public ProbeLanguageService(ProbeToolsPackage package)
		{
		}

		public override LanguagePreferences GetLanguagePreferences()
		{
			if (_prefs == null)
			{
				_prefs = new LanguagePreferences(this.Site, typeof(ProbeLanguageService).GUID, this.Name);
				_prefs.EnableMatchBraces = true;
				_prefs.EnableMatchBracesAtCaret = true;
				_prefs.EnableShowMatchingBrace = false;
				_prefs.HighlightMatchingBraceFlags = _HighlightMatchingBraceFlags.HMB_SUPPRESS_STATUS_BAR_UPDATE;
				_prefs.EnableCommenting = true;
				_prefs.EnableFormatSelection = true;
				//_prefs.AutoOutlining = true;
				//_prefs.ShowNavigationBar = true;
			}
			return _prefs;
		}

		public override void OnIdle(bool periodic)
		{
			try
			{
				var src = GetSource(this.LastActiveTextView);
				if (src != null && src.LastParseTime == Int32.MaxValue)
				{
					src.LastParseTime = 0;
				}
			}
			catch (Exception ex)
			{
				ProbeToolsPackage.Log.Error(ex);
			}

			base.OnIdle(periodic);
		}

		public override string Name
		{
			get { return "DK"; }
		}

		public override string GetFormatFilterList()
		{
			return "All Files\n*.*\n";
		}

		public override IScanner GetScanner(IVsTextLines buffer)
		{
			return new ProbeScanner(buffer);
		}

		public override AuthoringScope ParseSource(ParseRequest req)
		{
			return new ProbeAuthoringScope(req);
		}

		public override TypeAndMemberDropdownBars CreateDropDownHelper(IVsTextView forView)
		{
			return new ProbeDropDownHelper(this);
		}
	}
}

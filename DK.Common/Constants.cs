using System;
using System.Collections.Generic;

namespace DK
{
	public static class Constants
	{
		public const string WbdkRegKey = "Software\\Fincentric\\WBDK";
		public static readonly DateTime ZeroDate = new DateTime(1900, 1, 1);

		public static readonly HashSet<string> ProbeExtensions = StringHelper.ParseWordList("cc cc& cc+ ct ct& ct+ f f& f+ i i& i+ gp gp& gp+ nc nc& nc+ sc sc& sc+ sp sp& sp+ st st& st+ t t& t+");
		public static readonly HashSet<string> IncludeExtensions = StringHelper.ParseWordList("i i& i+ id id& id+ ie ie& ie+ il il& il+");

		// Outlining
		public const int OutliningMaxContextChars = 500;
		public const string DefaultOutliningText = "...";

		// Keywords
		public static readonly HashSet<string> DataTypeKeywords = StringHelper.ParseWordList(
			"alternate Boolean_t char character currency date enum int LEADINGZEROS like local_currency long longform",
			"nowarn numeric oleobject proto short shortform signed string time unsigned varchar variant void");

		public static readonly HashSet<string> GlobalKeywords = StringHelper.ParseWordList(
			"after all alter and BEGINHLP before break button case center comment col colff cols continue create display default description",
			"each else endgroup ENDHLP extern extract",
			"filterby footer for form formonly format group header if in index interface",
			"many nomenu nopick nopersist of on one onerror or order outfile permanent physical private prompt protected public",
			"relationship return row rows select snapshot static switch",
			"table tag to tool typedef unique updates where while widthof zoom");

		// Keywords that are not supported by the code model, but should be highlighted anyway.
		public static readonly HashSet<string> HighlightKeywords = StringHelper.ParseWordList("all group in where");

		public static readonly HashSet<string> TagNames = StringHelper.ParseWordList(
			"accesstype checkbox cols controlstyle defaultenumcontrolstyle disabledborder easyview formatstring formposition hideModalMenus",
			"probeform:col probeform:expressentry probeform:nobuttonbar probeform:row probeform:SelectedHighLight probeform:ShowChildForm",
			"probeform:tabkeycapture probeformgroup:folder probeformgroup:folderorder probeformgroup:homeform probeformgroup:LogicalCascadeClearParent",
			"probeformgroup:nextform probeformgroup:stayloaded probeformgroup:tooln probegroupmenu:alltables rows scrollbars wordwrap");

		public static readonly HashSet<string> PreprocessorDirectives = StringHelper.ParseWordList(
			"#define #elif #else #endif #if #ifdef #ifndef #include #insert #label #replace #undef #warnadd #warndel");

		public static readonly HashSet<string> ReportOutputKeywords = StringHelper.ParseWordList(
			"center col colff page row");
	}
}

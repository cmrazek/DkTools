using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools
{
	class Constants
	{
		public const string AppDataDir = "DkTools";
		public const string TempDir = "Temp";
		public const string WbdkRegKey = "Software\\Fincentric\\WBDK";
		public const string CompileOutputPaneTitle = "DK Compile";
		public const string FindReferencesOutputPaneTitle = "DK References";
		public const string ErrorCaption = "Error";
		public const string DkContentType = "DK";
		public const string TextContentType = "text";
        public const string ThemeRegKey = @"Software\Microsoft\VisualStudio\15.0";

        /// <summary>
        /// Directory name where log files will be stored (under AppDataDir)
        /// </summary>
        public const string LogDir = "Logs";

		/// <summary>
		/// Log file naming format. {0} is the date the log file is created.
		/// </summary>
		public const string LogFileNameFormat = "DkTools_{0:yyyyMMdd_HHmmss}.log";

		/// <summary>
		/// Number of days log files will be kept before they are purged.
		/// </summary>
		public const int LogFilePurgeDays = 7;

		public const int FileListMaxItems = 50;
		public const string FileListMaxItemsExceeded = "(more than {0} matches found)";

		public static readonly HashSet<string> ProbeExtensions = Util.ParseWordList("cc cc& cc+ ct ct& ct+ f f& f+ i i& i+ gp gp& gp+ nc nc& nc+ sc sc& sc+ sp sp& sp+ st st& st+ t t& t+");
		public static readonly HashSet<string> IncludeExtensions = Util.ParseWordList("i i& i+ id id& id+ ie ie& ie+ il il& il+");
		public const string DefaultHiddenExtensions = ".c .dll .exe .h .hlp .lib .obj .pdb .res .ss";

		public static readonly HashSet<string> DataTypeKeywords = Util.ParseWordList(
			"alternate Boolean_t char character currency date enum int LEADINGZEROS like local_currency long longform",
			"nowarn numeric oleobject proto short shortform signed string time unsigned varchar variant void");

		public static readonly HashSet<string> GlobalKeywords = Util.ParseWordList(
			"after all alter and BEGINHLP before break button case center comment col colff cols continue create display default description",
			"each else endgroup ENDHLP extern extract",
			"footer for form formonly format group header if in index interface",
			"many nomenu nopick nopersist of on one onerror or order outfile permanent physical private prompt protected public",
			"relationship return row rows select snapshot static switch",
			"table tag to tool typedef unique updates where while widthof zoom");

		// Keywords that are not supported by the code model, but should be highlighted anyway.
		public static readonly HashSet<string> HighlightKeywords = Util.ParseWordList("all group in where");

		public static readonly HashSet<string> TagNames = Util.ParseWordList(
			"accesstype checkbox cols controlstyle defaultenumcontrolstyle disabledborder easyview formatstring formposition hideModalMenus",
			"probeform:col probeform:expressentry probeform:nobuttonbar probeform:row probeform:SelectedHighLight probeform:ShowChildForm",
			"probeform:tabkeycapture probeformgroup:folder probeformgroup:folderorder probeformgroup:homeform probeformgroup:LogicalCascadeClearParent",
			"probeformgroup:nextform probeformgroup:stayloaded probeformgroup:tooln probegroupmenu:alltables rows scrollbars wordwrap");

		public static readonly HashSet<string> PreprocessorDirectives = Util.ParseWordList(
			"#define #elif #else #endif #if #ifdef #ifndef #include #insert #label #replace #undef #warnadd #warndel");

		public static readonly HashSet<string> ReportOutputKeywords = Util.ParseWordList(
			"center col colff page row");

		public static readonly char[] OperatorChars = "(){}|+-*/%?:<>=!&".ToCharArray();

		public const string DefaultDateFormat = "ddMMMyyyy";

		public const string DefaultOutliningText = "...";
		public const int OutliningMaxContextChars = 500;

		public const int WordSelectDelay = 600;	// milliseconds
		public const int CodeAnalysisDelay = 3000; // milliseconds
		public const int IncludeFileCheckFrequency = 30;	// seconds

		public const int MaxTableNameLength = 8;

		public const int KeyTimeout = 1000;	// milliseconds

		public static readonly DateTime ZeroDate = new DateTime(1900, 1, 1);

		public const int MaxIncludeRecursion = 8;	// The maximum number of levels deep #include files can nest.

		public const int BackgroundFecDelay = 750;
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DkTools
{
	class Constants
	{
		public const string AppDataDir = "DkTools2012";
		public const string EventLogSource = "DkTools2012";
		public const string TempDir = "Temp";
		public const string SettingsRegKey = "Software\\DkTools2012";
		public const string WbdkRegKey = "Software\\Fincentric\\WBDK";
		public const string CompileOutputPaneTitle = "DK Compile";
		public const string ErrorCaption = "Error";
		public const string DkContentType = "DK";

		/// <summary>
		/// Directory name where log files will be stored (under AppDataDir)
		/// </summary>
		public const string LogDir = "Logs";

		/// <summary>
		/// Log file naming format. {0} is the date the log file is created.
		/// </summary>
		public const string LogFileNameFormat = "DkTools2012_{0:yyyyMMdd_HHmmss}.log";

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
			"alternate Boolean_t char currency date enum int LEADINGZEROS like local_currency long longform",
			"nowarn numeric oleobject proto short shortform string time unsigned variant void");

		public static readonly HashSet<string> Keywords = Util.ParseWordList(
			"after all and asc BEGINHLP before break button by comment case col colff cols continue create default display desc description",
			"each else endgroup ENDHLP extern extract for form formonly format from group header if index interface interfacetype many nomenu nopick of on one or order outfile",
			"permanent physical private progid prompt protected public",
			"relationship return row rows select snapshot static switch table tag to tool typedef unique updates where while widthof zoom");

		public static readonly HashSet<string> GlobalKeywords = Util.ParseWordList(
			"after all and BEGINHLP before break button comment col colff cols continue create display description each else endgroup ENDHLP extern extract",
			"for form formonly format from group header index interface interfacetype",
			"many nomenu nopick on one or outfile permanent physical private progid prompt protected public relationship row rows select snapshot static",
			"table tag to tool typedef unique updates where widthof zoom");

		public static readonly HashSet<string> SwitchKeywords = Util.ParseWordList("case default");
		public static readonly HashSet<string> FunctionKeywords = Util.ParseWordList("if return switch while");
		public static readonly HashSet<string> SelectFromKeywords = Util.ParseWordList("of");
		public static readonly HashSet<string> SelectOrderByKeywords = Util.ParseWordList("order by asc desc");
		public static readonly HashSet<string> SelectBodyKeywords = Util.ParseWordList("after all before default each for group");

		public static readonly HashSet<string> TagNames = Util.ParseWordList(
			"accesstype checkbox cols controlstyle defaultenumcontrolstyle disabledborder formatstring formposition hideModalMenus",
			"probeform:col probeform:expressentry probeform:nobuttonbar probeform:row probeform:SelectedHighLight probeform:ShowChildForm",
			"probeform:tabkeycapture probeformgroup:folder probeformgroup:folderorder probeformgroup:homeform probeformgroup:LogicalCascadeClearParent",
			"probeformgroup:nextform probeformgroup:stayloaded probeformgroup:tooln probegroupmenu:alltables rows scrollbars wordwrap");

		public static readonly HashSet<string> AlterKeywords = Util.ParseWordList(
			"add ADD after AFTER alter ALTER AppIID application APPLICATION before BEFORE column COLUMN drop DROP extends EXTENDS move MOVE sametype SAMETYPE stringdef STRINGDEF table TABLE typedef TYPEDEF workspace WORKSPACE");

		public static readonly char[] OperatorChars = "(){}|+-*/%?:<>=!&".ToCharArray();

		public const string DefaultDateFormat = "ddMMMyyyy";

		public const string DefaultOutliningText = "...";
		public const int OutliningMaxContextChars = 500;

		public const int WordSelectDelay = 600;	// milliseconds
		public const int IncludeFileCheckFrequency = 30;	// seconds

		public const int MaxTableNameLength = 8;

		public const int KeyTimeout = 1000;	// milliseconds

		public static readonly DateTime ZeroDate = new DateTime(1900, 1, 1);
	}
}

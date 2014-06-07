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
		public const string CompileOutputPaneTitle = "DK Compile";
		public const string ErrorCaption = "Error";
		public const string FunctionFileDatabaseFileName = "FunctionFiles.xml";

		public const int FileListMaxItems = 50;
		public const string FileListMaxItemsExceeded = "(more than {0} matches found)";

		public static readonly HashSet<string> ProbeExtensions = Util.ParseWordList("cc ct ct& f f& i i& gp gp& nc sc sp sp& st st& t t&");

		public static readonly HashSet<string> DataTypeKeywords = Util.ParseWordList("alternate bool char currency date enum int LEADINGZEROS like local_currency long longform nowarn numeric proto shortform string time unsigned void");

		public static readonly HashSet<string> Keywords = Util.ParseWordList("after all and asc before break button by comment case col colff cols continue create default display desc each else extern extract for form format from group header if index many nopick of on one or order outfile permanent prompt relationship return row rows select snapshot static switch table to unique updates where while widthof zoom");
		public static readonly HashSet<string> GlobalKeywords = Util.ParseWordList("after all and before break button comment col colff cols continue create display each else extern extract for form format from group header index many nopick on one or outfile permanent prompt relationship row rows select snapshot static table to unique updates where widthof zoom");
		public static readonly HashSet<string> SwitchKeywords = Util.ParseWordList("case default");
		public static readonly HashSet<string> FunctionKeywords = Util.ParseWordList("if return switch while");
		public static readonly HashSet<string> SelectFromKeywords = Util.ParseWordList("of");
		public static readonly HashSet<string> SelectOrderByKeywords = Util.ParseWordList("order by asc desc");
		public static readonly HashSet<string> SelectBodyKeywords = Util.ParseWordList("after all before default each for group");

		public static readonly char[] OperatorChars = "(){}|+-*/%?:<>=!&".ToCharArray();

		public const string DefaultDateFormat = "ddMMMyyyy";

		public const string DefaultOutliningText = "...";
		public const int OutliningMaxContextChars = 500;

		public const int WordSelectDelay = 500;	// milliseconds
	}
}

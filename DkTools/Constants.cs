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
		public const string CompileOutputPaneTitle = "DK Compile";
		public const string FindReferencesOutputPaneTitle = "DK References";
		public const string RunOutputPaneTitle = "DK Run";
		public const string ErrorCaption = "Error";
		public const string DkContentType = "DK";
		public const string TextContentType = "text";
        public const string ThemeRegKey = @"Software\Microsoft\VisualStudio\15.0";
		public const string RunJsonFileName = "Run.json";

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

		public const string DefaultHiddenExtensions = ".c .dll .exe .h .hlp .lib .obj .pdb .res .ss";

		public static readonly char[] OperatorChars = "(){}|+-*/%?:<>=!&".ToCharArray();

		public const string DefaultDateFormat = "ddMMMyyyy";

		public const int WordSelectDelay = 600;	// milliseconds
		public const int CodeAnalysisDelay = 3000; // milliseconds
		public const int IncludeFileCheckFrequency = 30;	// seconds

		public const int MaxTableNameLength = 8;

		public const int KeyTimeout = 1000;	// milliseconds

		public const int MaxIncludeRecursion = 8;	// The maximum number of levels deep #include files can nest.

		public const int BackgroundFecDelay = 750;
	}
}

using System.Collections.Generic;
using System.IO;

namespace DK.Modeling
{
	static class SignatureDocumentor
	{
		private static bool _initialized;
		
		private static void Initialize()
		{
			// The WBDK header files do not contain any descriptions or argument file names, which makes them difficult to use in some cases.
			// The documentation below will be merged into those functions.

			AddFunc("stdlib.i", "action", "Executes an action trigger.", "TableName", "ActionName");
			AddFunc("stdlib.i", "asof", "Automatically calculates a master row in a time relationship as of the effective date and TimeOfDay, from the point of view of someone making the calculation on the entered date. The values from the calculated master row overwrite the current row buffer for the master.", "RelationshipName", "effective", "TimeOfDay", "entered");
			AddFunc("stdlib.i", "backsnap", "Used with time relationships, backsnap inserts a snapshot of the master row for a date in the past.", "RelationshipName", "effective", "TimeOfDay", "backdate");
			AddFunc("stdlib.i", "beginwork", "Starts a unit of work (transaction) such that all inherited writes to the database are either committed or rolled back.");
			AddFunc("stdlib.i", "clrrownoreq", "Clears the client stack of rowno numbers received via the qrownoreq function.");
			AddFunc("stdlib.i", "charchng", "Replaces each occurrence of a character in a string with another character.", "string", "search", "replace");
			AddFunc("stdlib.i", "clearbuf", "Clears the current row buffer, not including extended zoom columns.", "TableName");
			AddFunc("stdlib.i", "ClearPROBELocale", "Undoes the effect of SetPROBELocale.");
			AddFunc("stdlib.i", "closeall", "Closes all forms except: the current form, modal forms and their parents. The term \"parent\" refers to a form that spawned a modal form. It does not have any relational database implications.");
			AddFunc("stdlib.i", "closeform", "Closes the active form.", "ReturnValue");
			AddFunc("stdlib.i", "commit", "Commits a unit of work initiated with the beginwork function.", "work");
			AddFunc("stdlib.i", "compareoldbuf", "Compares the client current row buffer to the client old buffer. Columns that are formonly are included in the comparison at the client.", "TableName");
			AddFunc("stdlib.i", "day", "Extracts the day of the month from a date.", "date");
			AddFunc("stdlib.i", "dbdate", "Returns the date of the database, as stored in memory by SAM. If SAM does not have the date, it queries the database.");
			AddFunc("stdlib.i", "dbdateRefresh", "Instructs SAM to refresh its database date by querying the DBMS, and returns the refreshed date to the function.");
			AddFunc("stdlib.i", "dbtoset", "Instructs WBDK to abort a transaction or program, if during a loop (while, for, for each) the elapsed time of the transaction or program exceeds the function's argument.", "seconds");
			AddFunc("stdlib.i", "delete", "Deletes a row from a table.", "TableName");
			AddFunc("stdlib.i", "dispflds", "Refreshes the CAM form.");
			AddFunc("stdlib.i", "EarliestEffective", "Returns an effective date according to the algorithm below. This date is useful in reports for determining a safe and efficient starting point for selects on account activity during a particular period.\r\nIn general, the returned date for the current master is the earliest effective date (earliest within one snapshot interval) of transactions entered or reversed on or after the entered argument. See the algorithm below for exceptions.", "RelationshipName", "entered");
			AddFunc("stdlib.i", "encpwd", "Encrypts a password.", "password");
			AddFunc("stdlib.i", "endmarker", "Marks the end of a block of lines in a pickable listing.");
			AddFunc("stdlib.i", "fieldcomment", "Returns the comment for the CAM field with the focus.");
			AddFunc("stdlib.i", "fieldname", "Returns the column name for the CAM field with the focus.");
			AddFunc("stdlib.i", "fieldprompt", "Returns the prompt for the CAM field with the focus.");
			AddFunc("stdlib.i", "find", "Finds a row by using a relationship or index.", "table", "indrel", "mode");
			AddFunc("stdlib.i", "finddate", "Forms a date by repeatedly adding a given number of time periods to a starting date until the test requirement is reached.", "EndDate", "NumUnits", "units", "day", "StartDate", "test");
			AddFunc("stdlib.i", "formcomment", "Returns the comment for the CAM form with the focus.");
			AddFunc("stdlib.i", "formmode", "Returns the modality of the current form.");
			AddFunc("stdlib.i", "formname", "Returns the table name of the CAM form with the focus.");
			AddFunc("stdlib.i", "formprompt", "Returns the prompt for the CAM form with the focus.");
			AddFunc("stdlib.i", "freenotebuf", "Frees a buffer created by notebuf and not restored by pointbuf.", "TableName", "BufferNumber");
			AddFunc("stdlib.i", "freeoldbuf", "Clears the old buffer.", "TableName");
			AddFunc("stdlib.i", "genpwd", "Generates a random six-character password.", "password");
			AddFunc("stdlib.i", "getmsg", "Returns the current value of the most recent error message string.");
			AddFunc("stdlib.i", "getwarehoused", "Determines whether a transaction has been applied to its master.", "TableName");
			AddFunc("stdlib.i", "highlight", "Highlights or selects a row in a listing.", "INTENSITY");
			AddFunc("stdlib.i", "hour", "Extracts the hour value from a time.", "time");
			AddFunc("stdlib.i", "incdate", "Creates a date by adding a given number of time periods to a starting date.", "StartDate", "NumUnits", "units", "day");
			AddFunc("stdlib.i", "insert", "Inserts a row in a table (Name = table), or relates two rows in a relationship (Name = 1-1, 1-m relationship), or inserts a row in a m-m table, and relates that row to each row in both sides of the m-m relationship (Name = m-m relationship).", "Name", "mode");
			AddFunc("stdlib.i", "insrel", "For standard relationships, an insrel inserts a row in TableName, and relates that row to the row that is current in the other side of the relationship. For m-m relationships, an insrel inserts a row in TableName, then inserts a row in the m-m table identified by RelationshipName, and then relates that m-m row to the other inserted row, as well as to the current row in the other side of the m-m relationship.", "TableName", "RelationshipName", "mode");
			AddFunc("stdlib.i", "integ", "Checks or fixes the integrity of a time relationship.", "RelationshipName", "mode");
			AddFunc("stdlib.i", "integRange", "Checks or fixes the integrity of a time relationship within a specified date range.", "RelationshipName", "mode", "startDate", "endDate");
			AddFunc("stdlib.i", "isgermane", "Determines whether the current interface (e.g., CAM) to AFS has registered the germane key.", "keystring");
			AddFunc("stdlib.i", "logten", "Calculates the base 10 logarithm of a number.", "value");
			AddFunc("stdlib.i", "listupdates", "Lists updates for the current row by retrieving data from the central updates table.", "TableName", "StartDate", "EndDate", "headings", "rowmarkers", "NoneFound", "message", "MessageCol", "UpdateCol");
			AddFunc("stdlib.i", "makedate", "Creates a date from a given year, month and day.", "year", "month", "day");
			AddFunc("stdlib.i", "maketime", "Creates a time from a given hour, minute and second.", "hour", "minute", "second");
			AddFunc("stdlib.i", "minute", "Extracts the minute value from a time.", "time");
			AddFunc("stdlib.i", "month", "Extracts the month value from a date.", "date");
			AddFunc("stdlib.i", "notebuf", "Temporarily saves to memory the contents of the current row buffer so that it can be recalled later by the pointbuf function.", "TableName");
			AddFunc("stdlib.i", "noterow", "Notes the rowno value of the server row buffer so that the \"same\" row can be retrieved from the database later by calling the pointrow function.", "TableName");
			AddFunc("stdlib.i", "nullstr", "Converts a NULL pointer to a null string.", "pointer");
			AddFunc("stdlib.i", "openform", "Opens a new form, or switches focus to an open form that shares the same modality, form type, \"parent\", and table. A parent is an instance of a workspace.", "TableName", "mode");
			AddFunc("stdlib.i", "pageno", "Returns the page number of the output within the current output file.");
			AddFunc("stdlib.i", "pagerow", "Returns the line number on the current output page.");
			AddFunc("stdlib.i", "PCounterStart", "Returns a number used as a parameter for the PCounterElapsed function.");
			AddFunc("stdlib.i", "PCounterElapsed", "Returns the time elapsed since the calling of PCounterStart.", "StartTime");
			AddFunc("stdlib.i", "pendchar", "Reads a keyboard character entered at the console.");
			AddFunc("stdlib.i", "pointbuf", "Overwrites the contents of the current row buffer with the values stored by notebuf.", "TableName", "NotebufVariable");
			AddFunc("stdlib.i", "pointrow", "Retrieves from the database the row identified by the noterow variable, and overwrites the current row buffer and the old row buffer with the values of the row retrieved.", "TableName", "NoterowVariable");
			AddFunc("stdlib.i", "power", "Calculates the value of a number raised to a power.", "base", "exponent");
			AddFunc("stdlib.i", "print", "Prints the active form or listing to the specified printer, as listed in the Printers Setup dialog in CAM.", "PrinterNumber");
			AddFunc("stdlib.i", "printlisting", "Prints a named listing to the default printer.", "ListingName", "Reserved");
			AddFunc("stdlib.i", "puts", "Writes a string to the screen.", "string");
			AddFunc("stdlib.i", "pwordkey", "Extracts a keyword from a string.", "string", "keyword", "MaxLength");
			AddFunc("stdlib.i", "pwordget", "Extracts words from a string.", "string", "from", "to");
			AddFunc("stdlib.i", "qaction", "Instructs SAM to call the action function with the same parameters as the qaction.", "TableName", "ActionName");
			AddFunc("stdlib.i", "qlistingreq", "Instructs SAM to return the output of a server action to the client.", "rows", "cols", "SkipPages", "GenPages");
			AddFunc("stdlib.i", "qbufreq", "Requests SAM to return the contents of its current row buffer to the client. SAM's current row buffer overwrites the client's current row buffer, and overwrites the client's old buffer, but only if the rowno value of SAM's buffer is non-zero.", "TableName");
			AddFunc("stdlib.i", "qbufsend", "Sends the currrent row buffer from a client to SAM, or from SAM to a client. The current row buffer of the recepient is overwritten. The old buffer of CAM is also overwritten, but only if the rowno value of SAM's buffer is non-zero.", "TableName");
			AddFunc("stdlib.i", "qcompareoldbuf", "Sends CAM's old buffer to SAM who then compares its current row buffer to the former. If the two buffers do not match, an error is returned to the client.", "TableName");
			AddFunc("stdlib.i", "qdelete", "Instructs SAM to call the delete function with the same parameters as the qdelete.", "TableName");
			AddFunc("stdlib.i", "qemsgbuf", "Packs the current getmsg value into the output buffer. When sent back to the client, the qemsgbuf function will perform a setmsg. This function does not change the value stored in the getmsg() function at the server.");
			AddFunc("stdlib.i", "qfind", "Instructs SAM to call the find function with the same parameters as the qfind.", "TableName", "indrel", "mode");
			AddFunc("stdlib.i", "qgetcur", "Requests SAM to make current all parents of the current row, from the top of a relationship hierarchy down to the immediate parent, for the current relationship.");
			AddFunc("stdlib.i", "qinit", "Resets the output buffer to an initial state for subsequent q-buffer instructions.");
			AddFunc("stdlib.i", "qinsert", "Instructs SAM to call the insert function with the same parameters as qinsert.", "Name", "mode");
			AddFunc("stdlib.i", "qinsrel", "Instructs SAM to call the insrel function with the same parameters as qinsrel.", "TableName", "RelationshipName", "mode");
			AddFunc("stdlib.i", "qkeysend", "Sends the values in the client's current row buffer corresponding to key columns of an index or relationship to SAM.", "TableName", "indrel");
			AddFunc("stdlib.i", "qlocaleid", "Instructs SAM what localeid to use, for the scope of the client request.", "localeid");
			AddFunc("stdlib.i", "qroom", "Checks the space available in the message buffer to be sent from the client to the server, or vice-versa.");
			AddFunc("stdlib.i", "qrownoreq", "Requests SAM to return the client the rowno value in SAM's current row buffer.", "TableName");
			AddFunc("stdlib.i", "qrownosend", "Sends the row number at the top of the client's qrownoreq stack to SAM, and removes that number from the stack.", "TableName");
			AddFunc("stdlib.i", "qsend", "Sends the message queue from the client to SAM.");
			AddFunc("stdlib.i", "qsetoldbuf", "Requests SAM to copy the contents of the current row buffer into the server's old buffer.", "TableName");
			AddFunc("stdlib.i", "qupdate", "Instructs SAM to call the update function with the same parameters as qupdate.", "TableName", "mode");
			AddFunc("stdlib.i", "qupdrel", "Instructs SAM to call the updrel function with the same parameters as the qupdrel.", "TableName", "RelationshipName", "mode");
			AddFunc("stdlib.i", "revoke", "Revokes inserts or updates.", "mode");
			AddFunc("stdlib.i", "rollback", "Rolls back a unit of work initiated with beginwork.", "work");
			AddFunc("stdlib.i", "rowmarker", "Marks lines in an online listing so that rows can be selected.", "TableName");
			AddFunc("stdlib.i", "runtriggers", "Allows a client action, listing, before_action, before_listing, or after_listing to force a cascading of triggers in a client form.", "mode");
			AddFunc("stdlib.i", "savedbuser", "Saves the dbuser.lst file (output received from SAM) to FileName.", "FileName");
			AddFunc("stdlib.i", "second", "Extracts the second value from a time.", "time");
			AddFunc("stdlib.i", "sesprio", "Sets the priority level of a network session with SAM.", "database", "priority");
			AddFunc("stdlib.i", "SetAccessMode", "Specifies the mode used to access the database.", "mode");
			AddFunc("stdlib.i", "setkilltime", "Sets the date and time at which to initiate a controlled shutdown of SAM.", "KillDate", "KillTime");
			AddFunc("stdlib.i", "setlangid", "Changes the langid used by the application. The scope of the change depends on where the function is called.", "langid");
			AddFunc("stdlib.i", "setlocaleid", "Changes the localeid used by the application. The scope of the change depends on where the function is called.", "localeid");
			AddFunc("stdlib.i", "setmsg", "Writes to the error message buffer.", "string");
			AddFunc("stdlib.i", "setoldbuf", "Copies the contents of the current row buffer into the old buffer.", "TableName");
			AddFunc("stdlib.i", "SetPROBELocale", "Forces the display and input of numeric, date, and time data to conform to the format set by the PROBE keyword for those data types. Typically, this function is used when generating code.");
			AddFunc("stdlib.i", "setsnap", "Sets the snapshot request flag of the master row.", "RelationshipName", "SnapOption", "UpdateOption");
			AddFunc("stdlib.i", "snappod", "Indicates whether a snapshot was taken at the beginning of the day or at the end of the day.", "TableName");
			AddFunc("stdlib.i", "strcap", "Capitalizes the first character of each word in a string.", "string", "exception");
			AddFunc("stdlib.i", "strfind", "Locates the position of a given string within a string.", "string", "search");
			AddFunc("stdlib.i", "strlen", "Calculates the length of a string.", "string");
			AddFunc("stdlib.i", "strlwr", "Converts all characters in a string to lowercase.", "string");
			AddFunc("stdlib.i", "strsrch", "Locates the position of a given character in a string.", "string", "search");
			AddFunc("stdlib.i", "strstrip", "Removes trailing blanks from a string.", "string");
			AddFunc("stdlib.i", "strupr", "Converts all characters in a string to uppercase.", "string");
			AddFunc("stdlib.i", "strwords", "Converts a string into a series of space-delimited words.", "string", "exception", "spaces");
			AddFunc("stdlib.i", "sysdate", "Returns the current date as read from the computer's internal clock.");
			AddFunc("stdlib.i", "systemex", "Runs an executable program.", "command", "wait");
			AddFunc("stdlib.i", "systime", "Returns the current time as read from the computer's internal clock.");
			AddFunc("stdlib.i", "update", "Updates a row in a table.", "TableName", "mode");
			AddFunc("stdlib.i", "updrel", "Updates a row in a relationship.", "TableName", "RelationshipName", "mode");
			AddFunc("stdlib.i", "weekday", "Returns the day of the week, as an integer. (Sunday = 0)", "Date");
			AddFunc("stdlib.i", "wordpos", "Finds the position of a certain type of word within a string.", "string", "index", "MaxLength", "type");
			AddFunc("stdlib.i", "wordrepl", "Finds and optionally replaces substrings within a string.", "string", "type", "subslist");
			AddFunc("stdlib.i", "wordstrip", "Replaces consecutive spaces in a string with a single space and truncates trailing spaces.", "string");
			AddFunc("stdlib.i", "wsname", "Returns the name of the client process (e.g., a gateway or a CAM) from which wsname is called.");
			AddFunc("stdlib.i", "year", "Extracts the year value from a date.", "date");

			AddFunc("getpath.i", "getpath", "Returns some of the application directory variables defined using ACM.", "PathType");

			AddFunc("io.i", "clearcnt", "Resets the value of getcnt, a function which returns the number of bytes read or written since the last call to openf or clearcnt.");
			AddFunc("io.i", "closef", "Closes the file that was opened with openf.");
			AddFunc("io.i", "getcnt", "Counts the number of bytes read or written since the last call to openf or clearcnt.");
			AddFunc("io.i", "openf", "Opens a file for input/output.", "file", "mode", "code", "BlockSize");
			AddFunc("io.i", "rbc", "Reads a hexadecimal number from a file and converts it to an ASCII string of 1s and 0s (bitmap).", "length");
			AddFunc("io.i", "rbn", "Reads a binary number from a file and converts it to an ASCII string of 1s and 0s (bitmap).", "length");
			AddFunc("io.i", "rfiller", "Skips bytes when reading a file.", "bytes");
			AddFunc("io.i", "rn0", "Reads a number with zero decimal places from a file.", "bytes");
			AddFunc("io.i", "rn1", "Reads a number with one decimal place from a file.", "bytes");
			AddFunc("io.i", "rn2", "Reads a number with two decimal places from a file.", "bytes");
			AddFunc("io.i", "rn3", "Reads a number with three decimal places from a file.", "bytes");
			AddFunc("io.i", "rraw", "Reads raw (i.e. unconverted) characters from a file opened via the openf function. Moves the file pointer by the number of bytes in the buffer.", "buffer");
			AddFunc("io.i", "rs", "Reads a string of converted characters from a file.", "length");
			AddFunc("io.i", "statusf", "Returns the status of the file currently open. (non-zero = end-of-file)");
			AddFunc("io.i", "wbc", "Writes a hexadecimal number to a file, converting it from an ASCII string of 1s and 0s (bitmap).", "string", "bytes");
			AddFunc("io.i", "wbn", "Writes a binary number to a file, converting it from an ASCII string of 1s and 0s (bitmap). The maximum is 64 bytes.", "string", "length");
			AddFunc("io.i", "wfiller", "Writes a specified number of blank characters to a file.", "bytes");
			AddFunc("io.i", "wn0", "Writes a number with zero decimal places to a file.", "number", "bytes");
			AddFunc("io.i", "wn1", "Writes a number with one decimal place to a file.", "number", "bytes");
			AddFunc("io.i", "wn2", "Writes a number with two decimal places to a file.", "number", "bytes");
			AddFunc("io.i", "wn3", "Writes a number with three decimal places to a file.", "number", "bytes");
			AddFunc("io.i", "ws", "Writes a string of characters to a file after converting them to a binary form such as ASCII, EBCDIC, etc.", "string", "length");
			AddFunc("io.i", "clearcnt_m", "Resets the value of getcnt_m, a function which returns the number of bytes read or written for the specified fileHandle, since the last call to openf_m or clearcnt_m.", "fileHandle");
			AddFunc("io.i", "clearcnt_all", "Resets the value of getcnt_m for all open files.");
			AddFunc("io.i", "closef_m", "Closes the file (with descriptor fileHandle) that was opened with openf_m.", "fileHandle");
			AddFunc("io.i", "getcnt_all", "Counts the total number of bytes read or written since the last call to openf_m or clearcnt_m.");
			AddFunc("io.i", "getcnt_m", "Counts the number of bytes read or written since the last call to openf_m or clearcnt_m.", "fileHandle");
			AddFunc("io.i", "openf_m", "Opens a file for input/output.", "file", "openfmode", "sharemode", "permissionmode", "code", "BlockSize");
			AddFunc("io.i", "rbc_m", "Reads a hexadecimal number from the specified file and converts it to an ASCII string of 1s and 0s (bitmap).", "length", "fileHandle");
			AddFunc("io.i", "rbn_m", "Reads a binary number from the specified file and converts it to an ASCII string of 1s and 0s (bitmap).", "length", "fileHandle");
			AddFunc("io.i", "rfiller_m", "Skips bytes when reading a file.", "bytes", "fileHandle");
			AddFunc("io.i", "rn0_m", "Reads a number with zero decimal places from a file.", "bytes", "fileHandle");
			AddFunc("io.i", "rn1_m", "Reads a number with one decimal place from a file.", "bytes", "fileHandle");
			AddFunc("io.i", "rn2_m", "Reads a number with two decimal places from a file.", "bytes", "fileHandle");
			AddFunc("io.i", "rn3_m", "Reads a number with three decimal places from a file.", "bytes", "fileHandle");
			AddFunc("io.i", "rraw_m", "Reads raw (i.e. unconverted) characters from a file opened via the openf_m function. Moves the file pointer by the number of bytes in the buffer.", "buffer", "fileHandle");
			AddFunc("io.i", "rs_m", "Reads a string of converted characters from the file specified by fileHandle.", "length", "fileHandle");
			AddFunc("io.i", "statusf_m", "Returns the status of the file currently open.", "fileHandle");
			AddFunc("io.i", "wbc_m", "Writes a hexadecimal number to a file, converting it from an ASCII string of 1s and 0s (bitmap).", "string", "bytes", "fileHandle");
			AddFunc("io.i", "wbn_m", "Writes a binary number to a file, converting it from an ASCII string of 1s and 0s (bitmap). The maximum is 64 bytes.", "string", "length", "fileHandle");
			AddFunc("io.i", "wfiller_m", "Writes a specified number of blank characters to a file.", "bytes", "fileHandle");
			AddFunc("io.i", "wn0_m", "Writes a number with zero decimal places to a file.", "number", "bytes", "fileHandle");
			AddFunc("io.i", "wn1_m", "Writes a number with one decimal place to a file.", "number", "bytes", "fileHandle");
			AddFunc("io.i", "wn2_m", "Writes a number with two decimal places to a file.", "number", "bytes", "fileHandle");
			AddFunc("io.i", "wn3_m", "Writes a number with three decimal places to a file.", "number", "bytes", "fileHandle");
			AddFunc("io.i", "ws_m", "Writes a string of characters to a file after converting them to a binary form such as ASCII, EBCDIC, etc.", "string", "length", "fileHandle");
		}

		private static Dictionary<string, Dictionary<string, FuncDoc>> _doc = new Dictionary<string, Dictionary<string, FuncDoc>>();

		private static void AddFunc(string fileName, string funcName, string desc, params string[] args)
		{
			Dictionary<string, FuncDoc> file;
			if (!_doc.TryGetValue(fileName, out file))
			{
				file = new Dictionary<string, FuncDoc>();
				_doc[fileName] = file;
			}

			file[funcName] = new FuncDoc(funcName, desc, args);
		}

		public class FuncDoc
		{
			private string _name;
			private string _desc;
			private string[] _args;

			public FuncDoc(string name, string desc, string[] args)
			{
				_name = name;
				_desc = desc;
				_args = args;
			}

			public string Name
			{
				get { return _name; }
			}

			public string Description
			{
				get { return _desc; }
			}

			public string[] Arguments
			{
				get { return _args; }
			}
		}

		public static FuncDoc GetDocumentation(string fileName, string funcName)
		{
			if (_initialized)
			{
				Initialize();
				_initialized = true;
			}

			Dictionary<string, FuncDoc> file;
			if (_doc.TryGetValue(Path.GetFileName(fileName).ToLower(), out file))
			{
				FuncDoc func;
				if (file.TryGetValue(funcName, out func)) return func;
			}

			return null;
		}
	}
}

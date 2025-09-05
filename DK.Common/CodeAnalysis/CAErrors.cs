using System;
using System.Linq;

namespace DK.CodeAnalysis
{
    public enum CAError
    {
        #region DK Errors
        [ErrorMessage("Strings passed by reference are immutable; changes are not reflected back to the caller.")]
        [Warning]
        CA00106,

        [ErrorMessage("Converting from '{0}' to '{1}'; possible data loss.")]
        [Warning]
        CA00108,
        #endregion

        #region General Parsing
        [ErrorMessage("Unknown '{0}'.")]
        CA10001,

        [ErrorMessage("Function '{0}' with {1} argument(s) not found.")]
        CA10002,

        [ErrorMessage("Function '{0}' not found.")]
        CA10003,

        [ErrorMessage("Expected identifier to follow '.'")]
        CA10004,

        [ErrorMessage("Expected identifier to follow '$'.")]
        CA10005,

        [ErrorMessage("Expected expression.")]  // In brackets
        CA10009,

        [ErrorMessage("Expected ';'.")]
        CA10015,

        [ErrorMessage("Expected condition after '{0}'.")]
        CA10018,

        [ErrorMessage("Expected '{'.")]
        CA10019,

        [ErrorMessage("Expected '('.")]
        CA10025,

        [ErrorMessage("Expected ';'.")]
        CA10026,

        [ErrorMessage("Expected ')'.")]
        CA10027,

        [ErrorMessage("Statement is not valid here.")]
        CA10030,

        [ErrorMessage("Expected '='.")]
        CA10033,

        [ErrorMessage("Unmatched '{0}'.")]
        CA10076,
        #endregion

        #region Operators
        [ErrorMessage("Operator '?' expects ':' on right.")]
        CA10021,

        [ErrorMessage("Unknown operator '{0}'.")]
        CA10006,

        [ErrorMessage("Operator '{0}' expects value on left.")]
        CA10007,

        [ErrorMessage("Operator '{0}' expects value on right.")]
        CA10008,

        [ErrorMessage("Operator '{0}' expects assignable value on left.")]
        CA10100,

        [ErrorMessage("Syntax error.")]
        CA10101,

        [ErrorMessage("Unknown identifier.")]	// For writing
        CA10102,

        [ErrorMessage("Unknown identifier.")]	// For reading
        CA10103,
        #endregion

        #region Variable Usage (0110-0119)
        [ErrorMessage("Use of uninitialized variable '{0}'.")]
        [Warning]
        CA10110,

        [ErrorMessage("Variable '{0}' is assigned a value, but is never used.")]
        [Warning]
        CA10111,

        [ErrorMessage("Variable '{0}' is not used.")]
        [Warning]
        CA10112,

        [ErrorMessage("Variable '{0}' has already been declared.")]
        CA10113,

        [ErrorMessage("Passing the result of division into a string argument will trigger a compiler bug.")]
        CA10082,
        #endregion

        #region Select Statements
        [ErrorMessage("Expected '{0}'.")]	// Used for select statements
        CA10034,

        [ErrorMessage("Table or relationship '{0}' does not exist.")]
        CA10035,

        [ErrorMessage("Expected table name after 'of'.")]
        CA10036,

        [ErrorMessage("Table or relationship '{0}' is not referenced in the 'from' clause.")]
        CA10037,

        [ErrorMessage("Expected table or relationship name.")]
        CA10038,

        [ErrorMessage("Expected column name to follow table name.")]
        CA10039,

        [ErrorMessage("Table '{0}' has no column '{1}'.")]
        CA10040,

        [ErrorMessage("Expected number after 'top'.")]
        CA10072,

        [ErrorMessage("Assignment in select where clause.")]
        CA10073,
        #endregion

        #region Switch Statements
        [ErrorMessage("Expected case value.")]
        CA10028,

        [ErrorMessage("Expected ':'.")]
        CA10029,

        [ErrorMessage("Switch fall-throughs are inadvisable.")]
        [Warning]
        CA10031,

        [ErrorMessage("Duplicate default case.")]
        CA10032,
        #endregion

        #region Conditional Statements
        [ErrorMessage("Expected ':' to follow conditional result.")]
        CA10041,

        [ErrorMessage("Expected value to follow conditional '?'.")]
        CA10042,

        [ErrorMessage("Expected value to follow conditional ':'.")]
        CA10043,

        [ErrorMessage("Conditional statements should be wrapped in brackets to avoid compiler bugs.")]
        [Warning]
        CA10071,
        #endregion

        #region Extract Statements
        [ErrorMessage("Expected extract table name to follow 'extract'.")]
        CA10044,

        [ErrorMessage("Extract table '{0}' does not exist.")]
        CA10045,

        [ErrorMessage("Expected extract column name.")]
        CA10046,

        [ErrorMessage("Expected '=' to follow extract column name.")]
        CA10047,

        [ErrorMessage("Expected extract column expression.")]
        CA10048,
        #endregion

        #region Values
        [ErrorMessage("{0} cannot be used with this value.")]
        CA10050,

        [ErrorMessage("Division by zero.")]
        [Warning]
        CA10051,

        [ErrorMessage("Date math results in an out-of-bounds value.")]
        [Warning]
        CA10052,

        [ErrorMessage("Enum math results in an out-of-bounds value.")]
        [Warning]
        CA10053,

        [ErrorMessage("Time math results in an out-of-bounds value.")]
        [Warning]
        CA10054,

        [ErrorMessage("Converting {0} to {1}.")]
        [Warning]
        CA10055,

        [ErrorMessage("Char math results in an out-of-bounds value.")]
        [Warning]
        CA10056,

        [ErrorMessage("Function expects {0} argument(s).")]
        CA10057,

        [ErrorMessage("Use non-string enum values when possible.")]
        [Warning]
        CA10058,

        [ErrorMessage("Enum option {0} does not exist.")]
        [Warning]
        CA10059,

        [ErrorMessage("Enum option {0} does not exist; use a single space instead of an empty string.")]
        [Warning]
        CA10060,

        [ErrorMessage("Enum option '{0}' is ambigious with variable/argument of the same name.")]
        [Warning]
        CA10083,
        #endregion

        #region Aggregate Functions
        [ErrorMessage("Expected aggregate expression.")]
        CA10061,

        [ErrorMessage("Expected expression to follow '{0}'.")]
        CA10062,

        [ErrorMessage("Expected ')'.")]
        CA10063,

        [ErrorMessage("Table '{0}' does not exist.")]
        CA10064,

        [ErrorMessage("Expected table name to follow 'group'.")]
        CA10065,

        [ErrorMessage("Expected '.'")]
        CA10066,

        [ErrorMessage("Expected column name.")]
        CA10067,

        [ErrorMessage("Expected select name to follow 'in'.")]
        CA10068,
        #endregion

        #region Highlighting
        [ErrorMessage("This expression writes to the report stream.")]
        [ReportOutputTag]
        CA10070,
        #endregion

        #region Function Calls (10120-10129)
        /// <summary>
        /// Deprecated function call.
        /// {0} = description text.
        /// </summary>
        [ErrorMessage("{0}")]
        CA10120,

        [ErrorMessage("Function requires {0} arguments. ({1} passed)")]
        CA10121,

        [ErrorMessage("This function should not be called in a select where clause.")]
        CA10077,
        #endregion

        #region In Operator (0130-0139)
        [ErrorMessage("Expected '('.")]
        CA10130,

        [ErrorMessage("Expected ','.")]
        CA10131,

        [ErrorMessage("Expected expression.")]
        CA10132,

        [ErrorMessage("'in' operator requires at least 1 expression.")]
        CA10133,
        #endregion

        #region Like Operator (10140-10149)
        [ErrorMessage("'like' operator may only be used with a string.")]
        CA10140,
        #endregion

        #region Goto Operator (10150-10159)
        [ErrorMessage("Expected goto label.")]
        CA10150,
        #endregion

        #region Preprocessor (10160-10169)
        [ErrorMessage("Wrong number of arguments passed to macro. {0} passed, {1} expected.")]
        CA10160,

        [ErrorMessage("Cannot find include file '{0}'.")]
        CA10074,
        #endregion

        #region Function Arguments (10170-10179)
        [ErrorMessage("String constant must be quoted.")]
        CA10170,

        [ErrorMessage("Function arguments could not be parsed.")]
        CA10171,

        [ErrorMessage("Expected ','.")]
        CA10172,
        #endregion

        #region Arrays
        [ErrorMessage("Array indexer requires variable on left.")]
        CA10020,

        [ErrorMessage("Only 1 or 2 index accessors allowed.")]
        CA10022,

        [ErrorMessage("Expected {0} array indexers but got {1}.")]
        CA10180,

        [ErrorMessage("Expected array indexer to follow variable.")]
        CA10075,
        #endregion

        #region Function Definitions (10190-10199)
        [ErrorMessage("Unreachable code.")]
        [Warning]
        CA10016,

        [ErrorMessage("Not all code branches return a value.")]
        [Warning]
        CA10017,

        [ErrorMessage("Function '{0}' has already been defined.")]
        CA10190,

        [ErrorMessage("Argument '{0}' has already been declared.")]
        CA10191,

        [ErrorMessage("Duplicate #SQLWhereClauseCompatibleAttribute.")]
        CA10078,

        [ErrorMessage("#SQLResultsFilteringAttribute cannot be used with #SQLWhereClauseCompatibleAttribute.")]
        CA10079,

        [ErrorMessage("Duplicate #SQLResultsFilteringAttribute.")]
        CA10080,

        [ErrorMessage("#SQLWhereClauseCompatibleAttribute cannot be used with #SQLResultsFilteringAttribute.")]
        CA10081,
        #endregion

        #region Return Statements
        [ErrorMessage("Expected value after 'return'.")]
        CA10014,
        #endregion

        #region Break / Continue
        [ErrorMessage("'break' is not valid here.")]
        CA10023,

        [ErrorMessage("'continue' is not valid here.")]
        CA10024,
        #endregion

        // Last CA10083
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    class ErrorMessageAttribute : Attribute
    {
        private string _message;

        public ErrorMessageAttribute(string message)
        {
            _message = message;
        }

        public string Message
        {
            get { return _message; }
        }
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    class WarningAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    class ReportOutputTagAttribute : Attribute
    {
    }

    static class CAErrorEx
    {
        public static string GetText(this CAError code, object[] args)
        {
            var codeString = code.ToString();

            var memInfo = typeof(CAError).GetMember(codeString);
            if (memInfo == null || memInfo.Length == 0) return codeString;

            var attrib = memInfo[0].GetCustomAttributes(typeof(ErrorMessageAttribute), false);
            if (attrib == null || attrib.Length == 0) return codeString;

            var message = ((ErrorMessageAttribute)attrib[0]).Message;
            if (args != null && args.Length > 0) message = string.Format(message, args);
            return string.Concat(codeString, ": ", message);
        }

        public static CAErrorType GetErrorType(this CAError code)
        {
            var memInfo = typeof(CAError).GetMember(code.ToString());
            if (memInfo == null || memInfo.Length == 0) return CAErrorType.Error;

            if (memInfo[0].GetCustomAttributes(typeof(WarningAttribute), false).Any()) return CAErrorType.Warning;
            if (memInfo[0].GetCustomAttributes(typeof(ReportOutputTagAttribute), false).Any()) return CAErrorType.ReportOutputTag;
            return CAErrorType.Error;
        }
    }
}

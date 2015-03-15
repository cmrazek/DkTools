#if REPORT_ERRORS
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.ErrorTagging
{
	internal enum ErrorCode
	{
		[Description("(none)")]
		None,

		#region Root
		[Description("Expected function or variable name to follow data type on root.")]
		Root_UnknownAfterDataType = 100,

		[Description("Expected data type to follow 'static'.")]
		Root_UnknownAfterStatic,

		[Description("Expected data type or function name to follow 'extern'.")]
		Root_UnknownAfterExtern,

		[Description("Unknown identifier '{0}'.")]
		Root_UnknownIdent,

		[Description("Unexpected '{0}' after 'create'.")]
		Root_UnknownAfterCreate,
		#endregion

		#region Functions
		[Description("Expected '(' to follow function name.")]
		Func_NoArgBracket,

		[Description("Expected function name or data type to follow '{0}'.")]
		Func_UnknownAfterPrivacy,

		[Description("Expected function argument.")]
		Func_NoArg,

		[Description("Expected 'ENDHLP'.")]
		Func_NoEndHlp,

		[Description("Expected '{'.")]
		Func_NoBodyStart,
		#endregion

		#region Variable Declarations
		[Description("Expected either ',' or ';' to follow a variable declaration.")]
		VarDecl_UnknownAfterName,
		#endregion

		#region Extracts
		[Description("Expected extract name.")]
		Extract_NoName,

		[Description("Expected ';' to end extract.")]
		Extract_NoTerminator,
		#endregion

		#region Enum
		[Description("Expected ',' in enum option list.")]
		Enum_NoComma,

		[Description("Duplicate enum option '{0}'.")]
		Enum_DuplicateOption,

		[Description("Unexpected end of file in enum option list.")]
		Enum_UnexpectedEndOfFile,

		[Description("Expected enum option rather than ','.")]
		Enum_UnexpectedComma,
		#endregion

		#region Create Table
		[Description("Expected table name to follow 'create table'.")]
		CreateTable_NoTableName,

		[Description("Expected table number to follow '{0}'.")]
		CreateTable_NoTableNumber,

		[Description("Expected database number to follow 'database'.")]
		CreateTable_NoDatabaseNumber,

		[Description("Expected frequency number to follow 'snapshot'.")]
		CreateTable_NoFrequencyNumber,

		[Description("Expected string to follow 'prompt'.")]
		CreateTable_NoPromptString,

		[Description("Expected string to follow 'comment'.")]
		CreateTable_NoCommentString,

		[Description("Expected string to follow 'image'.")]
		CreateTable_NoImageString,

		[Description("Expected string(s) to follow 'description'.")]
		CreateTable_NoDescriptionString,

		[Description("Expected tag name to follow 'tag'.")]
		CreateTable_NoTagName,

		[Description("Expected string to follow '{0}'.")]
		CreateTable_NoTagValue,

		[Description("Invalid tag '{0}' for 'create table'.")]
		CreateTable_InvalidTagName,

		[Description("Use '(' rather than '{'.")]
		CreateTable_UsingOpenBraceInsteadOfBracket,

		[Description("Use ')' rather than '}'.")]
		CreateTable_UsingCloseBraceInsteadOfBracket,

		[Description("Expected '('.")]
		CreateTable_NoOpenBrace,

		[Description("Expected ')'.")]
		CreateTable_NoCloseBrace,

		[Description("Expected table name to follow 'updates'.")]
		CreateTable_NoUpdatesTableName,

		[Description("Table name exceeds max length of {0}.")]
		CreateTable_UpdatesTableNameTooLong,
		#endregion

		#region Column Definition
		[Description("Expected data type to follow '{0}'.")]
		ColDef_NoDataType,

		[Description("Expected 'INTENSITY_x'.")]
		ColDef_NoIntensity,

		[Description("Expected key sequence to follow 'accel'.")]
		ColDef_NoAccelSequence,

		[Description("Expected key code to follow '+'.")]
		ColDef_NoKeyCodeAfterPlus,

		[Description("Expected file name string to follow 'image'.")]
		ColDef_NoImageFileName,

		[Description("Expected string to follow 'prompt'.")]
		ColDef_NoPromptString,

		[Description("Expected string to follow 'comment'.")]
		ColDef_NoCommentString,

		[Description("Expected string(s) to follow 'description'.")]
		ColDef_NoDescriptionStrings,

		[Description("Expected offset to follow '{0}'.")]
		ColDef_NoCoordinateOffset,

		[Description("Expected coordinate to follow '{0}'.")]
		ColDef_NoCoordinate,

		[Description("Expected {0} to follow '{1}'.")]
		ColDef_NoSize,

		[Description("Expected string to follow 'group'.")]
		ColDef_NoGroupTitle,

		[Description("Unexpected '{0}'.")]
		ColDef_UnknownAttribute,
		#endregion

		#region Dot Separated Words
		[Description("'{0}.{1}' does not exist.")]
		DotSepWords_NotFound,

		[Description("Table '{0}' has no column '{1}'.")]
		DotSepWords_TableFieldNotFound,

		[Description("Expected identifier to follow '{0}.'")]
		DotSepWords_NoWord2,
		#endregion

		#region Functions
		[Description("Unable to continue analyzing due to a parsing error.")]
		Func_InfiniteLoop,

		[Description("Invalid '{0}'.")]
		Func_InvalidToken,
		#endregion

		#region Statements
		[Description("'{0}' does not exist.")]
		Stmt_WordNotFound,

		[Description("Function is not closed.")]
		Stmt_UnclosedFunction,

		[Description("Function '{0}' does not exist.")]
		Stmt_FunctionNotFound,

		[Description("Unknown '{0}'.")]
		Stmt_UnknownToken,

		//[Description("Expected '(' to follow function name '{0}'.")]
		//Stmt_FunctionWithoutFollowingBracket,

		[Description("Empty statement.")]
		Stmt_Empty,

		[Description("Statement not terminated with ';'.")]
		Stmt_NotComplete,

		//[Description("Unbalanced '{0}'.")]
		//Stmt_UnbalancedClosedBracket,	// TODO: remove

		[Description("'{' requires preceding control statement (e.g. 'if')")]
		Stmt_OpenBraceAlone,

		[Description("'{0}' is not valid here.")]
		Stmt_InvalidOperator,

		[Description("'{0}' is not closed.")]
		Stmt_BracketNotClosed,
		#endregion

		#region If Statements
		[Description("Expected '{' to follow 'if' and condition statements.")]
		If_ConditionNotComplete,

		[Description("'if' body is not complete.")]
		If_MainBodyNotComplete,

		[Description("'if-else' body is not complete.")]
		If_ElseBodyNotComplete,

		[Description("Expected '{' to follow 'else'.")]
		If_ElseNotComplete,
		#endregion

		#region Function Calls
		[Description("Argument list is not complete.")]
		FuncCall_ArgsNotComplete,
		#endregion

		#region Switch Statements
		[Description("Expected '{' to follow 'switch' and condition statements.")]
		Switch_ConditionNotComplete,

		[Description("Expected 'case' or 'default'.")]
		Switch_NoCaseOrDefault,

		[Description("'switch' body is not complete.")]
		Switch_BodyNotComplete,

		[Description("Expected ':' to follow '{0}'.")]
		Switch_CaseValueNotComplete,

		[Description("Duplicate 'default'.")]
		Switch_DuplicateDefault,

		[Description("Switch body must begin with 'case' or 'default'.")]
		Switch_BodyDoesNotStartWithCaseOrDefault,
		#endregion

		#region Return Statements
		[Description("'return' statement does not end with ';'.")]
		Return_NotComplete,
		#endregion
	}
}
#endif

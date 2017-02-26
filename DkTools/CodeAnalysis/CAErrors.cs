using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.ErrorTagging;

namespace DkTools.CodeAnalysis
{
	enum CAError
	{
		[ErrorMessage("Unknown '{0}'.")]
		CA0001,

		[ErrorMessage("Function '{0}' with {1} argument(s) not found.")]
		CA0002,

		[ErrorMessage("Function '{0}' not found.")]
		CA0003,

		[ErrorMessage("Expected identifier to follow '.'")]
		CA0004,

		//[ErrorMessage("Empty statement not allowed.")]
		//CA0005,

		[ErrorMessage("Unknown operator '{0}'.")]
		CA0006,

		[ErrorMessage("Operator '{0}' expects value on left.")]
		CA0007,

		[ErrorMessage("Operator '{0}' expects value on right.")]
		CA0008,

		[ErrorMessage("Use of uninitialized value.")]
		[Warning]
		CA0009,

		[ErrorMessage("Operator '{0}' expects assignable value on left.")]
		CA0010,

		[ErrorMessage("Syntax error.")]
		CA0011,

		[ErrorMessage("Cannot write to this identifier.")]
		CA0012,

		[ErrorMessage("Cannot read from this identifier.")]
		CA0013,

		[ErrorMessage("Expected value after 'return'.")]
		CA0014,

		[ErrorMessage("Expected ';'.")]
		CA0015,

		[ErrorMessage("Unreachable code.")]
		[Warning]
		CA0016,

		[ErrorMessage("Function does not return a value.")]
		[Warning]
		CA0017,

		[ErrorMessage("Expected condition after '{0}'.")]
		CA0018,

		[ErrorMessage("Expected '{'.")]
		CA0019,

		[ErrorMessage("Array indexer requires variable on left.")]
		CA0020,

		[ErrorMessage("Operator '?' expects ':' on right.")]
		CA0021,

		[ErrorMessage("Only 1 or 2 index accessors allowed.")]
		CA0022,

		[ErrorMessage("'break' is not valid here.")]
		CA0023,

		[ErrorMessage("'continue' is not valid here.")]
		CA0024,

		[ErrorMessage("Expected '('.")]
		CA0025,

		[ErrorMessage("Expected ';'.")]
		CA0026,

		[ErrorMessage("Expected ')'.")]
		CA0027,

		[ErrorMessage("Expected case value.")]
		CA0028,

		[ErrorMessage("Expected ':'.")]
		CA0029,

		[ErrorMessage("Statement is not valid here.")]
		CA0030,

		[ErrorMessage("Switch fall-throughs are inadvisable.")]
		[Warning]
		CA0031,

		[ErrorMessage("Duplicate default case.")]
		CA0032,

		[ErrorMessage("Expected '='.")]
		CA0033,
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

		public static ErrorType GetErrorType(this CAError code)
		{
			var memInfo = typeof(CAError).GetMember(code.ToString());
			if (memInfo == null || memInfo.Length == 0) return ErrorType.Error;

			var attrib = memInfo[0].GetCustomAttributes(typeof(WarningAttribute), false);
			if (attrib == null || attrib.Length == 0) return ErrorType.Error;

			return ErrorType.Warning;
		}
	}
}

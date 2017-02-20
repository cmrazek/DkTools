using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

		[ErrorMessage("Empty statement not allowed.")]
		CA0005,

		[ErrorMessage("Unknown operator '{0}'.")]
		CA0006,

		[ErrorMessage("Operator '{0}' expects value on left.")]
		CA0007,

		[ErrorMessage("Operator '{0}' expects value on right.")]
		CA0008,

		[ErrorMessage("Use of uninitialized value.")]
		CA0009,

		[ErrorMessage("Operator '{0}' expects assignable value on left.")]
		CA0010,

		[ErrorMessage("Syntax error.")]
		CA0011,
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
	}
}

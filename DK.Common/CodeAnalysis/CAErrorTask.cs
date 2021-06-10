using DK.Code;

namespace DK.CodeAnalysis
{
	public struct CAErrorTask
	{
		public CAError ErrorCode { get; private set; }
		public string Message { get; private set; }
		public string FilePath { get; private set; }
		public CodeSpan Span { get; private set; }
		public int LineNumber { get; private set; }
		public int LinePosition { get; private set; }
		public string InvokingFilePath { get; private set; }

		public CAErrorTask(CAError errorCode, string message, string filePath, CodeSpan span, int lineNumber, int linePosition, string invokingFilePath)
		{
			ErrorCode = errorCode;
			Message = message;
			FilePath = filePath;
			Span = span;
			LineNumber = lineNumber;
			LinePosition = linePosition;
			InvokingFilePath = invokingFilePath;
		}
	}
}

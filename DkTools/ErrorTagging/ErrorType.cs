using System;

namespace DkTools.ErrorTagging
{
	[Flags]
	enum ErrorType
	{
		Error = 0x01,
		Warning = 0x02,
		CodeAnalysisError = 0x04,
		ReportOutputTag = 0x08,

		ErrorTaskMask = Error | Warning | CodeAnalysisError,
		ErrorMarkerMask = ReportOutputTag
	}

	static class ErrorTypeEx
	{
		public static ErrorType? Combine(this ErrorType? a, ErrorType? b)
		{
			if (!a.HasValue && !b.HasValue) return null;
			if (a.HasValue && !b.HasValue) return a.Value;
			if (!a.HasValue && b.HasValue) return b.Value;
			if (a.Value == ErrorType.Error || b.Value == ErrorType.Error) return ErrorType.Error;
			return a.Value;
		}

		public static string GetErrorTypeString(this ErrorType type)
		{
			switch (type)
			{
				case ErrorType.Warning:
					return VSTheme.CurrentTheme == VSThemeMode.Light ? ErrorTagger.CodeWarningLight : ErrorTagger.CodeWarningDark;
				case ErrorType.CodeAnalysisError:
					return VSTheme.CurrentTheme == VSThemeMode.Light ? ErrorTagger.CodeAnalysisErrorLight : ErrorTagger.CodeAnalysisErrorDark;
				case ErrorType.ReportOutputTag:
					return VSTheme.CurrentTheme == VSThemeMode.Light ? ErrorTagger.ReportOutputTagLight : ErrorTagger.ReportOutputTagDark;
				default:
					return VSTheme.CurrentTheme == VSThemeMode.Light ? ErrorTagger.CodeErrorLight : ErrorTagger.CodeErrorDark;
			}
		}
	}
}

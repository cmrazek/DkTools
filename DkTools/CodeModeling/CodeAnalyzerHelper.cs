﻿using DK.CodeAnalysis;
using DkTools.ErrorTagging;
using System.Threading;

namespace DkTools.CodeModeling
{
	static class CodeAnalyzerHelper
	{
		public static CodeAnalysisResults Run(this CodeAnalyzer ca, CancellationToken cancel)
		{
			// Caller should do the following after:
			//ErrorTaskProvider.Instance.ReplaceForSourceAndInvokingFile(ErrorTaskSource.CodeAnalysis, ca.CodeModel.FilePath, results.Tasks.Select(x => x.ToErrorTask()));
			//ErrorMarkerTaggerProvider.ReplaceForSourceAndFile(ErrorTaskSource.CodeAnalysis, ca.CodeModel.FilePath, results.Markers.Select(x => x.ToErrorMarkerTag()));

			var editorOptions = ProbeToolsPackage.Instance.EditorOptions;

			return ca.RunAndGetResults(new CAOptions
			{
				HighlightReportOutput = editorOptions.HighlightReportOutput,
				MaxWarnings = editorOptions.MaxWarnings
			}, cancel);
		}

		public static ErrorTask ToErrorTask(this CAErrorTask task)
		{
			return new ErrorTask(
				invokingFilePath: task.InvokingFilePath,
				filePath: task.FilePath,
				lineNum: task.LineNumber,
				lineCol: task.LinePosition,
				message: task.Message,
				type: ErrorType.CodeAnalysisError,
				source: ErrorTaskSource.CodeAnalysis,
				reportedSpan: task.Span,
				errorCode: task.ErrorCode);
		}

		public static ErrorMarkerTag ToErrorMarkerTag(this CAErrorMarker marker)
		{
			return new ErrorMarkerTag(
				source: ErrorTaskSource.CodeAnalysis,
				tagType: VSTheme.CurrentTheme == VSThemeMode.Light ? ErrorTagger.ReportOutputTagLight : ErrorTagger.ReportOutputTagDark,
				filePath: marker.FilePath,
				span: marker.Span);
		}
	}
}

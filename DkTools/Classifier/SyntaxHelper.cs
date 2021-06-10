using DK.Syntax;
using Microsoft.VisualStudio.Text.Adornments;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

namespace DkTools.Classifier
{
	static class SyntaxHelper
	{
		public static ClassifiedTextRun ToVsTextRun(this string str, ProbeClassifierType type)
		{
			return new ClassifiedTextRun(ProbeClassifier.GetClassificationTypeName(type), str);
		}

		public static ClassifiedTextElement ToVsTextElement(this string str, ProbeClassifierType type)
		{
			return new ClassifiedTextElement(str.ToVsTextRun(type));
		}

		public static TextBlock ToWpfTextBlock(this string str, ProbeClassifierType type)
		{
			var brush = ProbeClassifier.GetClassificationBrush(type);
			return new TextBlock(new System.Windows.Documents.Run(str) { Foreground = brush });
		}

		public static ClassifiedTextRun ToClassifiedTextRun(this ProbeClassifiedRun run)
		{
			return new ClassifiedTextRun(ProbeClassifier.GetClassificationTypeName(run.Type), run.Text);
		}

		public static System.Windows.Documents.Run ToWpfTextRun(this ProbeClassifiedRun run)
		{
			return new System.Windows.Documents.Run(run.Text)
			{
				Foreground = ProbeClassifier.GetClassificationBrush(run.Type)
			};
		}

		public static IEnumerable<ClassifiedTextRun> ToVsTextRuns(this ProbeClassifiedString str) => str.Runs.Select(r => r.ToClassifiedTextRun()).ToArray();

		public static ClassifiedTextElement ToVsTextElement(this ProbeClassifiedString str) => new ClassifiedTextElement(str.Runs.Select(r => r.ToClassifiedTextRun()));

		public static TextBlock ToWpfTextBlock(this ProbeClassifiedString str)
		{
			var tb = new TextBlock();
			tb.Inlines.AddRange(str.Runs.Select(r => r.ToWpfTextRun()));
			return tb;
		}

		public static IEnumerable<ClassifiedTextRun> ToVsTextRuns(this ProbeClassifiedStringBuilder sb) => sb.Runs.Select(r => r.ToClassifiedTextRun()).ToArray();

		public static ClassifiedTextElement ToVsTextElement(this ProbeClassifiedStringBuilder sb) => new ClassifiedTextElement(sb.Runs.Select(r => r.ToClassifiedTextRun()));

		public static TextBlock ToWpfTextBlock(this ProbeClassifiedStringBuilder sb)
		{
			var tb = new TextBlock();
			tb.Inlines.AddRange(sb.Runs.Select(r => r.ToWpfTextRun()));
			return tb;
		}
	}
}

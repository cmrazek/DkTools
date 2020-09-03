using Microsoft.VisualStudio.Text.Adornments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace DkTools.Classifier
{
	static class ClassifiedStringHelpers
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
			var brush = ProbeClassificationDefinitions.GetForegroundBrush(ProbeClassifier.GetClassificationTypeName(type));
			return new TextBlock(new System.Windows.Documents.Run(str) { Foreground = brush });
		}
	}
}

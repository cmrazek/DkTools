using DK.Definitions;
using DK.Syntax;
using DkTools.Classifier;
using Microsoft.VisualStudio.Language.StandardClassification;
using Microsoft.VisualStudio.Text.Adornments;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DkTools.CodeModeling
{
	static class DefinitionHelper
	{
		#region Quick Info (VS2017 API)
		public static ClassifiedTextElement QuickInfoAttribute_VS(this Definition def, string label, string value)
		{
			return new ClassifiedTextElement(
				new ClassifiedTextRun(PredefinedClassificationTypeNames.Comment, label + ": "),
				new ClassifiedTextRun(PredefinedClassificationTypeNames.NaturalLanguage, value)
			);
		}

		public static TextBlock QuickInfoAttribute_WPF(this Definition def, string label, string value)
		{
			return new ProbeClassifiedString(new ProbeClassifiedRun[]
			{
				new ProbeClassifiedRun(ProbeClassifierType.Comment, label + ": "),
				new ProbeClassifiedRun(ProbeClassifierType.Normal, value)
			}).ToWpfTextBlock();
		}

		public static ClassifiedTextElement QuickInfoAttribute_VS(this Definition def, string label, ProbeClassifiedString value)
		{
			return new ClassifiedTextElement(
				new ClassifiedTextRun[] {
					new ClassifiedTextRun(PredefinedClassificationTypeNames.Comment, label + ": "),
				}.Concat(value.ToVsTextRuns())
			);
		}

		public static TextBlock QuickInfoAttribute_WPF(this Definition def, string label, ProbeClassifiedString value)
		{
			return new ProbeClassifiedString(
				new ProbeClassifiedRun[] {
					new ProbeClassifiedRun(ProbeClassifierType.Comment, label + ": ")
				}.Concat(value.Runs)
			).ToWpfTextBlock();
		}

		public static ContainerElement QuickInfoStack_VS(this Definition def, params object[] lines)
		{
			return new ContainerElement(ContainerElementStyle.Stacked, lines.Where(x => x != null).ToArray());
		}

		public static StackPanel QuickInfoStack_WPF(this Definition def, params UIElement[] lines)
		{
			var sp = new StackPanel()
			{
				Orientation = Orientation.Vertical
			};

			foreach (var line in lines)
			{
				if (line != null) sp.Children.Add(line);
			}

			return sp;
		}

		public static ClassifiedTextElement QuickInfoDescription_VS(string desc) => desc.ToVsTextElement(ProbeClassifierType.Comment);

		public static TextBlock QuickInfoDescription_WPF(string desc) => desc.ToWpfTextBlock(ProbeClassifierType.Comment);

		public static ClassifiedTextElement QuickInfoMainLine_VS(string text) => text.ToVsTextElement(ProbeClassifierType.Normal);

		public static TextBlock QuickInfoMainLine_WPF(string text) => text.ToWpfTextBlock(ProbeClassifierType.Normal);
		#endregion
	}
}

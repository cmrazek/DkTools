using DK.Syntax;
using DkTools.Classifier;
using Microsoft.VisualStudio.Text.Adornments;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DkTools.QuickInfo
{
	static class QuickInfoLayoutHelper
	{
		public static object GenerateElements_VS(this QuickInfoLayout layout)
		{
			if (layout is QuickInfoClassifiedString str)
			{
				return str.SyntaxString.ToVsTextElement();
			}

			if (layout is QuickInfoStack stack)
			{
				return new ContainerElement(ContainerElementStyle.Stacked, stack.Elements.Select(e => e.GenerateElements_VS()));
			}

			if (layout is QuickInfoAttribute att)
			{
				return new ProbeClassifiedString(
					new ProbeClassifiedString(ProbeClassifierType.Comment, att.Label + ": ").Runs.Concat(att.Value.Runs)
				).ToVsTextElement();
			}

			if (layout is QuickInfoDescription desc)
			{
				return desc.Text.ToVsTextElement(ProbeClassifierType.Comment);
			}

			if (layout is QuickInfoMainLine line)
			{
				return line.Text.ToVsTextElement(ProbeClassifierType.Normal);
			}

			if (layout is QuickInfoText text)
			{
				return text.Text.ToVsTextElement(text.Type);
			}

			return null;
		}

		public static UIElement GenerateElements_WPF(this QuickInfoLayout layout)
		{
			if (layout is QuickInfoClassifiedString str)
			{
				return str.SyntaxString.ToWpfTextBlock();
			}

			if (layout is QuickInfoStack stack)
			{
				var sp = new StackPanel();
				sp.Orientation = Orientation.Vertical;
				foreach (var element in stack.Elements) sp.Children.Add(element.GenerateElements_WPF());
				return sp;
			}

			if (layout is QuickInfoAttribute att)
			{
				return new ProbeClassifiedString(
					new ProbeClassifiedString(ProbeClassifierType.Comment, att.Label + ": ").Runs.Concat(att.Value.Runs)
				).ToWpfTextBlock();
			}

			if (layout is QuickInfoDescription desc)
			{
				return desc.Text.ToWpfTextBlock(ProbeClassifierType.Comment);
			}

			if (layout is QuickInfoMainLine line)
			{
				return line.Text.ToWpfTextBlock(ProbeClassifierType.Normal);
			}

			if (layout is QuickInfoText text)
			{
				return text.Text.ToWpfTextBlock(text.Type);
			}

			return null;
		}
	}
}

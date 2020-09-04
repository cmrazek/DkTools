using DkTools.Classifier;
using Microsoft.VisualStudio.Text.Adornments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace DkTools.QuickInfo
{
	abstract class QuickInfoLayout
	{
		public abstract object GenerateElements_VS();
		public abstract UIElement GenerateElements_WPF();
	}

	class QuickInfoClassifiedString : QuickInfoLayout
	{
		private ProbeClassifiedString _text;

		public QuickInfoClassifiedString(ProbeClassifiedString text)
		{
			_text = text ?? throw new ArgumentNullException(nameof(text));
		}

		public QuickInfoClassifiedString(params ProbeClassifiedString[] items)
		{
			var pcs = new ProbeClassifiedStringBuilder();
			foreach (var item in items)
			{
				if (item != null) pcs.AddClassifiedString(item);
			}
			_text = pcs.ToClassifiedString();
		}

		public override object GenerateElements_VS()
		{
			return _text.ToVsTextElement();
		}

		public override UIElement GenerateElements_WPF()
		{
			return _text.ToWpfTextBlock();
		}
	}

	class QuickInfoStack : QuickInfoLayout
	{
		private QuickInfoLayout[] _elements;

		public QuickInfoStack(params QuickInfoLayout[] elements)
		{
			_elements = elements.Where(e => e != null).ToArray();
		}

		public override object GenerateElements_VS()
		{
			return new ContainerElement(ContainerElementStyle.Stacked, _elements.Select(e => e.GenerateElements_VS()));
		}

		public override UIElement GenerateElements_WPF()
		{
			var sp = new StackPanel();
			sp.Orientation = Orientation.Vertical;
			foreach (var element in _elements) sp.Children.Add(element.GenerateElements_WPF());
			return sp;
		}
	}

	class QuickInfoAttribute : QuickInfoLayout
	{
		private string _label;
		private ProbeClassifiedString _value;

		public QuickInfoAttribute(string label, ProbeClassifiedString value)
		{
			_label = label ?? throw new ArgumentNullException(nameof(label));
			_value = value ?? throw new ArgumentNullException(nameof(value));
		}

		public QuickInfoAttribute(string label, string value)
		{
			_label = label ?? throw new ArgumentNullException(nameof(label));
			_value = new ProbeClassifiedString(ProbeClassifierType.Normal, value);
		}

		public override object GenerateElements_VS()
		{
			return new ProbeClassifiedString(
				new ProbeClassifiedString(ProbeClassifierType.Comment, _label + ": ").Runs.Concat(_value.Runs)
			).ToVsTextElement();
		}

		public override UIElement GenerateElements_WPF()
		{
			return new ProbeClassifiedString(
				new ProbeClassifiedString(ProbeClassifierType.Comment, _label + ": ").Runs.Concat(_value.Runs)
			).ToWpfTextBlock();
		}
	}

	class QuickInfoDescription : QuickInfoLayout
	{
		private string _text;

		public QuickInfoDescription(string text)
		{
			_text = text ?? throw new ArgumentNullException(nameof(text));
		}

		public override object GenerateElements_VS()
		{
			return _text.ToVsTextElement(ProbeClassifierType.Comment);
		}

		public override UIElement GenerateElements_WPF()
		{
			return _text.ToWpfTextBlock(ProbeClassifierType.Comment);
		}
	}

	class QuickInfoMainLine : QuickInfoLayout
	{
		private string _text;

		public QuickInfoMainLine(string text)
		{
			_text = text ?? throw new ArgumentNullException(nameof(text));
		}

		public override object GenerateElements_VS()
		{
			return _text.ToVsTextElement(ProbeClassifierType.Normal);
		}

		public override UIElement GenerateElements_WPF()
		{
			return _text.ToWpfTextBlock(ProbeClassifierType.Normal);
		}
	}

	class QuickInfoText : QuickInfoLayout
	{
		private string _text;
		private ProbeClassifierType _type;

		public QuickInfoText(ProbeClassifierType type, string text)
		{
			_type = type;
			_text = text ?? throw new ArgumentNullException(nameof(text));
		}

		public override object GenerateElements_VS()
		{
			return _text.ToVsTextElement(_type);
		}

		public override UIElement GenerateElements_WPF()
		{
			return _text.ToWpfTextBlock(_type);
		}
	}
}

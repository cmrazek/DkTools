using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VsText = Microsoft.VisualStudio.Text;
using DkTools.CodeModel.Tokens;

namespace DkTools.CodeModel.Definitions
{
	internal abstract class Definition
	{
		private string _name;
		private string _sourceFileName;
		private int _sourceStartPos;

		public abstract bool CompletionVisible { get; }
		public abstract StatementCompletion.CompletionType CompletionType { get; }
		public abstract Classifier.ProbeClassifierType ClassifierType { get; }
		public abstract string QuickInfoTextStr { get; }
		public abstract UIElement QuickInfoTextWpf { get; }

		public Definition(string name, string sourceFileName, int sourceStartPos)
		{
#if DEBUG
			if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException("name");
#endif
			_name = name;
			_sourceFileName = sourceFileName;
			_sourceStartPos = sourceStartPos;
		}

		/// <summary>
		/// Gets the name of the definition.
		/// </summary>
		public string Name
		{
			get { return _name; }
		}

		/// <summary>
		/// Gets the file name where this definition was defined.
		/// </summary>
		public string SourceFileName
		{
			get { return _sourceFileName; }
		}

		/// <summary>
		/// Gets the position in the source file name where this definition was defined.
		/// </summary>
		public int SourceStartPos
		{
			get { return _sourceStartPos; }
		}

		public void DumpTree(System.Xml.XmlWriter xml)
		{
			xml.WriteStartElement(GetType().Name);
			DumpTreeAttribs(xml);
			DumpTreeInner(xml);
			xml.WriteEndElement();
		}

		public virtual void DumpTreeAttribs(System.Xml.XmlWriter xml)
		{
			xml.WriteAttributeString("name", _name);
			xml.WriteAttributeString("sourceFileName", _sourceFileName);
			xml.WriteAttributeString("sourceStartPos", _sourceStartPos.ToString());
		}

		public virtual void DumpTreeInner(System.Xml.XmlWriter xml)
		{
		}

#if DEBUG
		public string Dump()
		{
			var sb = new StringBuilder();
			sb.AppendFormat("Type [{0}]", GetType());
			sb.AppendFormat(" Name [{0}]", Name);
			sb.AppendFormat(" File [{0}]", _sourceFileName != null ? _sourceFileName : "(null)");
			sb.AppendFormat(" Offset [{0}]", _sourceStartPos.ToString());
			sb.AppendFormat(" CompletionType [{0}]", CompletionType);
			sb.AppendFormat(" QuickInfoText [{0}]", QuickInfoTextStr.ToSingleLine());
			return sb.ToString();
		}
#endif

		public static UIElement WpfDivs(params UIElement[] items)
		{
			var panel = new StackPanel
			{
				Orientation = Orientation.Vertical,
				HorizontalAlignment = HorizontalAlignment.Left
			};

			foreach (var item in items)
			{
				if (item != null) panel.Children.Add(item);
			}

			return panel;
		}

		public static UIElement WpfDivs(IEnumerable<UIElement> items)
		{
			return WpfDivs(items.ToArray());
		}

		public static UIElement WpfAttribute(string label, string value)
		{
			var panel = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				HorizontalAlignment = HorizontalAlignment.Left
			};
			panel.Children.Add(new TextBlock
			{
				Text = label + ":",
				FontWeight = FontWeights.Bold,
				MinWidth = 75,
				HorizontalAlignment = HorizontalAlignment.Left
			});
			panel.Children.Add(new TextBlock
			{
				Text = value,
				FontStyle = FontStyles.Normal,
				HorizontalAlignment = HorizontalAlignment.Left
			});

			return panel;
		}

		public static UIElement WpfAttribute(string label, UIElement value)
		{
			var panel = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				HorizontalAlignment = HorizontalAlignment.Left
			};
			panel.Children.Add(new TextBlock
			{
				Text = label + ":",
				FontWeight = FontWeights.Bold,
				MinWidth = 75,
				HorizontalAlignment = HorizontalAlignment.Left
			});
			panel.Children.Add(value);

			return panel;
		}

		public static UIElement WpfMainLine(string text)
		{
			return new TextBlock
			{
				Text = text,
				HorizontalAlignment = HorizontalAlignment.Left,
				Margin = new Thickness(0, 0, 0, 4),
			};
		}

		public static UIElement WpfInfoLine(string text)
		{
			return new TextBlock
			{
				Text = text,
				FontStyle = FontStyles.Italic,
				HorizontalAlignment = HorizontalAlignment.Left,
				Margin = new Thickness(0, 4, 0, 0)
			};
		}

		public static UIElement WpfIndent(UIElement child)
		{
			var panel = new StackPanel
			{
				Orientation = System.Windows.Controls.Orientation.Vertical,
				Margin = new Thickness(0, 16, 0, 0),
				HorizontalAlignment = HorizontalAlignment.Left
			};
			panel.Children.Add(child);
			return panel;
		}
	}
}

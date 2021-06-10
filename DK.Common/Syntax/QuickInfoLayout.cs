using System;
using System.Collections.Generic;
using System.Linq;

namespace DK.Syntax
{
	public abstract class QuickInfoLayout
	{
	}

	public class QuickInfoClassifiedString : QuickInfoLayout
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

		public ProbeClassifiedString SyntaxString => _text;
	}

	public class QuickInfoStack : QuickInfoLayout
	{
		private QuickInfoLayout[] _elements;

		public QuickInfoStack(params QuickInfoLayout[] elements)
		{
			_elements = elements.Where(e => e != null).ToArray();
		}

		public IEnumerable<QuickInfoLayout> Elements => _elements;
	}

	public class QuickInfoAttribute : QuickInfoLayout
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

		public string Label => _label;
		public ProbeClassifiedString Value => _value;
	}

	public class QuickInfoDescription : QuickInfoLayout
	{
		private string _text;

		public QuickInfoDescription(string text)
		{
			_text = text ?? throw new ArgumentNullException(nameof(text));
		}

		public string Text => _text;
	}

	public class QuickInfoMainLine : QuickInfoLayout
	{
		private string _text;

		public QuickInfoMainLine(string text)
		{
			_text = text ?? throw new ArgumentNullException(nameof(text));
		}

		public string Text => _text;
	}

	public class QuickInfoText : QuickInfoLayout
	{
		private string _text;
		private ProbeClassifierType _type;

		public QuickInfoText(ProbeClassifierType type, string text)
		{
			_type = type;
			_text = text ?? throw new ArgumentNullException(nameof(text));
		}

		public string Text => _text;
		public ProbeClassifierType Type => _type;
	}
}

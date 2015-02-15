using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel.Tokens;

namespace DkTools.CodeModel.Definitions
{
	internal class ConstantDefinition : Definition
	{
		private string _text;

		public ConstantDefinition(string name, string fileName, int startPos, string text)
			: base(name, fileName, startPos, null)
		{
			_text = text;
		}

		public string Text
		{
			get { return _text; }
		}

		public override bool CompletionVisible
		{
			get { return true; }
		}

		public override StatementCompletion.CompletionType CompletionType
		{
			get { return StatementCompletion.CompletionType.Constant; }
		}

		public override Classifier.ProbeClassifierType ClassifierType
		{
			get { return Classifier.ProbeClassifierType.Constant; }
		}

		public override string QuickInfoTextStr
		{
			get { return _text; }
		}

		public override System.Windows.UIElement QuickInfoTextWpf
		{
			get
			{
				return WpfMainLine(_text);
			}
		}

		public override string PickText
		{
			get { return QuickInfoTextStr; }
		}
	}
}

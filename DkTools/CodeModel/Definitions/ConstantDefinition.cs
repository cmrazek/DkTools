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
			: base(name, fileName, startPos, CreateExternalRefId(name, fileName))
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

		public override bool AllowsChild
		{
			get { return false; }
		}

		public override bool RequiresChild
		{
			get { return false; }
		}

		public override Definition GetChildDefinition(string name)
		{
			throw new NotSupportedException();
		}

		public override bool RequiresArguments
		{
			get { return false; }
		}

		private static string CreateExternalRefId(string name, string fileName)
		{
			if (!string.IsNullOrEmpty(fileName))
			{
				return string.Concat("const:", name, ":", System.IO.Path.GetFileName(fileName));
			}
			else
			{
				return string.Concat("const:", name);
			}
		}
	}
}

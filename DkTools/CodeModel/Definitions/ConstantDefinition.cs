using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel.Tokens;
using DkTools.QuickInfo;
using Microsoft.VisualStudio.Text.Adornments;

namespace DkTools.CodeModel.Definitions
{
	internal class ConstantDefinition : Definition
	{
		private string _text;

		public ConstantDefinition(string name, FilePosition filePos, string text)
			: base(name, filePos, CreateExternalRefId(name, filePos.FileName))
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

		public override StatementCompletion.ProbeCompletionType CompletionType
		{
			get { return StatementCompletion.ProbeCompletionType.Constant; }
		}

		public override Classifier.ProbeClassifierType ClassifierType
		{
			get { return Classifier.ProbeClassifierType.Constant; }
		}

		public override string QuickInfoTextStr
		{
			get { return _text; }
		}

		public override QuickInfoLayout QuickInfo => new QuickInfoText(Classifier.ProbeClassifierType.Constant, _text);

		public override string PickText
		{
			get { return QuickInfoTextStr; }
		}

		public override bool ArgumentsRequired
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

		public override bool CanRead
		{
			get
			{
				return true;
			}
		}
	}
}

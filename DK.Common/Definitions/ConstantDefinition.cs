using DK.Code;
using DK.Syntax;

namespace DK.Definitions
{
	public class ConstantDefinition : Definition
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

		public override ProbeCompletionType CompletionType
		{
			get { return ProbeCompletionType.Constant; }
		}

		public override ProbeClassifierType ClassifierType
		{
			get { return ProbeClassifierType.Constant; }
		}

		public override string QuickInfoTextStr
		{
			get { return _text; }
		}

		public override QuickInfoLayout QuickInfo => new QuickInfoText(ProbeClassifierType.Constant, _text);

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

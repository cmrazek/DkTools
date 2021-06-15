using DK.Code;
using DK.Modeling;
using DK.Schema;
using DK.Syntax;

namespace DK.Definitions
{
	public class StringDefDefinition : Definition
	{
		private Stringdef _stringDef;

		public StringDefDefinition(Stringdef stringDef, FilePosition filePos)
			: base(stringDef.Name, filePos, GetExternalRefId(stringDef.Name))
		{
			_stringDef = stringDef;
		}

		public override ServerContext ServerContext => ServerContext.Neutral;

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

		public override string QuickInfoTextStr => _stringDef.Text;

		public override QuickInfoLayout QuickInfo => new QuickInfoText(ProbeClassifierType.StringLiteral, CodeParser.StringToStringLiteral(_stringDef.Text));

		public override string PickText
		{
			get { return QuickInfoTextStr; }
		}

		public static string GetExternalRefId(string name)
		{
			return string.Concat("stringdef:", name);
		}

		public override bool ArgumentsRequired
		{
			get { return false; }
		}

		public override DataType DataType
		{
			get
			{
				return DataType.String;
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

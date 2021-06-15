using DK.Code;
using DK.Syntax;

namespace DK.Definitions
{
	/// <summary>
	/// A temporary definition object used to convey a file and position rather than an item in source code.
	/// </summary>
	public class FilePositionDefinition : Definition
	{
		public FilePositionDefinition(FilePosition filePos)
			: base(System.IO.Path.GetFileName(filePos.FileName), filePos, string.Empty)
		{
		}

		public override bool CompletionVisible => false;
		public override ProbeCompletionType CompletionType => ProbeCompletionType.Constant;
		public override ProbeClassifierType ClassifierType => ProbeClassifierType.Normal;
		public override string QuickInfoTextStr => LocationText;
		public override QuickInfoLayout QuickInfo => new QuickInfoMainLine(LocationText);
		public override string PickText => LocationText;
		public override ServerContext ServerContext => ServerContextHelper.FromFileName(FilePosition.FileName);
	}
}

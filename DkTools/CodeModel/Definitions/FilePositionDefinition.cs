using DkTools.QuickInfo;
using Microsoft.VisualStudio.Text.Adornments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Definitions
{
	/// <summary>
	/// A temporary definition object used to convey a file and position rather than an item in source code.
	/// </summary>
	class FilePositionDefinition : Definition
	{
		public FilePositionDefinition(FilePosition filePos)
			: base(System.IO.Path.GetFileName(filePos.FileName), filePos, string.Empty)
		{
		}

		public override bool CompletionVisible => false;
		public override StatementCompletion.ProbeCompletionType CompletionType => StatementCompletion.ProbeCompletionType.Constant;
		public override Classifier.ProbeClassifierType ClassifierType => Classifier.ProbeClassifierType.Normal;
		public override string QuickInfoTextStr => LocationText;
		public override QuickInfoLayout QuickInfo => new QuickInfoMainLine(LocationText);
		public override string PickText => LocationText;
	}
}

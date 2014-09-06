using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Definitions
{
	internal class ClassDefinition : Definition
	{
		public ClassDefinition(Scope scope, string name, string fileName)
			: base(scope, name, new ExternalToken(fileName, Span.Empty), true)
		{
		}

		public override bool CompletionVisible
		{
			get { return true; }
		}

		public override StatementCompletion.CompletionType CompletionType
		{
			get { return StatementCompletion.CompletionType.Class; }
		}

		public override string CompletionDescription
		{
			get { return string.Concat("Class: ", Name); }
		}

		public override Classifier.ProbeClassifierType ClassifierType
		{
			get { return Classifier.ProbeClassifierType.TableName; }
		}

		public override string QuickInfoText
		{
			get { return CompletionDescription; }
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel.Tokens;

namespace DkTools.CodeModel.Definitions
{
	internal class ClassDefinition : Definition
	{
		public ClassDefinition(string name, string fileName)
			: base(name, fileName, 0, FunctionFileScanning.FFClass.GetExternalRefId(name))
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

		public override Classifier.ProbeClassifierType ClassifierType
		{
			get { return Classifier.ProbeClassifierType.TableName; }
		}

		public override string QuickInfoTextStr
		{
			get { return string.Concat("Class: ", Name); }
		}

		public override System.Windows.UIElement QuickInfoTextWpf
		{
			get
			{
				return WpfAttribute("Class", Name);
			}
		}

		public override string PickText
		{
			get { return QuickInfoTextStr; }
		}
	}
}

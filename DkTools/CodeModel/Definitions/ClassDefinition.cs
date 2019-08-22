using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Language.StandardClassification;
using DkTools.CodeModel.Tokens;

namespace DkTools.CodeModel.Definitions
{
	internal class ClassDefinition : Definition
	{
		public ClassDefinition(string name, string fileName)
			: base(name, new FilePosition(fileName, 0, true), FunctionFileScanning.FFClass.GetExternalRefId(name))
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

		public override object QuickInfoElements
		{
			get { return QuickInfoAttributeElement("Class", QuickInfoClassified(QuickInfoRun(Classifier.ProbeClassifierType.TableName, Name))); }
		}

		public override string PickText
		{
			get { return QuickInfoTextStr; }
		}

		public override bool RequiresChild
		{
			get { return true; }
		}

		public override bool AllowsChild
		{
			get { return true; }
		}

		public override IEnumerable<Definition> GetChildDefinitions(string name)
		{
			foreach (var cls in ProbeToolsPackage.Instance.FunctionFileScanner.CurrentApp.GetClasses(Name))
			{
				foreach (var func in cls.GetFunctionDefinitions(name))
				{
					yield return func;
				}
			}
		}

		public override IEnumerable<Definition> ChildDefinitions
		{
			get
			{
				foreach (var cls in ProbeToolsPackage.Instance.FunctionFileScanner.CurrentApp.GetClasses(Name))
				{
					foreach (var func in cls.FunctionDefinitions)
					{
						yield return func;
					}
				}
			}
		}

		public override bool ArgumentsRequired
		{
			get { return false; }
		}

		public override bool CaseSensitive
		{
			get { return false; }
		}
	}
}

using DkTools.Classifier;
using DkTools.CodeModel.Definitions;
using Microsoft.VisualStudio.Language.Intellisense;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.Navigation
{
	public class DkPeekResultDisplayInfo : IPeekResultDisplayInfo
	{
		private Definition _def;

		internal DkPeekResultDisplayInfo(Definition definition)
		{
			_def = definition ?? throw new ArgumentNullException(nameof(definition));
		}

		public string Label => _def.QuickInfoTextStr;

		public object LabelTooltip => _def.QuickInfoElements;

		public string Title => _def.Name;

		public object TitleTooltip => new ProbeClassifiedString(ProbeClassifierType.Normal, _def.Name).ToClassifiedTextElement();

		public void Dispose()
		{
			_def = null;
		}
	}
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel.Tokens;

namespace DkTools.CodeModel.Definitions
{
	internal class VariableDefinition : Definition
	{
		private DataType _dataType;
		private bool _arg;

		public VariableDefinition(string name, string fileName, int startPos, DataType dataType, bool arg)
			: base(name, fileName, startPos)
		{
#if DEBUG
			if (dataType == null) throw new ArgumentNullException("dataType");
#endif
			_dataType = dataType;
			_arg = arg;
		}

		public DataType DataType
		{
			get { return _dataType; }
		}

		public bool Argument
		{
			get { return _arg; }
		}

		public override bool CompletionVisible
		{
			get { return true; }
		}

		public override StatementCompletion.CompletionType CompletionType
		{
			get { return StatementCompletion.CompletionType.Variable; }
		}

		public override Classifier.ProbeClassifierType ClassifierType
		{
			get { return Classifier.ProbeClassifierType.Variable; }
		}

		public override string QuickInfoTextStr
		{
			get
			{
				if (_dataType != null) return string.Concat(_dataType.Name, " ", Name);
				return Name;
			}
		}

		public override System.Windows.UIElement QuickInfoTextWpf
		{
			get
			{
				if (_dataType != null)
				{
					if (!string.IsNullOrEmpty(_dataType.InfoText))
					{
						return WpfDivs(WpfMainLine(string.Concat(_dataType.Name, " ", Name)), WpfInfoLine(_dataType.InfoText));
					}
					else
					{
						return WpfDivs(WpfMainLine(string.Concat(_dataType.Name, " ", Name)));
					}
				}
				else
				{
					return WpfMainLine(Name);
				}
			}
		}

		public override void DumpTreeInner(System.Xml.XmlWriter xml)
		{
			base.DumpTreeInner(xml);
			_dataType.DumpTree(xml);
		}
	}
}

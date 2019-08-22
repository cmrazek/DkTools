using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using DkTools.CodeModel.Tokens;

namespace DkTools.CodeModel.Definitions
{
	enum VariableType
	{
		Global,
		Argument,
		Local
	}

	internal class VariableDefinition : Definition
	{
		private DataType _dataType;
		private bool _arg;
		private int[] _arrayLengths;
		private string _declText;
		private VariableType _varType;

		public VariableDefinition(string name, FilePosition filePos, DataType dataType, bool arg, int[] arrayLengths, VariableType varType)
			: base(name, filePos, null)
		{
#if DEBUG
			if (dataType == null) throw new ArgumentNullException("dataType");
#endif
			_dataType = dataType;
			_arg = arg;
			_arrayLengths = arrayLengths;
			_varType = varType;

			// Build the declaration text
			var sb = new StringBuilder();
			if (_dataType != null)
			{
				sb.Append(_dataType.Name);
				sb.Append(' ');
			}
			sb.Append(Name);

			if (_arrayLengths != null)
			{
				foreach (var len in _arrayLengths)
				{
					sb.Append('[');
					sb.Append(len);
					sb.Append(']');
				}
			}
			_declText = sb.ToString();
		}

		public override DataType DataType
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
				if (_dataType != null && !string.IsNullOrEmpty(_dataType.InfoText))
				{
					return string.Concat(_declText, "\r\n", _dataType.InfoText);
				}
				else
				{
					return _declText;
				}
			}
		}

		public override object QuickInfoElements
		{
			get
			{
				var runs = new List<ClassifiedTextRun>();

				if (_dataType != null)
				{
					runs.AddRange(_dataType.QuickInfoRuns);
					runs.Add(QuickInfoRun(Classifier.ProbeClassifierType.Normal, " "));
				}
				runs.Add(QuickInfoRun(Classifier.ProbeClassifierType.Variable, Name));

				if (_arrayLengths != null)
				{
					foreach (var len in _arrayLengths)
					{
						runs.Add(QuickInfoRun(Classifier.ProbeClassifierType.Operator, "["));
						runs.Add(QuickInfoRun(Classifier.ProbeClassifierType.Number, len.ToString()));
						runs.Add(QuickInfoRun(Classifier.ProbeClassifierType.Operator, "]"));
					}
				}

				return new ClassifiedTextElement(runs);
			}
		}

		public override void DumpTreeInner(System.Xml.XmlWriter xml)
		{
			base.DumpTreeInner(xml);
			_dataType.DumpTree(xml);
		}

		public override string PickText
		{
			get { return QuickInfoTextStr; }
		}

		public override bool AllowsChild
		{
			get
			{
				if (_dataType != null) return _dataType.HasCompletionOptions;
				return false;
			}
		}

		public override bool RequiresChild
		{
			get { return false; }
		}

		public override IEnumerable<Definition> GetChildDefinitions(string name)
		{
			if (_dataType != null)
			{
				foreach (var opt in _dataType.CompletionOptions)
				{
					if (opt.Name == name) yield return opt;
				}
			}
		}

		public override IEnumerable<Definition> ChildDefinitions
		{
			get
			{
				if (_dataType != null) return _dataType.CompletionOptions;
				return Definition.EmptyArray;
			}
		}

		public override bool ArgumentsRequired
		{
			get { return false; }
		}

		public override bool CanRead
		{
			get
			{
				return true;
			}
		}

		public override bool CanWrite
		{
			get
			{
				return true;
			}
		}

		public int[] ArrayLengths
		{
			get { return _arrayLengths; }
		}

		public override int SelectionOrder
		{
			get
			{
				switch (_varType)
				{
					case VariableType.Global:
						return 20;
					case VariableType.Argument:
						return 30;
					case VariableType.Local:
						return 40;
					default:
						return 0;
				}
			}
		}
	}
}

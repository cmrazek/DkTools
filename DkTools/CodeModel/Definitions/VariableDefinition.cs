using System;
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
		private int[] _arrayLengths;
		private string _declText;

		public VariableDefinition(string name, FilePosition filePos, DataType dataType, bool arg, int[] arrayLengths)
			: base(name, filePos, null)
		{
#if DEBUG
			if (dataType == null) throw new ArgumentNullException("dataType");
#endif
			_dataType = dataType;
			_arg = arg;
			_arrayLengths = arrayLengths;

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

		public override System.Windows.UIElement QuickInfoTextWpf
		{
			get
			{
				if (_dataType != null && !string.IsNullOrEmpty(_dataType.InfoText))
				{
					return WpfDivs(WpfMainLine(_declText), WpfInfoLine(_dataType.InfoText));
				}
				else
				{
					return WpfMainLine(_declText);
				}
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
	}
}

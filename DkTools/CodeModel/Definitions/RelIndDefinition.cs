using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel.Definitions
{
	internal class RelIndDefinition : Definition
	{
		private string _baseTableName;
		private string _relText;
		private string _devDesc;

		public static readonly RelIndDefinition Physical = new RelIndDefinition("physical", string.Empty, string.Empty, "Index on rowno");

		public RelIndDefinition(string name, string baseTableName, string relText, string devDesc)
			: base(name, null, -1, string.Concat("relind:", baseTableName, ".", name))
		{
			_relText = relText;
			_devDesc = devDesc;
			_baseTableName = baseTableName;
		}

		public override bool CompletionVisible
		{
			get { return true; }
		}

		public override StatementCompletion.CompletionType CompletionType
		{
			get { return StatementCompletion.CompletionType.Table; }
		}

		public override Classifier.ProbeClassifierType ClassifierType
		{
			get { return Classifier.ProbeClassifierType.TableName; }
		}

		public override string QuickInfoTextStr
		{
			get
			{
				if (!string.IsNullOrEmpty(_devDesc)) return string.Concat(_relText, "\r\n", _devDesc);
				return _relText;
			}
		}

		public override System.Windows.UIElement QuickInfoTextWpf
		{
			get
			{
				var items = new List<System.Windows.UIElement>();
				items.Add(WpfMainLine(_relText));
				if (!string.IsNullOrEmpty(_devDesc)) items.Add(WpfInfoLine(_devDesc));
				return WpfDivs(items);
			}
		}

		public string BaseTableName
		{
			get { return _baseTableName; }
		}

		public override string PickText
		{
			get { return QuickInfoTextStr; }
		}
	}
}

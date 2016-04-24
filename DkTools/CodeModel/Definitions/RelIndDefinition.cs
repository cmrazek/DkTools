using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.DkDict;

namespace DkTools.CodeModel.Definitions
{
	internal class RelIndDefinition : Definition
	{
		private RelInd _relind;
		private string _baseTableName;
		private string _relText;
		private string _devDesc;

		public static readonly RelIndDefinition Physical = new RelIndDefinition(
			new RelInd(RelIndType.Index, "physical", 0, string.Empty, FilePosition.Empty), string.Empty, string.Empty, 
			"Index on rowno", FilePosition.Empty);

		public RelIndDefinition(RelInd relind, string baseTableName, string relText, string devDesc, FilePosition filePos)
			: base(relind.Name, filePos, GetExternalRefId(baseTableName, relind.Name))
		{
			_relind = relind;
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

		public static string GetExternalRefId(string baseTableName, string name)
		{
			return string.Concat("relind:", baseTableName, ".", name);
		}

		public override bool AllowsChild
		{
			get { return true; }
		}

		public override bool RequiresChild
		{
			get { return false; }
		}

		public override Definition GetChildDefinition(string name)
		{
			var col = _relind.GetColumn(name);
			if (col != null) return col.Definition;
			return null;
		}

		public override bool RequiresArguments
		{
			get { return false; }
		}
	}
}

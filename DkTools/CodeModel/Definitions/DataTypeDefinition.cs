using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel.Tokens;

namespace DkTools.CodeModel.Definitions
{
	internal class DataTypeDefinition : Definition
	{
		private DataType _dataType;

		public DataTypeDefinition(string name, string fileName, int startPos, DataType dataType)
			: base(name, fileName, startPos, null)
		{
#if DEBUG
			if (dataType == null) throw new ArgumentNullException("dataType");
#endif
			_dataType = dataType;
		}

		public DataTypeDefinition(string name, DataType dataType, bool global)
			: base(name, null, -1, global ? string.Concat("typedef:", name) : null)
		{
			_dataType = dataType;
		}

		public override DataType DataType
		{
			get { return _dataType; }
		}

		public override bool CompletionVisible
		{
			get { return true; }
		}

		public override StatementCompletion.CompletionType CompletionType
		{
			get { return StatementCompletion.CompletionType.DataType; }
		}

		public override Classifier.ProbeClassifierType ClassifierType
		{
			get { return Classifier.ProbeClassifierType.DataType; }
		}

		public override string QuickInfoTextStr
		{
			get { return !string.IsNullOrEmpty(_dataType.InfoText) ? _dataType.InfoText : _dataType.Name; }
		}

		public override System.Windows.UIElement QuickInfoTextWpf
		{
			get
			{
				return _dataType.QuickInfoWpf;
			}
		}

		public override string PickText
		{
			get { return QuickInfoTextStr; }
		}

		public override bool AllowsChild
		{
			get { return false; }
		}

		public override bool RequiresChild
		{
			get { return false; }
		}

		public override Definition GetChildDefinition(string name)
		{
			throw new NotSupportedException();
		}

		public override bool RequiresArguments
		{
			get { return false; }
		}
	}
}

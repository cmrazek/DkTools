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

		public DataTypeDefinition(string name, FilePosition filePos, DataType dataType)
			: base(name, filePos, CreateExternalRefId(name, filePos.FileName))
		{
#if DEBUG
			if (dataType == null) throw new ArgumentNullException("dataType");
#endif
			_dataType = dataType;
		}

		public DataTypeDefinition(string name, DataType dataType)
			: base(name, FilePosition.Empty, CreateExternalRefId(name, null))
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

		public override object QuickInfoElements => _dataType.QuickInfoElements;

		public override string PickText
		{
			get { return QuickInfoTextStr; }
		}

		public override bool ArgumentsRequired
		{
			get { return false; }
		}

		private static string CreateExternalRefId(string name, string fileName)
		{
			if (!string.IsNullOrEmpty(fileName))
			{
				return string.Concat("typedef:", name, ":", System.IO.Path.GetFileName(fileName));
			}
			else
			{
				return string.Concat("typedef:", name);
			}
		}
	}
}

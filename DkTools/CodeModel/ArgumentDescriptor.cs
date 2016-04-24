using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DkTools.CodeModel
{
	struct ArgumentDescriptor
	{
		private string _name;
		private DataType _dataType;
		private PassByMethod _passByMethod;

		public ArgumentDescriptor(string name, DataType dataType, PassByMethod passByMethod)
		{
			_name = name;
			_dataType = dataType;
			_passByMethod = passByMethod;
		}

		public ArgumentDescriptor(string name, DataType dataType)
		{
			_name = name;
			_dataType = dataType;
			_passByMethod = DkTools.CodeModel.PassByMethod.Value;
		}

		public string Name
		{
			get { return _name; }
		}

		public DataType DataType
		{
			get { return _dataType; }
		}

		public PassByMethod PassByMethod
		{
			get { return _passByMethod; }
		}

		public string ToDbString()
		{
			var sb = new StringBuilder();
			if (!string.IsNullOrEmpty(_name))
			{
				sb.Append("name ");
				sb.Append(_name);
			}

			switch (_passByMethod)
			{
				case DkTools.CodeModel.PassByMethod.Reference:
					if (sb.Length > 0) sb.Append(' ');
					sb.Append("ref");
					break;
				case DkTools.CodeModel.PassByMethod.ReferencePlus:
					if (sb.Length > 0) sb.Append(' ');
					sb.Append("refp");
					break;
			}

			if (_dataType != null)
			{
				if (sb.Length > 0) sb.Append(' ');
				sb.Append("dt ");
				sb.Append(_dataType.ToCodeString());
			}

			return sb.ToString();
		}

		public static ArgumentDescriptor? ParseFromDb(string str)
		{
			string name = null;
			PassByMethod passByMethod = PassByMethod.Value;
			DataType dataType = null;

			var code = new CodeParser(str);
			if (code.ReadExactWholeWord("name"))
			{
				if (!code.ReadWord())
				{
					Log.WriteDebug("Unable to parse argument name from: {0}", str);
					return null;
				}
				name = code.Text;
			}

			if (code.ReadExactWholeWord("ref"))
			{
				passByMethod = PassByMethod.Reference;
			}
			else if (code.ReadExactWholeWord("refp"))
			{
				passByMethod = PassByMethod.ReferencePlus;
			}

			if (code.ReadExactWholeWord("dt"))
			{
				dataType = DataType.TryParse(new DataType.ParseArgs { Code = code });
				if (dataType == null)
				{
					Log.WriteDebug("Unable to parse data type from: {0}", str);
				}
			}

			return new ArgumentDescriptor(name, dataType, passByMethod);
		}
	}
}

using DK.AppEnvironment;
using DK.Code;
using DK.Diagnostics;
using DK.Syntax;
using System.Text;

namespace DK.Modeling
{
	public class ArgumentDescriptor
	{
		private string _name;
		private DataType _dataType;
		private PassByMethod _passByMethod;
		private ProbeClassifiedString _source;
		private CodeSpan _sigSpan;

		public static readonly ArgumentDescriptor[] EmptyArray = new ArgumentDescriptor[0];

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
			_passByMethod = PassByMethod.Value;
		}

		public string Name
		{
			get { return _name; }
			set { _name = value; }
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
				case PassByMethod.Reference:
					if (sb.Length > 0) sb.Append(' ');
					sb.Append("ref");
					break;
				case PassByMethod.ReferencePlus:
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

		public static ArgumentDescriptor ParseFromDb(string str, DkAppSettings appSettings)
		{
			string name = null;
			PassByMethod passByMethod = PassByMethod.Value;
			DataType dataType = null;

			var code = new CodeParser(str);
			if (code.ReadExactWholeWord("name"))
			{
				if (!code.ReadWord())
				{
					Log.Debug("Unable to parse argument name from: {0}", str);
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
				dataType = DataType.TryParse(new DataType.ParseArgs(code, appSettings));
				if (dataType == null)
				{
					Log.Debug("Unable to parse data type from: {0}", str);
				}
			}

			return new ArgumentDescriptor(name, dataType, passByMethod);
		}

		public string SignatureText
		{
			get { return ClassifiedString.ToString(); }
		}

		public ProbeClassifiedString ClassifiedString
		{
			get
			{
				if (_source == null)
				{
					var pcs = new ProbeClassifiedStringBuilder();
					var spaceRequired = false;

					if (_dataType != null)
					{
						pcs.AddClassifiedString(_dataType.GetClassifiedString(shortVersion: true));
						spaceRequired = true;
					}

					switch (_passByMethod)
					{
						case PassByMethod.Reference:
							if (spaceRequired) pcs.AddSpace();
							pcs.AddOperator("&");
							spaceRequired = false;
							break;
						case PassByMethod.ReferencePlus:
							if (spaceRequired) pcs.AddSpace();
							pcs.AddOperator("&+");
							spaceRequired = false;
							break;
					}

					if (!string.IsNullOrEmpty(_name))
					{
						if (spaceRequired) pcs.AddSpace();
						pcs.AddVariable(_name);
					}

					_source = pcs.ToClassifiedString();
				}

				return _source;
			}
		}

		public CodeSpan SignatureSpan
		{
			get { return _sigSpan; }
			set { _sigSpan = value; }
		}
	}
}

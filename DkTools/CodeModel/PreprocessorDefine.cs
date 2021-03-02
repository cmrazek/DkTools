using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel.Definitions;

namespace DkTools.CodeModel
{
	class PreprocessorDefine
	{
		private string _name;
		private string _content;
		private List<string> _paramNames;
		private FilePosition _filePos;
		private bool _disabled;
		private DataType _dataType;
		private Definition _def;

		public PreprocessorDefine(string name, string content, List<string> paramNames, FilePosition filePos, DkAppSettings appSettings)
		{
			_name = name;
			_content = content;
			_paramNames = paramNames;
			_filePos = filePos;

			if (_paramNames == null)
			{
				var parser = new CodeParser(_content);
				var dataType = DataType.TryParse(new DataType.ParseArgs(parser, appSettings));
				if (dataType != null)
				{
					// If the data type does not consume the entire string, then this is not a data type definition.
					if (parser.Read()) dataType = null;
					else if (dataType.Name == null) dataType.Name = _name;
				}

				_dataType = dataType;
			}
		}

		public string Name
		{
			get { return _name; }
		}

		public string Content
		{
			get { return _content; }
		}

		public List<string> ParamNames
		{
			get { return _paramNames; }
		}

		public bool Disabled
		{
			get { return _disabled; }
			set { _disabled = value; }
		}

		public Definition Definition
		{
			get
			{
				if (_def == null)
				{
					if (_paramNames == null)
					{
						if (_dataType != null)
						{
							_def = new Definitions.DataTypeDefinition(_name, _filePos, _dataType);
						}
						else
						{
							_def = new Definitions.ConstantDefinition(_name, _filePos, CodeParser.NormalizeText(_content));
						}
					}
					else
					{
						var args = new List<ArgumentDescriptor>();
						foreach (var paramName in _paramNames)
						{
							args.Add(new ArgumentDescriptor(paramName, DataType.Unknown));
						}

						var sig = new FunctionSignature(false, FunctionPrivacy.Public, DataType.Unknown, null, _name, null, args);

						_def = new Definitions.MacroDefinition(_name, _filePos, sig, CodeParser.NormalizeText(_content));
					}
				}

				return _def;
			}
		}

		public bool IsDataType
		{
			get { return _dataType != null; }
		}

		public DataType DataType
		{
			get { return _dataType; }
		}
	}
}

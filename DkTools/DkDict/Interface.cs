using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.Classifier;
using DkTools.CodeModel;
using DkTools.CodeModel.Definitions;

namespace DkTools.DkDict
{
	class Interface
	{
		public string Path { get; set; }
		public bool Framework { get; set; }
		public string ProgId { get; set; }
		public string ClsId { get; set; }
		public string TLibId { get; set; }
		public string Iid { get; set; }
		public string Description { get; set; }
		public string InterfaceName { get; set; }
		public bool Default { get; set; }
		public bool DefaultEvent { get; set; }

		private string _name;
		private List<Tag> _tags;
		private InterfaceTypeDefinition _def;
		private FilePosition _filePos;
		private List<InterfaceMethod> _methods = new List<InterfaceMethod>();
		private List<InterfaceProperty> _properties = new List<InterfaceProperty>();
		private DataType _dataType;

		public Interface(string name, FilePosition filePos)
		{
			_name = name;
			_filePos = filePos;
			_def = new InterfaceTypeDefinition(this, filePos);
			_dataType = new DataType(ValType.Interface, "",
				new ProbeClassifiedString(ProbeClassifierType.Interface, _name),
				CodeModel.Definitions.Definition.EmptyArray, CodeModel.DataType.CompletionOptionsType.InterfaceMembers)
			{
				Interface = this
			};
		}

		public void AddTag(Tag tag)
		{
			if (_tags == null) _tags = new List<Tag>();
			_tags.Add(tag);
		}

		public InterfaceTypeDefinition Definition
		{
			get
			{
				if (_def == null)
				{
					_def = new InterfaceTypeDefinition(_name, _filePos);
				}
				return _def;
			}
		}

		public string Name
		{
			get { return _name; }
		}

		public DataType DataType
		{
			get { return _dataType; }
		}

		public IEnumerable<Definition> MethodDefinitions
		{
			get
			{
				foreach (var method in _methods)
				{
					yield return method.Definition;
				}
			}
		}

		public IEnumerable<Definition> PropertyDefinitions
		{
			get
			{
				foreach (var prop in _properties)
				{
					yield return prop.Definition;
				}
			}
		}

		public IEnumerable<InterfaceMethodDefinition> GetMethods(string name)
		{
			foreach (var method in _methods) yield return method.Definition;
		}

		public IEnumerable<InterfacePropertyDefinition> GetProperties(string name)
		{
			foreach (var prop in _properties) yield return prop.Definition;
		}

		public void LoadFromRepo(DICTSRVRLib.IPInterfaceType repoIntf, ProbeAppSettings appSettings)
		{
			var str = repoIntf.InterfaceName;
			if (!string.IsNullOrEmpty(str))
			{
				str = CleanInterfaceName(str);
				if (str != _name)
				{
					appSettings.Dict.AddImpliedInterface(str, this);
					_dataType.Name = _dataType.Source.ToString();
					_dataType.Source = new Classifier.ProbeClassifiedString(Classifier.ProbeClassifierType.Interface, str);
				}
			}

			for (int m = 1, mm = repoIntf.MethodCount; m <= mm; m++)
			{
				var funcName = repoIntf.MethodName[m];
				var returnDataType = ConvertRepoDataType(_name, funcName, repoIntf.MethodDataDef[m], appSettings);

				var args = new List<ArgumentDescriptor>();
				for (int a = 1, aa = repoIntf.MethodParamCount[m]; a <= aa; a++)
				{
					args.Add(new ArgumentDescriptor(repoIntf.MethodParamName[m, a],
						ConvertRepoDataType(_name, funcName, repoIntf.MethodParamDataDef[m, a], appSettings)));
				}

				var sig = new FunctionSignature(false, FunctionPrivacy.Public, returnDataType, null, funcName, null, args);

				_methods.Add(new InterfaceMethod(_def, sig));
			}

			for (int p = 1, pp = repoIntf.PropertyCount; p <= pp; p++)
			{
				var propName = repoIntf.PropertyName[p];
				var propDataType = ConvertRepoDataType(_name, propName, repoIntf.PropertyDataDef[p], appSettings);

				_properties.Add(new InterfaceProperty(_def, propName, propDataType));
			}
		}

		private DataType ConvertRepoDataType(string intfName, string fieldName, DICTSRVRLib.PDataDef dataDef, ProbeAppSettings appSettings)
		{
			if (dataDef == null) return DataType.Void;

			var str = dataDef.InterfaceName;
			if (!string.IsNullOrEmpty(str))
			{
				str = CleanInterfaceName(str);

				var intf = appSettings.Dict.GetInterface(str);
				if (intf != null) return intf._dataType;

				intf = appSettings.Dict.GetImpliedInterface(str);
				if (intf != null) return intf._dataType;

				return new DataType(ValType.Interface, null,
					new Classifier.ProbeClassifiedString(Classifier.ProbeClassifierType.Interface, CleanInterfaceName(str)));
			}

			var code = new CodeParser(dataDef.TypeText[0]);
			var dataType = DataType.TryParse(new DataType.ParseArgs(code, appSettings));

			if (dataType == null)
			{
				Log.Warning("Unable to parse repo data type for {0}.{1}: {2}", intfName, fieldName, code.DocumentText);
				return DataType.Void;
			}

			dataType.Interface = this;
			return dataType;
		}

		private string CleanInterfaceName(string str)
		{
			if (str.EndsWith("&")) str = str.Substring(0, str.Length - 1);
			return str;
		}
	}
}

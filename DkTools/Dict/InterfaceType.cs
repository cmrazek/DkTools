using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DkTools.CodeModel;
using DkTools.CodeModel.Definitions;

namespace DkTools.Dict
{
	internal sealed class InterfaceType : IDictObj
	{
		private string _name;
		private string _devDesc;
		private InterfaceTypeDefinition _definition;
		private InterfaceMethodDefinition[] _methodDefs;
		private InterfacePropertyDefinition[] _propDefs;

		public InterfaceType(DICTSRVRLib.IPInterfaceType repoIntType)
		{
			_name = repoIntType.Name;

			var dev = repoIntType as DICTSRVRLib.IPDictObj;
			if (dev != null)
			{
				var devInfo = dev.DevInfo;
				if (!string.IsNullOrEmpty(devInfo)) _devDesc = devInfo;
			}

			var sb = new StringBuilder();
			var methods = new List<InterfaceMethodDefinition>();
			var props = new List<InterfacePropertyDefinition>();

			_definition = new CodeModel.Definitions.InterfaceTypeDefinition(this);

			for (int m = 1, mm = repoIntType.MethodCount; m <= mm; m++)
			{
				sb.Clear();

				var dataDef = repoIntType.MethodDataDef[m];
				var typeText = dataDef.TypeText[0];
				var parser = new TokenParser.Parser(typeText);
				var returnDataType = DataType.Parse(new DataType.ParseArgs
				{
					Code = parser,
					Flags = DataType.ParseFlag.FromRepo | DataType.ParseFlag.InterfaceType
				});
#if DEBUG
				DataType.CheckDataTypeParsing(typeText, parser, returnDataType);
#endif
				if (returnDataType == null) returnDataType = new DataType(typeText);
				sb.Append(returnDataType.Name);
				sb.Append(' ');


				var methodName = repoIntType.MethodName[m];
				sb.Append(methodName);
				sb.Append('(');

				for (int p = 1, pp = repoIntType.MethodParamCount[m]; p <= pp; p++)
				{
					if (p > 1) sb.Append(", ");

					var paramDataDef = repoIntType.MethodParamDataDef[m, p];
					var paramTypeText = paramDataDef.TypeText[0];
					parser = new TokenParser.Parser(paramTypeText);
					var paramDataType = DataType.Parse(new DataType.ParseArgs
					{
						Code = parser,
						Flags = DataType.ParseFlag.FromRepo | DataType.ParseFlag.InterfaceType
					});
#if DEBUG
					DataType.CheckDataTypeParsing(paramTypeText, parser, paramDataType);
#endif
					if (paramDataType == null) paramDataType = new DataType(paramTypeText);
					sb.Append(paramDataType.Name);
					sb.Append(' ');

					sb.Append(repoIntType.MethodParamName[m, p]);
				}

				sb.Append(')');

				methods.Add(new InterfaceMethodDefinition(_definition, methodName, sb.ToString(), returnDataType));
			}

			_methodDefs = methods.ToArray();

			for (int p = 1, pp = repoIntType.PropertyCount; p <= pp; p++)
			{
				sb.Clear();

				var dataDef = repoIntType.PropertyDataDef[p];
				var typeText = dataDef.TypeText[0];
				var parser = new TokenParser.Parser(typeText);
				var dataType = DataType.Parse(new DataType.ParseArgs
				{
					Code = parser,
					Flags = DataType.ParseFlag.FromRepo | DataType.ParseFlag.InterfaceType
				});
#if DEBUG
				DataType.CheckDataTypeParsing(typeText, parser, dataType);
#endif
				if (dataType == null) dataType = new DataType(typeText);
				sb.Append(dataType.Name);
				sb.Append(' ');

				var propName = repoIntType.PropertyName[p];
				sb.Append(propName);

				props.Add(new InterfacePropertyDefinition(_definition, propName, dataType));
			}

			_propDefs = props.ToArray();
		}

		public string Name
		{
			get { return _name; }
		}

		public string DevDescription
		{
			get { return _devDesc; }
		}

		public CodeModel.Definitions.InterfaceTypeDefinition Definition
		{
			get { return _definition; }
		}

		public object CreateRepoObject(Dict dict)
		{
			return dict.GetInterface(_name);
		}

		public IEnumerable<CodeModel.Definitions.InterfaceMethodDefinition> MethodDefinitions
		{
			get { return _methodDefs; }
		}

		public CodeModel.Definitions.InterfaceMethodDefinition GetMethod(string name)
		{
			return (from m in _methodDefs where m.Name == name select m).FirstOrDefault();
		}

		public IEnumerable<CodeModel.Definitions.InterfacePropertyDefinition> PropertyDefinitions
		{
			get { return _propDefs; }
		}

		public CodeModel.Definitions.InterfacePropertyDefinition GetProperty(string name)
		{
			return (from m in _propDefs where m.Name == name select m).FirstOrDefault();
		}
	}
}
